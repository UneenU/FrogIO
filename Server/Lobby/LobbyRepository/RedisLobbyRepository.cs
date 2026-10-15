using System.Text.Json;
using StackExchange.Redis;

namespace Server.Lobby.LobbyRepository;

public class RedisLobbyRepository(IConnectionMultiplexer redis) : ILobbyRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    private const string LobbyIdsKey = "lobbies:ids";
    private const int MaxRetries = 5;

    private static string LobbyKey(string lobbyId) => $"lobby:{lobbyId}";
    private static string MembersKey(string lobbyId) => $"lobby:{lobbyId}:members";
    private static string UserLobbyKey(int userId) => $"user-lobby:{userId}";

    public async Task AddLobbyAsync(Lobby lobby)
    {
        var transaction = _database.CreateTransaction();
        transaction.AddCondition(Condition.KeyNotExists(LobbyKey(lobby.LobbyId)));

        _ = transaction.HashSetAsync(LobbyKey(lobby.LobbyId), [
            new("name", lobby.LobbyName),
            new("maxPlayers", lobby.MaxPlayers)
        ]);

        var memberEntries = new List<HashEntry>();
        foreach (var member in lobby.Members)
        {
            memberEntries.Add(new HashEntry(member.UserId, JsonSerializer.Serialize(member.ToDto())));
            _ = transaction.StringSetAsync(UserLobbyKey(member.UserId), lobby.LobbyId);
        }

        if (memberEntries.Count > 0)
            _ = transaction.HashSetAsync(MembersKey(lobby.LobbyId), memberEntries.ToArray());

        _ = transaction.SetAddAsync(LobbyIdsKey, lobby.LobbyId);

        if (!await transaction.ExecuteAsync())
            throw new InvalidOperationException($"Lobby '{lobby.LobbyId}' already exists.");
    }

    public async Task<Lobby?> GetLobbyByIdAsync(string lobbyId)
    {
        var lobbyTask = _database.HashGetAllAsync(LobbyKey(lobbyId));
        var membersTask = _database.HashGetAllAsync(MembersKey(lobbyId));
        await Task.WhenAll(lobbyTask, membersTask);

        var meta = lobbyTask.Result;
        if (meta.Length == 0)
            return null;

        var fields = meta.ToDictionary(e => e.Name.ToString(), e => e.Value);
        var name = fields["name"].ToString();
        var maxPlayers = (int)fields["maxPlayers"];

        var members = membersTask.Result.Select(e => DeserializeMember(e.Value)).ToList();

        return Lobby.Rehydrate(lobbyId, name, maxPlayers, members);
    }

    public async Task<bool> SetLobbyMemberAsync(string lobbyId, LobbyMember member)
    {
        var lobbyKey = LobbyKey(lobbyId);
        var membersKey = MembersKey(lobbyId);
        var userKey = UserLobbyKey(member.UserId);
        var json = JsonSerializer.Serialize(member.ToDto());

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var maxPlayersTask = _database.HashGetAsync(lobbyKey, "maxPlayers");
            var isMemberTask = _database.HashExistsAsync(membersKey, member.UserId);
            var countTask = _database.HashLengthAsync(membersKey);
            await Task.WhenAll(maxPlayersTask, isMemberTask, countTask);

            if (maxPlayersTask.Result.IsNull)
                return false; // лобби не существует

            var maxPlayers = (int)maxPlayersTask.Result;
            var isExisting = isMemberTask.Result;

            if (!isExisting && countTask.Result >= maxPlayers)
                return false; // лобби заполнено

            var transaction = _database.CreateTransaction();
            transaction.AddCondition(Condition.KeyExists(lobbyKey));

            if (isExisting)
            {
                // участник всё ещё должен быть в лобби.
                transaction.AddCondition(Condition.HashExists(membersKey, member.UserId));
            }
            else
            {
                // участника нет, и в лобби всё ещё есть место.
                transaction.AddCondition(Condition.HashNotExists(membersKey, member.UserId));
                transaction.AddCondition(Condition.HashLengthLessThan(membersKey, maxPlayers));
            }

            _ = transaction.HashSetAsync(membersKey, member.UserId, json);
            _ = transaction.StringSetAsync(userKey, lobbyId);

            if (await transaction.ExecuteAsync())
                return true;

            // Условие не выполнилось: кто-то изменил лобби между чтением и записью.
            // Следующая итерация заново оценит состояние.
        }

        return false;
    }

    public async Task RemoveUserFromLobbyAsync(string lobbyId, int userId)
    {
        var userKey = UserLobbyKey(userId);

        var transaction = _database.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(userKey, lobbyId));
        _ = transaction.HashDeleteAsync(MembersKey(lobbyId), userId);
        _ = transaction.KeyDeleteAsync(userKey);

        if (!await transaction.ExecuteAsync())
        {
            await _database.HashDeleteAsync(MembersKey(lobbyId), userId);
        }
    }

    public async Task<string?> FindUserLobbyAsync(int userId)
    {
        var value = await _database.StringGetAsync(UserLobbyKey(userId));
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<IReadOnlyList<LobbyMember>> GetLobbyMembersAsync(string lobbyId)
    {
        var entries = await _database.HashGetAllAsync(MembersKey(lobbyId));
        return entries.Select(e => DeserializeMember(e.Value)).ToList();
    }


    public async Task RemoveLobbyAsync(string lobbyId)
    {
        var lobbyKey = LobbyKey(lobbyId);
        var membersKey = MembersKey(lobbyId);

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var memberIds = await _database.HashKeysAsync(membersKey);
            var userKeys = memberIds.Select(id => UserLobbyKey((int)id)).ToArray();
            var pointers = await Task.WhenAll(userKeys.Select(k => _database.StringGetAsync(k)));

            var transaction = _database.CreateTransaction();

            // Состав участников не должен измениться с момента чтения:
            // размер совпадает и каждый прочитанный участник на месте.
            transaction.AddCondition(Condition.HashLengthEqual(membersKey, memberIds.Length));
            foreach (var id in memberIds)
                transaction.AddCondition(Condition.HashExists(membersKey, id));

            // Удаляем только те индексы пользователей, что указывают на это лобби.
            for (var i = 0; i < userKeys.Length; i++)
            {
                if (pointers[i] != (RedisValue)lobbyId)
                    continue;

                transaction.AddCondition(Condition.StringEqual(userKeys[i], lobbyId));
                _ = transaction.KeyDeleteAsync(userKeys[i]);
            }

            _ = transaction.KeyDeleteAsync([lobbyKey, membersKey]);
            _ = transaction.SetRemoveAsync(LobbyIdsKey, lobbyId);

            if (await transaction.ExecuteAsync())
                return;
        }

        throw new InvalidOperationException($"Failed to remove lobby '{lobbyId}' due to concurrent modifications.");
    }

    public async Task<List<Lobby>> GetLobbiesByNameAsync(string lobbyName)
    {
        var ids = await _database.SetMembersAsync(LobbyIdsKey);

        var names = await Task.WhenAll(
            ids.Select(id => _database.HashGetAsync(LobbyKey(id.ToString()), "name")));

        var matchingIds = ids
            .Zip(names, (id, name) => (Id: id.ToString(), Name: name))
            .Where(x => x.Name.HasValue &&
                        x.Name.ToString().Contains(lobbyName, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id);

        var lobbies = await Task.WhenAll(matchingIds.Select(GetLobbyByIdAsync));
        return lobbies.OfType<Lobby>().ToList();
    }

    public async Task<LobbyMember> GetLobbyMemberAsync(string lobbyId, int userId)
    {
        var value = await _database.HashGetAsync(MembersKey(lobbyId), userId);
        if (value.IsNullOrEmpty)
            throw new KeyNotFoundException($"User {userId} is not a member of lobby '{lobbyId}'.");

        return DeserializeMember(value);
    }

    private static LobbyMember DeserializeMember(RedisValue value)
    {
        var dto = JsonSerializer.Deserialize<LobbyMemberDto>(value.ToString())
                  ?? throw new InvalidOperationException("Failed to deserialize lobby member.");
        return dto.ToDomain();
    }
}

public record LobbyMemberDto
{
    public int PlayerId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsReady { get; set; }
    public string? ChosenFrog { get; set; }
}

public static class LobbyMemberMapper
{
    public static LobbyMemberDto ToDto(this LobbyMember member) => new()
    {
        PlayerId = member.UserId,
        Nickname = member.Nickname,
        IsReady = member.IsReady,
        ChosenFrog = member.ChosenHero
    };

    public static LobbyMember ToDomain(this LobbyMemberDto dto)
    {
        var lobbyMember = new LobbyMember(dto.PlayerId, dto.Nickname);
        lobbyMember.SetReady(dto.IsReady);
        lobbyMember.SetChosenHero(dto.ChosenFrog);
        return lobbyMember;
    }
}
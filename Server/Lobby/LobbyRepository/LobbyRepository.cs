using System.Collections.Concurrent;

namespace Server.Lobby.LobbyRepository;

public class LobbyRepository : ILobbyRepository
{
    // Хранилище лобби по их ID
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();

    // Быстрый маппинг: UserId -> LobbyId, чтобы не искать перебором
    private readonly ConcurrentDictionary<int, string> _userLobbyMap = new();

    public async Task AddLobbyAsync(Lobby lobby)
    {
        if (lobby == null) throw new ArgumentNullException(nameof(lobby));
        _lobbies.TryAdd(lobby.LobbyId, lobby);
    }

    public async Task RemoveLobbyAsync(string lobbyId)
    {
        _lobbies.TryRemove(lobbyId, out var lobby);
    }

    public async Task<List<Lobby>> GetLobbiesByNameAsync(string lobbyName)
    {
        // TODO: оптимизировать реализацию
        lock (_lobbies)
        {
            return _lobbies.Values.Where(l => l.LobbyName == lobbyName).ToList();
        }
    }

    public async Task<LobbyMember> GetLobbyMemberAsync(string lobbyId, int userId)
    {
        return (await GetLobbyMembersAsync(lobbyId)).First(l => l.UserId == userId);
    }

    public async Task<Lobby?> GetLobbyByIdAsync(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId)) return null;

        _lobbies.TryGetValue(lobbyId, out var lobby);
        return lobby;
    }

    public async Task<string?> FindUserLobbyAsync(int userId)
    {
        _userLobbyMap.TryGetValue(userId, out var lobbyId);
        return lobbyId;
    }

    public async Task<bool> SetLobbyMemberAsync(string lobbyId, LobbyMember member)
    {
        var userId = member.UserId;
        if (string.IsNullOrEmpty(lobbyId)) return false;
        if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return false;

        // Блокируем конкретное лобби, чтобы избежать race condition при проверке IsFull
        lock (lobby)
        {
            if (lobby.Members.FindIndex(m => m.UserId == userId) is var i && i != -1)
            {
                lobby.Members[i] = member;
                return true;
            }

            if (lobby.IsFull) return false;
            lobby.Members.Add(member);
            _userLobbyMap[userId] = lobbyId;
            return true;
        }
    }

    public async Task RemoveUserFromLobbyAsync(string lobbyId, int userId)
    {
        if (string.IsNullOrEmpty(lobbyId)) return;
        if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return;

        lock (lobby)
        {
            var member = lobby.Members.FirstOrDefault(m => m.UserId == userId);
            if (member != null)
            {
                lobby.Members.Remove(member);
                _userLobbyMap.TryRemove(userId, out _);
            }
        }
    }

    public async Task<IReadOnlyList<LobbyMember>> GetLobbyMembersAsync(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId)) return Array.Empty<LobbyMember>();
        if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return Array.Empty<LobbyMember>();

        lock (lobby)
        {
            // Возвращаем копию списка, чтобы избежать thread-safety проблем при чтении из других потоков
            return lobby.Members.ToList();
        }
    }
}
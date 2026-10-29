using Core.Match.MatchCreator;
using Microsoft.AspNetCore.SignalR;
using Server.ConnectionManager;
using Server.Lobby;
using Server.Lobby.LobbyRepository;
using Server.Lobby.LobbyService;
using Server.Match;
using Server.Match.MatchRepository;
using Server.Match.MatchService;
using Server.Matches;

namespace Server.Hub;


public class GameHub(
    ILobbyService lobbyService,
    IConnectionManager connectionManager,
    IMatchService matchService,
    ILobbyRepository lobbyRepository,
    IMatchRepository matchRepository,
    ILogger<GameHub> logger)
    : Hub<IGameHubClient>
{
    public static string GetLobbyGroupName(string lobbyId) => $"lobby_{lobbyId}";
    public static string GetMatchGroupName(string matchId) => $"match_{matchId}";


    public async Task<LobbyState> JoinLobby(string lobbyId)
    {
        var playerId = connectionManager.IdentifyUserByConnection(Context);
        logger.LogInformation($"player {playerId} joining lobby {lobbyId}");

        // Пытаемся добавить в лобби
        var canJoin = await lobbyService.AddUserToLobby(lobbyId, playerId);
        if (!canJoin) throw new HubException("Не удалось присоединиться к игре");

        var lobbyGroupName = GetLobbyGroupName(lobbyId);
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyGroupName);

        var player = await lobbyRepository.GetLobbyMemberAsync(lobbyId, playerId);
        await Clients.Group(lobbyGroupName).PlayerJoinedTheLobby(player.ToDto());

        var members = await lobbyRepository.GetLobbyMembersAsync(lobbyId);
        return new LobbyState(lobbyId, playerId, 8, members);
    }

    public async Task SetReady(bool ready, string? heroName, string? mode)
    {
        var playerId = connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = await lobbyRepository.FindUserLobbyAsync(playerId);
        if (string.IsNullOrEmpty(lobbyId)) throw new HubException("лобби не существует");

        logger.LogInformation($"player {playerId} setting ready {ready} lobby {lobbyId}");

        await lobbyService.OnMembersReadyAsync(lobbyId, playerId, ready, heroName);
        await Clients.Group(GetLobbyGroupName(lobbyId)).PlayerReadyChanged(playerId, ready);
    }

    public async Task LeaveLobby()
    {
        var playerId = connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = await lobbyRepository.FindUserLobbyAsync(playerId);
        logger.LogInformation($"player {playerId} leaving lobby {lobbyId}");

        if (!string.IsNullOrEmpty(lobbyId))
        {
            var player = await lobbyRepository.GetLobbyMemberAsync(lobbyId, playerId);
            await lobbyService.RemoveUserFromLobby(lobbyId, playerId);

            var group = GetLobbyGroupName(lobbyId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            await Clients.Group(group).PlayerLeftTheLobby(player.ToDto());
        }
    }

    public async Task HandleCommandAsync(Input input)
    {
        logger.LogInformation("Handling command");
        var playerId = connectionManager.IdentifyUserByConnection(Context);
        await matchService.HandleUserInputAsync(playerId, input);
    }

    // public async Task CloseLobbyAsync(string lobbyId)
    // {
    //     var playerId = _connectionManager.IdentifyUserByConnection(Context);
    //     _logger.LogInformation($"Closing lobby {lobbyId}");
    //     
    //     var result = _lobbyService.CloseLobbyAsync(lobbyId);
    //     await Clients.Group(lobbyId).LobbyWasClosed();
    // }

    public async Task StartMatch()
    {
        var playerId = connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = await lobbyRepository.FindUserLobbyAsync(playerId);
        if (string.IsNullOrEmpty(lobbyId)) throw new HubException("Игрок не в лобби");

        var lobby = await lobbyRepository.GetLobbyByIdAsync(lobbyId);
        if (!lobby!.EveryoneIsReady) throw new HubException("Матч не может быть начат: не все игроки готовы");

        logger.LogInformation($"player {playerId} starting match for lobby {lobbyId}");

        var playersInit = (await lobbyRepository.GetLobbyMembersAsync(lobbyId))
            .Select(m => new PlayerInit(m.UserId, m.ChosenHero))
            .ToArray();

        var (initState, matchId) = await matchService.StartMatchAsync(playersInit);
        await lobbyService.CloseLobbyAsync(lobbyId);

        foreach (var player in playersInit)
        {
            var connectionId = await connectionManager.GetConnectionId(player.PlayerId)!;
            await Groups.AddToGroupAsync(connectionId, GetMatchGroupName(matchId));
            await Groups.RemoveFromGroupAsync(connectionId, GetLobbyGroupName(lobbyId));
        }

        await Clients.Group(GetMatchGroupName(matchId)).MatchStarted(initState);
    }

    public override async Task OnConnectedAsync()
    {
        var playerId = connectionManager.IdentifyUserByConnection(Context);
        logger.LogInformation($"player {playerId} connecting to hub");

        var matchId = await matchRepository.FindUserMatchAsync(playerId);
        if (!string.IsNullOrEmpty(matchId))
        {
            var init = await matchService.OnRejoinMatchAsync(playerId);
            await Clients.Caller.MatchStarted(init);
            await Groups.AddToGroupAsync(Context.ConnectionId, GetMatchGroupName(matchId));
        }

        var prevConnectionId = await connectionManager.GetConnectionId(playerId);
        if (!string.IsNullOrEmpty(prevConnectionId))
        {
            var lobbyId = await lobbyRepository.FindUserLobbyAsync(playerId);
            if (!string.IsNullOrEmpty(lobbyId))
                await Groups.RemoveFromGroupAsync(prevConnectionId, GetLobbyGroupName(lobbyId));
            else if (!string.IsNullOrEmpty(matchId))
                await Groups.RemoveFromGroupAsync(prevConnectionId, GetMatchGroupName(matchId));
            await Clients.Client(prevConnectionId).YouWereDisconnected();
        }

        await connectionManager.Register(playerId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = connectionManager.IdentifyUserByConnection(Context);


        var isObsoleteConnection = await connectionManager.GetConnectionId(userId) != Context.ConnectionId;
        var wasInLobby = !string.IsNullOrEmpty(await lobbyRepository.FindUserLobbyAsync(userId));
        if (wasInLobby) await LeaveLobby();
        if (!isObsoleteConnection)
        {
            await connectionManager.Unregister(userId);
        }
    }
}

public record LobbyState(string LobbyId, int SelfId, int MaxPlayers, IReadOnlyList<LobbyMember> Members);
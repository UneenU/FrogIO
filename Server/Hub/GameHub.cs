using Microsoft.AspNetCore.SignalR;
using Server.ConnectionManager;
using Server.Lobby;
using Server.Lobby.LobbyRepository;
using Server.Lobby.LobbyService;
using Server.Match;
using Server.Match.MatchService;
using PlayerInfo = Core.Match.MatchCreator.PlayerInfo;

namespace Server.Hub;

public class GameHub : Hub<IGameHub>
{
    private readonly ILobbyService _lobbyService;
    private readonly IConnectionManager _connectionManager;
    private readonly IMatchService _matchService;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        ILobbyService lobbyService,
        IConnectionManager connectionManager,
        IMatchService matchService, ILobbyRepository lobbyRepository, ILogger<GameHub> logger)
    {
        _logger = logger;
        _lobbyService = lobbyService;
        _connectionManager = connectionManager;
        _matchService = matchService;
        _lobbyRepository = lobbyRepository;

        _logger.LogInformation("Hub created");
    }

    public async Task<LobbyState> JoinLobby(string lobbyId)
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        _logger.LogInformation($"player {playerId} joining lobby {lobbyId}");

        // Пытаемся добавить в лобби
        var canJoin = await _lobbyService.AddUserToLobby(lobbyId, playerId);
        if (!canJoin) throw new HubException("Не удалось присоединиться к игре");

        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        // _connectionManager.MapConnectionToGame(gamerId, lobbyId);

        var player = await _lobbyRepository.GetLobbyMemberAsync(lobbyId, playerId);
        await Clients.Group(lobbyId).PlayerJoinedTheLobby(player);

        var members = await _lobbyRepository.GetLobbyMembersAsync(lobbyId);
        return new LobbyState(lobbyId, playerId, 8, members);
    }

    public async Task SetReady(bool ready, string? heroId, string? mode)
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = await _lobbyRepository.FindUserLobbyAsync(playerId);
        
        if (lobbyId == null) throw new HubException("lobby doesn't exist");
        _logger.LogInformation($"player {playerId} setting ready {ready} lobby {lobbyId}");

        await _lobbyService.OnMembersReadyAsync(lobbyId, playerId, ready, heroId);
        
        // поменять в самом лобби значение готовности пользователя
        await Clients.Group(lobbyId).PlayerReadyChanged(playerId, ready);
    }

    public async Task LeaveLobby()
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = await _lobbyRepository.FindUserLobbyAsync(playerId);
        _logger.LogInformation($"player {playerId} leaving lobby {lobbyId}");

        if (!string.IsNullOrEmpty(lobbyId))
        {
            var player = await _lobbyRepository.GetLobbyMemberAsync(lobbyId, playerId);
            await _lobbyService.RemoveUserFromLobby(lobbyId, playerId);

            // удалить все соединения пользователя, найдя их через IConnectionManager
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
            await Clients.Group(lobbyId).PlayerLeftTheLobby(player);
        }
    }

    // пока не проверяем
    public async Task HandleCommandAsync(Input input)
    {
        _logger.LogInformation("Handling command");
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        await _matchService.HandleUserInputAsync(playerId, input);
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
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = await _lobbyRepository.FindUserLobbyAsync(playerId);
        _logger.LogInformation($"player {playerId} starting match {lobbyId}");
        
        var players = (await _lobbyRepository.GetLobbyMembersAsync(lobbyId))
            .Select(m => new PlayerInfo(m.UserId, m.ChosenHero))
            .ToArray();
        var (initState, matchId) = _matchService.StartMatch(players);
        await Clients.Group(lobbyId).MatchStarted(initState);

        await _lobbyService.CloseLobbyAsync(lobbyId);
        foreach (var player in players)
        {
            var connectionId = _connectionManager.GetConnectionId(player.playerId);
            await Groups.AddToGroupAsync(connectionId, matchId);
            await Groups.RemoveFromGroupAsync(connectionId, lobbyId);
            // убрать пользователя из группы по лобби, добавить в группу по матчу
        }
    }

    public override async Task OnConnectedAsync()
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        _logger.LogInformation($"player {playerId} connecting to hub");

        //TODO: добавить в группу по матчу, если матч уже начался

        var lobbyId = await _lobbyRepository.FindUserLobbyAsync(playerId);
        if (lobbyId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

            var player = await _lobbyRepository.GetLobbyMemberAsync(lobbyId, playerId);
            await Clients.Group(lobbyId).PlayerReturnedInTheLobby(player);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveLobby();
    }
}

public record LobbyState(string LobbyId, int SelfId, int MaxPlayers, IReadOnlyList<LobbyMember> Members);
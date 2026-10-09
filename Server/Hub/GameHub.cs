using Core.Commands;
using Microsoft.AspNetCore.SignalR;
using Server.MatchRuntime;

namespace Server;

public class GameHub : Hub<IGameHub>
{
    private readonly ILobbyService _lobbyService;
    private readonly IConnectionManager _connectionManager;
    private readonly IMatchService _matchService;
    private readonly ILobbyRepository _lobbyRepository;

    public GameHub(
        ILobbyService lobbyService,
        IConnectionManager connectionManager,
        IMatchService matchService, ILobbyRepository lobbyRepository)
    {
        _lobbyService = lobbyService;
        _connectionManager = connectionManager;
        _matchService = matchService;
        _lobbyRepository = lobbyRepository;
    }

    public async Task JoinLobby(string lobbyId)
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);

        // Проверяем, можно ли войти (через сервис)
        var canJoin = _lobbyService.AddUserToLobby(lobbyId, playerId);
        if (!canJoin) throw new HubException("Не удалось присоединиться к игре");

        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        // _connectionManager.MapConnectionToGame(gamerId, lobbyId);

        //TODO: get user info somehow
        var player = new LobbyMember(playerId, playerId);
        await Clients.Group(lobbyId).PlayerJoinedTheLobby(player);
    }

    public async Task LeaveLobby()
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = _lobbyRepository.FindUserLobby(playerId);

        if (lobbyId != null)
        {
            _lobbyService.RemoveUserFromLobby(lobbyId, playerId);

            // удалить все соединения пользователя, найдя их через IConnectionManager
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);

            var player = new LobbyMember(playerId, playerId);
            await Clients.Group(lobbyId).PlayerLeftTheLobby(player);
        }
    }

    public async Task HandleCommandAsync(Input input)
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        _matchService.HandleUserInput(playerId, input);
    }

    public async Task CloseLobbyAsync(string lobbyId)
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        var result = _lobbyService.CloseLobby(lobbyId, playerId);
        await Clients.Group(lobbyId).LobbyWasClosed();
    }

    public async Task StartMatch()
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = _lobbyRepository.FindUserLobby(playerId);
        var players = _lobbyRepository.GetLobbyMembers(lobbyId).Select(m => m.UserId).ToArray();
        var matchId = _matchService.StartMatch(players);

        foreach (var player in players)
        {
            // убрать пользователя из группы по лобби, добавить в группу по матчу
        }

        await Clients.Group(matchId).MatchStarted();
    }

    public override async Task OnConnectedAsync()
    {
        var playerId = _connectionManager.IdentifyUserByConnection(Context);
        var lobbyId = _lobbyRepository.FindUserLobby(playerId);
        if (lobbyId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

            //TODO: get user info somehow
            var player = new LobbyMember(playerId, playerId);
            await Clients.Group(lobbyId).PlayerReturnedInTheLobby(player);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
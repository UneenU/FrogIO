using Server.Lobby.LobbyRepository;

namespace Server.Lobby.LobbyService;

public class LobbyService : ILobbyService
{
    private readonly ILogger<LobbyService> _logger;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IUserRepository _userRepository;

    public LobbyService(ILogger<LobbyService> logger, ILobbyRepository lobbyRepository, IUserRepository userRepository)
    {
        _logger = logger;
        _lobbyRepository = lobbyRepository;
        _userRepository = userRepository;
    }

    public async Task<string> CreateLobby(string lobbyName, int maxPlayers)
    {
        var lobby = Lobby.NewLobby(lobbyName, maxPlayers);
        await _lobbyRepository.AddLobbyAsync(lobby);
        return lobby.LobbyId;
    }

    public async Task<bool> AddUserToLobby(string lobbyId, int userId)
    {
        var lobby = await _lobbyRepository.GetLobbyByIdAsync(lobbyId);
        var userLobby = await _lobbyRepository.FindUserLobbyAsync(userId);
        if (userLobby is not null ||
            lobby is null ||
            lobby.Members.Any(p => p.UserId == userId) ||
            lobby.IsFull)
        {
            _logger.LogInformation($"user lobby: {userLobby}, lobby: {lobbyId}, lobby is full: {lobby?.IsFull}");
            return false;
        }

        _logger.LogInformation($"user {userId} added to lobby {lobbyId}");

        var user = await _userRepository.GetUserShortInfoAsync(userId);
        var lobbyMember = new LobbyMember(userId, user!.Nickname);
        await _lobbyRepository.SetLobbyMemberAsync(lobbyId, lobbyMember);
        return true;
    }

    public async Task RemoveUserFromLobby(string lobbyId, int userId)
    {
        // лобби или пользователя может не существовать
        await _lobbyRepository.RemoveUserFromLobbyAsync(lobbyId, userId);
        var lobby = await _lobbyRepository.GetLobbyByIdAsync(lobbyId);
        if (lobby.Members.Count == 0) await CloseLobbyAsync(lobbyId);
    }

    public async Task<bool> CloseLobbyAsync(string lobbyId)
    {
        var lobby = await _lobbyRepository.GetLobbyByIdAsync(lobbyId);
        foreach (var member in lobby.Members)
        {
            await _lobbyRepository.RemoveUserFromLobbyAsync(lobbyId, member.UserId);
        }
        await _lobbyRepository.RemoveLobbyAsync(lobbyId);
        
        return true;
    }

    public async Task OnMembersReadyAsync(string lobbyId, int userId, bool ready, string heroName)
    {
        var member = await _lobbyRepository.GetLobbyMemberAsync(lobbyId, userId);
        member.SetReady(ready);
        member.SetChosenHero(heroName);
        await _lobbyRepository.SetLobbyMemberAsync(lobbyId, member);
        // var lobby = await _lobbyRepository.GetLobbyByIdAsync(lobbyId);
    }
}
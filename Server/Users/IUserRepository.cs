using Server.Data;

namespace Server;

public interface IUserRepository
{
    Task<User?> GetUserShortInfoAsync(int userId);
    Task<User?> GetUserAsync(int userId);
    Task<bool> AddUserAsync(User userInfo, CancellationToken cancellationToken);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<int> CreateUserAsync(User userInfo);
    public Task<bool> SetUsersRatingAsync(Dictionary<int, int> usersRating);
    Task<Dictionary<int, int>> GetUsersRatingAsync(IEnumerable<int> userIds);
    Task<bool> SetNewUserPasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken);
}
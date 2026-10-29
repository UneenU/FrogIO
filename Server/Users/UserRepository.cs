using Microsoft.EntityFrameworkCore;
using Npgsql;
using Server.Data;
using Server.Storage;

namespace Server;

public sealed class UserRepository : IUserRepository
{
    private readonly GameDbContext _dbContext;

    public UserRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserShortInfoAsync(int userId)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _dbContext.Users
            .Include(u => u.MatchParticipants)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> AddUserAsync(User userInfo, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(userInfo);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation
                  })
        {
            throw new ArgumentException("Пользователь с таким email уже существует");
        }

        return true;
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<int> CreateUserAsync(User userInfo)
    {
        await _dbContext.AddAsync(userInfo);
        return userInfo.UserId;
    }

    public async Task<bool> SetUsersRatingAsync(Dictionary<int, int> usersRating)
    {
        foreach (var (userId, rating) in usersRating)
        {
            await _dbContext.Users
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.Rating, rating));
        }

        return true;
    }

    public async Task<Dictionary<int, int>> GetUsersRatingAsync(IEnumerable<int> userIds)
    {
        return await _dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.Rating);
    }

    public async Task<bool> SetNewUserPasswordHashAsync(int userId, string password,
        CancellationToken cancellationToken)
    {
        await _dbContext.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PasswordHash, password), cancellationToken);
        return true;
    }
}
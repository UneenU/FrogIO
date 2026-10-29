namespace Server;

public interface IUserService
{
    public Task RegisterUserAsync(string nickname, string email, string password);
}
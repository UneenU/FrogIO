using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Server.Data;


namespace Server.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapGet("/csrf", (
                HttpContext context,
                IAntiforgery antiforgery) =>
            {
                var tokens = antiforgery.GetAndStoreTokens(context);

                return Results.Ok(new
                {
                    requestToken = tokens.RequestToken
                });
            })
            .AllowAnonymous();

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        HttpContext context,
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var nickname = request.Nickname?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        var password = request.Password;

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(nickname) ||
            nickname.Length > 32)
        {
            errors["nickname"] =
            [
                "Никнейм обязателен и должен содержать не более 32 символов."
            ];
        }

        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 256 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            errors["email"] = ["Укажите корректный email."];
        }

        if (string.IsNullOrEmpty(password) ||
            password.Length < 8 ||
            password.Length > 128)
        {
            errors["password"] =
            [
                "Пароль должен содержать от 8 до 128 символов."
            ];
        }

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        // var exists = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        var exists = await userRepository.GetUserByEmailAsync(email, cancellationToken) != null;

        if (exists)
        {
            return Results.Conflict(new
            {
                error = "Email уже занят."
            });
        }

        var user = new User
        {
            Nickname = nickname!,
            Email = email!,
            Rating = 1000,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password!);

        // db.Users.Add(user);
        try
        {
            // await db.SaveChangesAsync(cancellationToken);
            await userRepository.AddUserAsync(user, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Results.Conflict(new
            {
                error = "Email уже занят."
            });
        }

        await IssueCookieAsync(context, user);

        return Results.Created(
            "/api/auth/me",
            ToDto(user));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext context,
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrEmpty(password))
        {
            return InvalidCredentials();
        }

        var user = await userRepository.GetUserByEmailAsync(email, cancellationToken);

        if (user is null)
            return InvalidCredentials();

        var result = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
            return InvalidCredentials();

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newPassword = passwordHasher.HashPassword(user, password);
            await userRepository.SetNewUserPasswordHashAsync(user.UserId, newPassword, cancellationToken);
        }

        await IssueCookieAsync(context, user);
        
        return Results.Ok(ToDto(user));
    }

    private static async Task<IResult> LogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.NoContent();
    }

    private static IResult GetCurrentUser(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var parsedUserId))
            return Results.Unauthorized();

        return Results.Ok(new
        {
            UserId = parsedUserId,
            Nickname = principal.FindFirstValue(ClaimTypes.Name),
            Email = principal.FindFirstValue(ClaimTypes.Email),
        });
    }

    private static async Task IssueCookieAsync(HttpContext context, User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),

            new(ClaimTypes.Name, user.Nickname),

            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });
    }

    private static AuthUserDto ToDto(User user)
    {
        return new AuthUserDto(
            user.UserId,
            user.Nickname,
            user.Email,
            user.Rating);
    }

    private static IResult InvalidCredentials()
    {
        return Results.Json(
            new { error = "Неверный email или пароль." },
            statusCode: StatusCodes.Status401Unauthorized);
    }
}

public sealed record RegisterRequest(
    string Nickname,
    string Email,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record AuthUserDto(
    int UserId,
    string Nickname,
    string Email,
    int Rating);
using Core.Commands;
using Core.MapSystem;
using Core.Match.MatchCreator;
using Core.Objects;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server;
using Server.Auth;
using Server.ConnectionManager;
using Server.Data;
using Server.Hub;
using Server.Lobby.LobbyRepository;
using Server.Lobby.LobbyService;
using Server.Match;
using Server.Match.MatchRepository;
using Server.Match.MatchService;
using Server.Matches;
using Server.Storage;
using StackExchange.Redis;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    var proxyAddress = builder.Configuration["ReverseProxy:Address"];

    if (!string.IsNullOrWhiteSpace(proxyAddress))
    {
        options.KnownProxies.Add(IPAddress.Parse(proxyAddress));
    }
});

var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("FrogIO");

var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var isDevelopment = builder.Environment.IsDevelopment();

var allowedOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>();

if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    if (!isDevelopment)
    {
        throw new InvalidOperationException(
            "Frontend:AllowedOrigins must be configured in production.");
    }

    allowedOrigins =
    [
        "http://localhost:5173",
        "http://192.168.3.6:5173"
    ];
}

builder.Services.AddSignalR(options => { options.EnableDetailedErrors = isDevelopment; });


var connectionString =
    builder.Configuration.GetConnectionString("GameDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'GameDatabase' is missing.");

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // __Host- требует Secure, Path=/ и отсутствия Domain.
        // Для обычного локального HTTP используем отдельное имя.
        options.Cookie.Name = isDevelopment
            ? "FrogGame.Auth"
            : "__Host-FrogGame.Auth";

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isDevelopment
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";
        options.Cookie.IsEssential = true;

        options.ExpireTimeSpan = TimeSpan.FromHours(48);
        options.SlidingExpiration = true;

        // API должен возвращать HTTP 401/403, а не редиректы
        // на HTML-страницы логина и доступа.
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                return Task.CompletedTask;
            },

            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                return Task.CompletedTask;
            }
        };
    });

// Все конечные точки защищены по умолчанию.
// Регистрация, вход и получение CSRF-токена будут AllowAnonymous.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

// -------------------- Password hashing --------------------

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// -------------------- Antiforgery / CSRF --------------------

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";

    options.Cookie.Name = isDevelopment
        ? "FrogGame.Csrf"
        : "__Host-FrogGame.Csrf";

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isDevelopment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});

// -------------------- CORS --------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueClient", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// -------------------- Redis --------------------

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                            ?? throw new InvalidOperationException("Connection string 'Redis' is missing.");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddSingleton<IConnectionManager, AuthConnectionManager>();
builder.Services.AddSingleton<MatchNotifier>();

// -------------------- Lobby --------------------

builder.Services.AddScoped<ILobbyRepository, RedisLobbyRepository>();
builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// -------------------- Match --------------------

builder.Services.AddScoped<MapCreator>(_ =>
{
    var config = new MapConfig(400, 400,
    [
        new ObjectSpawn(typeof(WaterLily), 40)
    ]);

    return new MapCreator(config, 42);
});

builder.Services.AddScoped<IMatchRepository, RedisMatchRepository>();
builder.Services.AddScoped<IMatchHistoryRepository, MatchHistoryRepository>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IRatingSystem, RatingSystem>();

builder.Services.AddScoped<IMatchCreator>(provider =>
{
    var mapCreator = provider.GetRequiredService<MapCreator>();

    return new MatchCreator(
        mapCreator,
        1,
        new AiControllerFactory());
});

// -------------------- Other services --------------------

builder.Services.AddSingleton<IHeroCatalog, HeroCatalog>();

builder.Logging.AddConsole();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("RunMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();

    app.Logger.LogInformation("Applying database migrations");
    await dbContext.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations completed");
    return;
}

// -------------------- HTTP pipeline --------------------

app.UseForwardedHeaders();
if (!isDevelopment)
{
    app.UseHttpsRedirection();
}

// Нужен для проверки источника WebSocket-подключения.
app.UseWebSockets();

// Origin проверяется дополнительно к аутентификации.
// CORS сам по себе WebSocket не защищает.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/hubs/game"))
    {
        var origin = context.Request.Headers.Origin.ToString();

        var hasOrigin = !string.IsNullOrWhiteSpace(origin);
        var isAllowedOrigin = hasOrigin &&
                              allowedOrigins.Contains(
                                  origin,
                                  StringComparer.OrdinalIgnoreCase);

        // Для WebSocket браузера Origin обязателен.
        // Для остальных запросов Hub проверяем его, если он передан.
        if (context.WebSockets.IsWebSocketRequest && !hasOrigin ||
            hasOrigin && !isAllowedOrigin)
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return;
        }
    }

    await next();
});

app.UseRouting();

// CORS должен выполняться после маршрутизации,
// но до авторизации.
app.UseCors("VueClient");

app.UseAuthentication();
app.UseAuthorization();

// Проверяем antiforgery-токен на изменяющих запросах /api.
// GET, HEAD, OPTIONS и TRACE не проверяем.
// Hub находится вне /api и защищается отдельно.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    var isApiRequest = path.StartsWithSegments("/api");

    var isSafeMethod =
        HttpMethods.IsGet(context.Request.Method) ||
        HttpMethods.IsHead(context.Request.Method) ||
        HttpMethods.IsOptions(context.Request.Method) ||
        HttpMethods.IsTrace(context.Request.Method);

    if (!isApiRequest || isSafeMethod)
    {
        await next();
        return;
    }

    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        context.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Missing or invalid antiforgery token."
        });

        return;
    }

    await next();
});

// -------------------- Authentication endpoints --------------------

app.MapAuthEndpoints();

app.MapGet("api/user/{userId}/matches-history",
        async (IMatchHistoryRepository historyRepository,
            [FromRoute] int userId,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "page-size")] int pageSize = 10) =>
        {
            return Results.Ok(await historyRepository.GetUserMatchHistoryAsync(userId, page - 1, pageSize));
        })
    .AllowAnonymous();

// -------------------- Lobby endpoints --------------------

app.MapPost("/api/lobbies",
    async (
        ILogger<Program> logger,
        LobbyCreateRequest request,
        ILobbyService lobbyService) =>
    {
        logger.LogInformation(
            "Начата попытка создать лобби с названием {LobbyName}",
            request.Name);

        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.MaxPlayers < 1)
        {
            return Results.BadRequest();
        }

        var lobbyId = await lobbyService.CreateLobby(
            request.Name,
            request.MaxPlayers);

        logger.LogInformation(
            "Создано лобби с id {LobbyId}",
            lobbyId);

        return Results.Ok(new LobbyCreateResponse(lobbyId));
    });

app.MapGet("/api/lobbies",
    async (string lobbyName, ILobbyRepository repository) =>
    {
        var lobbies =
            await repository.GetLobbiesByNameAsync(lobbyName);

        return Results.Ok(
            lobbies.Select(l => new
            {
                l.LobbyName,
                l.LobbyId
            }));
    });

// -------------------- Heroes --------------------

app.MapGet("/api/heroes",
    (IHeroCatalog catalog) =>
        Results.Ok(catalog.GetAll()));

// -------------------- Game Hub --------------------

app.MapHub<GameHub>("/hubs/game")
    .RequireAuthorization();

app.Run();


public record LobbyCreateRequest(string Name, int MaxPlayers);

public record LobbyCreateResponse(string LobbyId);
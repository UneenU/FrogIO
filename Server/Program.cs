using Core.Commands;
using Core.MapSystem;
using Core.Match.MatchCreator;
using Core.Objects;
using MessagePack;
using MessagePack.Resolvers;
using Server.ConnectionManager;
using Server.Hub;
using Server.Lobby.LobbyRepository;
using Server.Lobby.LobbyService;
using Server.Match;
using Server.Match.MatchRepository;
using Server.Match.MatchService;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddSignalR(o => o.EnableDetailedErrors = true);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<MatchNotifier>();

builder.Services.AddScoped<ILobbyRepository, RedisLobbyRepository>();
builder.Services.AddScoped<ILobbyService, LobbyService>();

builder.Services.AddScoped<MapCreator>(_ =>
{
    MapConfig config =
        new MapConfig(400, 400, [
            new ObjectSpawn(typeof(WaterLily), 40)
        ]);
    return new MapCreator(config, 42);
});

builder.Services.AddScoped<IMatchRepository, RedisMatchRepository>();
builder.Services.AddScoped<IMatchService, MatchService>();

builder.Services.AddScoped<IMatchCreator>(provider =>
{
    var mapCreator = provider.GetService<MapCreator>();
    return new MatchCreator(mapCreator, 1, new AiControllerFactory());
});

builder.Services.AddSingleton<IHeroCatalog, HeroCatalog>();

builder.Logging.AddConsole();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapPost("/api/lobbies",
    async (ILogger<int> logger, LobbyCreateRequest request, ILobbyRepository repository, ILobbyService lobbyService) =>
    {
        logger.LogInformation($"Начата попытка создать лобби с названием {request.Name}");
        if (string.IsNullOrEmpty(request.Name) || request.MaxPlayers < 1) return Results.BadRequest();

        var lobbyId = await lobbyService.CreateLobby(request.Name, request.MaxPlayers);
        logger.LogInformation($"Было создано лобби c id {lobbyId}");

        return Results.Ok(new LobbyCreateResponse(lobbyId));
    });

app.MapGet("/api/lobbies", async (string lobbyName, ILobbyRepository repository) =>
{
    var lobbies = await repository.GetLobbiesByNameAsync(lobbyName);
    return Results.Ok(lobbies.Select(l => new { l.LobbyName, l.LobbyId }));
});

app.MapGet("/api/heroes", (IHeroCatalog catalog) => Results.Ok(catalog.GetAll()));

app.MapHub<GameHub>("/hubs/game");

app.Run();

public record LobbyCreateRequest(string Name, int MaxPlayers);

public record LobbyCreateResponse(string LobbyId);


public record HeroDto(
    string Id,
    string Name,
    string Role,
    int Hp,
    int Attack,
    int Speed,
    string Ability
);

public interface IHeroCatalog
{
    HeroDto[] GetAll();
}

public class HeroCatalog : IHeroCatalog
{
    public HeroDto[] GetAll()
    {
        return
        [
            new HeroDto("wren", "wren", "Scout", 100, 100, 10, "NONE"),
            new HeroDto("vex", "vex", "____", 100, 100, 10, "NONE"),
            new HeroDto("rook", "rook", "____", 100, 100, 10, "NONE")
        ];
    }
}
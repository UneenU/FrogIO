using System.Collections.Concurrent;
using Core;
using Core.Commands;
using Core.Map;
using Core.Match;
using Core.Match.MatchCreator;
using Core.Objects;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.SignalR;
using Server;
using Server.ConnectionManager;
using Server.Hub;
using Server.Lobby.LobbyRepository;
using Server.Lobby.LobbyService;
using Server.Match;
using Server.Match.MatchService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);
builder.Services.AddSingleton<ILobbyRepository, LobbyRepository>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<MapCreator>(provider =>
{
    MapConfig config = new MapConfig(400, 300, [new ObjectSpawn(typeof(WaterLily),   30)]); 
    return new MapCreator(config, 42);
});
builder.Services.AddSingleton<IMatchService, MatchService>();
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<IMatchCreator>(provider =>
{
    var mapCreator = provider.GetService<MapCreator>();
    return new MatchCreator(mapCreator, 42, new AiControllerFactory());
});
builder.Services.AddSingleton<IMatchRepository, MatchRepository>();
builder.Services.AddSingleton<IHeroCatalog, HeroCatalog>();
builder.Logging.AddConsole();  
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
}


app.UseHttpsRedirection();

app.MapPost("/api/lobbies", async (ILogger<int> logger, LobbyCreateRequest request, ILobbyRepository repository, ILobbyService lobbyService) =>
{
    logger.LogInformation($"Начата попытка создать лобби с названием {request.name}");
    if (string.IsNullOrEmpty(request.name) || request.maxPlayers < 1) return Results.BadRequest();
    
    var lobbyId = await lobbyService.CreateLobby(request.name, request.maxPlayers);
    logger.LogInformation($"Было создано лобби c id {lobbyId}");
    
    return Results.Ok(new LobbyCreateResponse(lobbyId));
});

app.MapGet("/api/lobbies", async (string lobbyName, ILobbyRepository repository) =>
{
    var lobbies = await repository.GetLobbiesByNameAsync(lobbyName);
    return Results.Ok(lobbies.Select(l => new {l.LobbyName, l.LobbyId}));
});

app.MapGet("/api/heroes", (IHeroCatalog catalog) => Results.Ok(catalog.GetAll()));

app.MapHub<GameHub>("/hubs/game");

app.Run();

public record LobbyCreateRequest(string name, int maxPlayers);

public record LobbyCreateResponse(string lobbyId);


public record HeroDto(
    string Id,          // "wren", "rook", "vex": стабильный ключ, по нему клиент подбирает картинку
    string Name,        // "Wren"
    string Role,        // "Scout" (клиент покажет "THE SCOUT")
    int Hp,
    int Attack,
    int Speed,
    string Ability // "Ghost step" + описание
);

public interface IHeroCatalog
{
    HeroDto[] GetAll();
}

public class HeroCatalog : IHeroCatalog
{
    public HeroDto[] GetAll()
    {
        return [new HeroDto("wren", "wren", "Scout", 100, 100, 100, "NONE"), new HeroDto("vex", "vex", "____", 1020, 10, 10, "NONE")];
    }
}
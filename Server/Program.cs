using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Server;

var builder = WebApplication.CreateBuilder(args);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

// создает лобби и возвращает его id и метаданные клиенту
app.MapPost("/api/lobbies", (LobbyCreateRequest request, ILobbyRepository repository) =>
{
    var lobby = Lobby.NewLobby(request.maxPlayers);
    repository.AddLobby(lobby);
    return Results.Ok(new LobbyCreateResponse(lobby.LobbyId));
});

app.MapPost("/api/lobbies/join", (LobbyJoinRequest request) =>
{
    // if (!storage.Sessions.TryGetValue(lobbyId, out var session)) return Results.NotFound("Игра не найдена");
    var lobbyId = request.lobbyId;
    var gamerId = Guid.NewGuid().ToString();
    return Results.Ok(new LobbyJoinResponse( lobbyId, "Можно подключаться"));
});

app.Run();

public record LobbyCreateRequest(int maxPlayers);

public record LobbyCreateResponse(string LobbyId);

public record LobbyJoinResponse(string LobbyId, string Message);

public record LobbyJoinRequest(string lobbyId);
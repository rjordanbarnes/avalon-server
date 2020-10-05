using Avalon.Server.Model;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalon.Server.Hubs
{
    public class MainHub : Hub
    {
        public List<Game> games;

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine(Context.ConnectionId + " connected.");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            // TODO: Leave all games that this user is in
            Console.WriteLine(Context.ConnectionId + " disconnected.");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task CreateGame(string username)
        {
            Player host = new Player(Context.ConnectionId, username);
            Game game = new Game(host);
            games.Add(game);
            await Groups.AddToGroupAsync(host.connectionId, game.gameId);
        }

        public async Task JoinGame(string gameId, string username)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            game.addPlayer(Context.ConnectionId, username);

            if (game.containsConnection(Context.ConnectionId))
            {
                // Successfully added the player.
                await Groups.AddToGroupAsync(Context.ConnectionId, game.gameId);
            }
        }

        public async Task LeaveGame(string gameId)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            game.removePlayer(Context.ConnectionId);

            if (!game.containsConnection(Context.ConnectionId))
            {
                // Successfully removed the player.
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            }
        }
    }
}
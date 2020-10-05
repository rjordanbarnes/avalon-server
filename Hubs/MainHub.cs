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

            Player player = new Player(Context.ConnectionId, username);
            game.addPlayer(player);

            if (game.players.Contains(player))
            {
                // Successfully added the player.
                await Groups.AddToGroupAsync(player.connectionId, game.gameId);
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

            if (game.players.Find(player => player.connectionId.Equals(Context.ConnectionId)) == null)
            {
                // Successfully removed the player.
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            }
        }
    }
}
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

        // Creates a new game. The host will have the passed username.
        public async Task CreateGame(string username)
        {
            Player host = new Player(Context.ConnectionId, username);
            Game game = new Game(host);
            games.Add(game);
            await Groups.AddToGroupAsync(host.connectionId, game.gameId);
        }

        // Joins a game. The user will have the passed username.
        public async Task JoinGame(string gameId, string username)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            game.AddPlayer(Context.ConnectionId, username);

            if (game.ContainsPlayer(Context.ConnectionId))
            {
                // Successfully added the player.
                await Groups.AddToGroupAsync(Context.ConnectionId, game.gameId);
            }
        }

        // Leaves a game.
        public async Task LeaveGame(string gameId)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            if (!game.ContainsPlayer(Context.ConnectionId))
            {
                // Not in the game
                return;
            }

            game.RemovePlayer(Context.ConnectionId);

            if (!game.ContainsPlayer(Context.ConnectionId))
            {
                // Successfully removed the player.
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            }
        }

        // Starts a game. Player must be host.
        public async Task StartGame(string gameId)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            if (!game.ContainsPlayer(Context.ConnectionId))
            {
                // Not in the game
                return;
            }

            if (!game.host.connectionId.Equals(Context.ConnectionId))
            {
                // Not the host
                return;
            }

            game.Start();
        }

        // Toggles whether the passed Player is in the party. Player must be leader.
        public async Task ToggleParty(string gameId, string username)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            if (!game.ContainsPlayer(Context.ConnectionId))
            {
                // Not in the game
                return;
            }

            if (!game.leader.connectionId.Equals(Context.ConnectionId))
            {
                // Not the leader
                return;
            }

            game.ToggleParty(username);
        }

        // Confirms the current party. Player must be leader.
        public async Task ConfirmParty(string gameId)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            if (!game.ContainsPlayer(Context.ConnectionId))
            {
                // Not in the game
                return;
            }

            if (!game.leader.connectionId.Equals(Context.ConnectionId))
            {
                // Not the leader
                return;
            }

            game.ConfirmParty();
        }

        // Approves or disapproves of the current party.
        public async Task ApproveParty(string gameId, bool approve)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            if (!game.ContainsPlayer(Context.ConnectionId))
            {
                // Not in the game
                return;
            }

            game.ApproveParty(Context.ConnectionId, approve);
        }

        // Votes to succeed or fail the quest.
        public async Task SucceedQuest(string gameId, bool success)
        {
            Game game = games.Find(game => game.gameId.Equals(gameId));

            if (game == null)
            {
                // No such game
                return;
            }

            if (!game.ContainsPlayer(Context.ConnectionId))
            {
                // Not in the game
                return;
            }

            game.SucceedQuest(Context.ConnectionId, success);
        }
    }
}
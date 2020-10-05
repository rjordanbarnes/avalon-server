using System.Collections.Generic;

namespace Avalon.Server.Model
{
    public class Game
    {
        private static readonly int MAX_PLAYERS = 10;

        public string gameId { get; }
        public Player host { get { return players[0]; } set { players[0] = value; } }
        public List<Player> players { get; }

        public Game(Player host)
        {
            this.gameId = "TEST";
            this.host = host;
        }

        public void addPlayer(string connectionId, string username)
        {
            if (players.Find(player => player.connectionId.Equals(connectionId)) != null)
            {
                // Connection is already in game.
                return;
            }

            if (players.Find(player => player.name.Equals(username)) != null)
            {
                // Name already in use.
                return;
            }

            if (players.Count >= Game.MAX_PLAYERS)
            {
                // Game is full.
                return;
            }

            this.players.Add(new Player(connectionId, username));
        }

        public void removePlayer(string connectionId)
        {
            if (host.connectionId.Equals(connectionId))
            {
                // Host is leaving!
            }

            this.players.RemoveAll(player => player.connectionId.Equals(connectionId));
        }

        public bool containsConnection(string connectionId)
        {
            return this.players.Find(player => player.connectionId.Equals(connectionId)) != null;
        }
    }
}
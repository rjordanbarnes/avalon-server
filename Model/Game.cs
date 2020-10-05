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

        internal void addPlayer(Player player)
        {
            if (players.Contains(player))
            {
                // No work, player is already in game.
                return;
            }

            if (players.Count >= Game.MAX_PLAYERS)
            {
                // Game is full.
                return;
            }

            this.players.Add(player);
        }

        internal void removePlayer(string connectionId)
        {
            if (host.connectionId.Equals(connectionId))
            {
                // Host is leaving!
            }

            this.players.RemoveAll(player => player.connectionId.Equals(connectionId));
        }
    }
}
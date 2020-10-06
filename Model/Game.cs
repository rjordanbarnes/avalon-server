using System;
using System.Collections.Generic;

namespace Avalon.Server.Model
{
    public class Game
    {
        private static readonly int MAX_PLAYERS = 10;

        public string gameId { get; }
        public Player host { get { return players[0]; } set { players[0] = value; } }
        public List<Player> players { get; }
        public GamePhase gamePhase { get; }

        public Game(Player host)
        {
            this.gameId = "TEST";
            this.host = host;
            this.gamePhase = GamePhase.Lobby;
        }

        public void AddPlayer(string connectionId, string username)
        {
            if (!gamePhase.Equals(GamePhase.Lobby))
            {
                // Not in lobby.
            }

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

        public void RemovePlayer(string connectionId)
        {
            if (!gamePhase.Equals(GamePhase.Lobby))
            {
                // Not in lobby.
            }

            if (host.connectionId.Equals(connectionId))
            {
                // Host is leaving!
            }

            this.players.RemoveAll(player => player.connectionId.Equals(connectionId));
        }

        public void Start()
        {
            if (!gamePhase.Equals(GamePhase.Lobby))
            {
                // Not in lobby.
            }

            throw new NotImplementedException();
        }

        public bool ContainsConnection(string connectionId)
        {
            return this.players.Find(player => player.connectionId.Equals(connectionId)) != null;
        }
    }
}
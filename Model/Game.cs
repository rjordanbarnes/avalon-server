using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalon.Server.Model
{
    public class Game
    {
        private static readonly int MIN_PLAYERS = 5;
        private static readonly int MAX_PLAYERS = 10;
        private Random random = new Random();

        public string gameId { get; }
        public Player host { get; private set; }
        public Player leader { get; private set; }
        public List<Player> players { get; }
        public List<Team> questResults { get; }
        public GamePhase gamePhase { get; private set; }

        public Game(Player host)
        {
            this.gameId = "TEST";
            this.host = host;
            this.players.Add(host);
            this.gamePhase = GamePhase.Lobby;
        }

        public void AddPlayer(string connectionId, string username)
        {
            if (!gamePhase.Equals(GamePhase.Lobby))
            {
                // Not in lobby.
                return;
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
                return;
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
                return;
            }

            if (players.Count < MIN_PLAYERS)
            {
                // Not enough players.
                return;
            }

            // Assign random roles
            List<Role> roles = new List<Role>();
            int numEvil = (int)Math.Ceiling(this.players.Count / 3.0);
            int numGood = this.players.Count - numEvil;

            for (int i = 0; i < numEvil; i++)
            {
                roles.Add(Role.MinionOfMordred);
            }

            for (int i = 0; i < numGood; i++)
            {
                roles.Add(Role.ServantOfArthur);
            }

            roles = roles.OrderBy(a => random.Next()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                players[i].role = roles[i];
            }

            // Assign random leader
            this.leader = this.players[random.Next(players.Count)];

            this.gamePhase = GamePhase.PartySelection;
        }

        public bool ContainsConnection(string connectionId)
        {
            return this.players.Find(player => player.connectionId.Equals(connectionId)) != null;
        }
    }
}
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

        public string gameId { get; }                       // Identifier for this game

        // Player trackers.
        public List<Player> players { get; }                // Contains all players, including the host
        public Player host { get; private set; }            // Current host. Must be contained in players
        public Player leader { get; private set; }          // Current leader. Must be contained in players
        public List<Player> questParty { get; }             // Players in the current quest party.

        // Round trackers.
        public List<Team> questResults { get; }             // Which team has won each quest
        public int gameRound { get; private set; }          // Current quest
        public GamePhase gamePhase { get; private set; }    // Current game phase

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
                // TODO: Host is leaving!
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

            this.gameRound = 1;
            this.gamePhase = GamePhase.PartySelection;
        }

        public void ToggleParty(string username)
        {
            Player player = players.Find(player => player.name.Equals(username));

            if (player == null)
            {
                // Not a player.
                return;
            }

            if (!gamePhase.Equals(GamePhase.PartySelection))
            {
                // Not in party selection phase.
                return;
            }

            if (this.questParty.Contains(player))
            {
                this.questParty.Remove(player);
            }
            else if (this.questParty.Count < GetPartyCount(this.gameRound, this.players.Count))
            {
                this.questParty.Add(player);
            }
        }

        public void ConfirmParty()
        {
            if (!gamePhase.Equals(GamePhase.PartySelection))
            {
                // Not in party selection phase.
                return;
            }

            if (this.questParty.Count != GetPartyCount(this.gameRound, this.players.Count))
            {
                // Don't have the required number of party members.
                return;
            }

            this.gamePhase = GamePhase.PartyVote;
        }

        public bool ContainsPlayer(string connectionId)
        {
            return this.players.Find(player => player.connectionId.Equals(connectionId)) != null;
        }

        public static int GetPartyCount(int questNumber, int playerCount)
        {
            // Indexed by [Quest# - 1][PlayerCount - MIN_PLAYERS]
            // Table taken from Team Building Phase section of Avalon rulebook
            int[,] partyCountTable = new int[,] {
                {2, 2, 2, 3, 3, 3},
                {3, 3, 3, 4, 4, 4},
                {2, 4, 3, 4, 4, 4},
                {3, 3, 4, 5, 5, 5},
                {3, 4, 4, 5, 5, 5}
            };

            return partyCountTable[questNumber - 1, playerCount - MIN_PLAYERS];
        }
    }
}
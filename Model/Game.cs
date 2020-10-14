using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalon.Server.Model
{
    public class Game
    {
        private static readonly int MIN_PLAYERS = 5;
        private static readonly int MAX_PLAYERS = 10;
        private static readonly int MAX_REJECTIONS = 5;
        private Random random = new Random();

        public string gameId { get; }                               // Identifier for this game

        // Player trackers.
        public List<Player> players { get; }                        // Players in clockwise order, including the host
        public Player host { get; private set; }                    // Current host. Must be contained in players
        public Player leader { get; private set; }                  // Current leader. Must be contained in players
        public List<Player> questParty { get; }                     // Players in the current quest party.
        public Dictionary<Player, bool> questVotes { get; }         // Tracks quest party approvals and quest successes, depending on game phase.

        // Round trackers.
        public List<Team> questResults { get; }                     // Which team has won each quest
        public int gameRound { get; private set; }                  // Current quest
        public GamePhase gamePhase { get; private set; }            // Current game phase
        public int numQuestRejections { get; private set; }         // Number of times the quest selection has been rejected for the current round

        public Game(Player host)
        {
            this.gameId = "TEST";
            this.players = new List<Player>();
            this.players.Add(host);
            this.host = host;
            this.leader = null;
            this.questParty = new List<Player>();
            this.questVotes = new Dictionary<Player, bool>();
            this.questResults = new List<Team>();
            this.gameRound = 1;
            this.gamePhase = GamePhase.Lobby;
            this.numQuestRejections = 0;
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
            this.numQuestRejections = 0;
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

        public void ApproveParty(string connectionId, bool approve)
        {
            Player player = players.Find(player => player.connectionId.Equals(connectionId));

            if (player == null)
            {
                // Connection not in the game.
                return;
            }

            if (!gamePhase.Equals(GamePhase.PartyVote))
            {
                // Not in party vote phase.
                return;
            }

            questVotes[player] = approve;

            if (questVotes.Count == players.Count)
            {
                // All of the votes are in.
                int numApprove = 0;

                foreach (bool vote in questVotes.Values)
                {
                    if (vote)
                    {
                        numApprove++;
                    }
                }

                if (numApprove > this.players.Count / 2.0)
                {
                    // Approved!
                    this.gamePhase = GamePhase.Quest;
                    this.questVotes.Clear();
                    this.numQuestRejections = 0;
                }
                else
                {
                    // Rejected!
                    this.numQuestRejections++;

                    if (this.numQuestRejections >= MAX_REJECTIONS)
                    {
                        // TODO: Too many rejections, evil win!
                        return;
                    }

                    // Find next leader.
                    for (int i = 0; i < this.players.Count; i++)
                    {
                        if (this.leader.Equals(this.players[i]))
                        {
                            this.leader = this.players[i + 1 % this.players.Count];
                            break;
                        }
                    }

                    // Reset for a new team building phase
                    questParty.Clear();
                    questVotes.Clear();
                    gamePhase = GamePhase.PartySelection;
                }
            }
        }

        public void SucceedQuest(string connectionId, bool success)
        {
            Player player = players.Find(player => player.connectionId.Equals(connectionId));

            if (player == null)
            {
                // Connection not in the game.
                return;
            }

            if (!gamePhase.Equals(GamePhase.PartyVote))
            {
                // Not in party vote phase.
                return;
            }

            if (!questParty.Contains(player))
            {
                // Player isn't in quest party.
                return;
            }

            questVotes[player] = success;

            if (questVotes.Count == questParty.Count)
            {
                // All votes are in.
                gamePhase = GamePhase.QuestResults;
            }
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
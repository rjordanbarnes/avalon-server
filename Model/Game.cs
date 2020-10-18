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

        public string gameId { get; }                                       // Identifier for this game.

        // Persist across rounds.
        public List<Player> players { get; }                                // Players in clockwise order, including the host.
        public Player host { get; private set; }                            // Current host. Must be contained in players.
        public GamePhase gamePhase { get; private set; }                    // Current game phase.
        public int gameRound { get; private set; }                          // Current quest.
        public List<Loyalty> questResults { get; private set; }             // Which loyalty has won each quest.

        // Reset from round to round.
        public Player leader { get; private set; }                          // Current leader. Must be contained in players.
        public List<Player> team { get; private set; }                      // Players in the current quest team.
        public Dictionary<Player, bool> questVotes { get; private set; }    // Tracks quest team approvals and quest successes, depending on game phase.
        public List<bool> revealedQuestVotes { get; private set; }          // Quest votes that have been revealed by the leader.
        public int numTeamRejections { get; private set; }                  // Number of times the team selection has been rejected for the current round.

        public Game(Player host)
        {
            this.gameId = "TEST";
            this.players = new List<Player>();
            this.players.Add(host);
            this.host = host;
            this.ResetToLobby();
        }

        private void ResetToLobby()
        {
            this.gamePhase = GamePhase.Lobby;
            this.gameRound = 1;
            this.questResults = new List<Loyalty>();
            this.leader = null;
            this.team = new List<Player>();
            this.questVotes = new Dictionary<Player, bool>();
            this.revealedQuestVotes = new List<bool>();
            this.numTeamRejections = 0;
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
            this.gamePhase = GamePhase.TeamBuilding;
            this.numTeamRejections = 0;
        }

        public void ToggleTeam(string username)
        {
            Player player = players.Find(player => player.name.Equals(username));

            if (player == null)
            {
                // Not a player.
                return;
            }

            if (!gamePhase.Equals(GamePhase.TeamBuilding))
            {
                // Not in team building phase.
                return;
            }

            if (this.team.Contains(player))
            {
                this.team.Remove(player);
            }
            else if (this.team.Count < GetTeamCount(this.gameRound, this.players.Count))
            {
                this.team.Add(player);
            }
        }

        public void ConfirmTeam()
        {
            if (!gamePhase.Equals(GamePhase.TeamBuilding))
            {
                // Not in team building phase.
                return;
            }

            if (this.team.Count != GetTeamCount(this.gameRound, this.players.Count))
            {
                // Don't have the required number of team members.
                return;
            }

            this.gamePhase = GamePhase.TeamVote;
        }

        public void ApproveTeam(string connectionId, bool approve)
        {
            Player player = players.Find(player => player.connectionId.Equals(connectionId));

            if (player == null)
            {
                // Connection not in the game.
                return;
            }

            if (!gamePhase.Equals(GamePhase.TeamVote))
            {
                // Not in team vote phase.
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
                    this.numTeamRejections = 0;
                }
                else
                {
                    // Rejected!
                    this.numTeamRejections++;

                    // Game could be over due to too many rejections
                    if (IsGameOver())
                    {
                        ResetToLobby();
                    }
                    else
                    {
                        // Game not over, find next leader.
                        this.leader = GetNextClockwisePlayer(this.leader);

                        // Reset for a new team building phase
                        team.Clear();
                        questVotes.Clear();
                        gamePhase = GamePhase.TeamBuilding;
                    }
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

            if (!gamePhase.Equals(GamePhase.TeamVote))
            {
                // Not in team vote phase.
                return;
            }

            if (!team.Contains(player))
            {
                // Player isn't in quest team.
                return;
            }

            questVotes[player] = success;

            if (questVotes.Count == team.Count)
            {
                // All votes are in.
                gamePhase = GamePhase.QuestResults;
            }
        }

        public void RevealQuestResult()
        {
            if (!gamePhase.Equals(GamePhase.QuestResults))
            {
                // Not in quest results phase.
                return;
            }

            // Get random quest result and track it.
            Player randomQuestVote = this.questVotes.Keys.ElementAt(random.Next(this.questVotes.Count));
            this.revealedQuestVotes.Add(this.questVotes[randomQuestVote]);
            this.questVotes.Remove(randomQuestVote);

            if (this.questVotes.Count <= 0)
            {
                // Quest results are over

                bool questFailed = false;
                int numberOfFailures = 0;

                foreach (bool result in this.revealedQuestVotes)
                {
                    if (!result)
                    {
                        numberOfFailures++;
                    }
                }

                if (this.gameRound == 4 && this.players.Count >= 7)
                {
                    // This quest requires at least two fails to count as a failure.
                    questFailed = numberOfFailures >= 2;
                }
                else
                {
                    questFailed = numberOfFailures >= 1;
                }

                if (questFailed)
                {
                    this.questResults.Add(Loyalty.Evil);
                }
                else
                {
                    this.questResults.Add(Loyalty.Good);
                }

                if (IsGameOver())
                {
                    ResetToLobby();
                }
                else
                {
                    // Game not over, go to next round.
                    gameRound++;
                    gamePhase = GamePhase.TeamBuilding;
                    this.leader = GetNextClockwisePlayer(this.leader);
                    this.team.Clear();
                    this.questVotes.Clear();
                    this.revealedQuestVotes.Clear();
                    this.numTeamRejections = 0;
                }
            }
        }

        public bool IsGameOver()
        {
            if (this.numTeamRejections >= MAX_REJECTIONS)
            {
                // Too many rejections, evil win!
                return true;
            }

            int numGoodRounds = 0;
            int numEvilRounds = 0;

            foreach (Loyalty round in questResults)
            {
                if (round.Equals(Loyalty.Good))
                {
                    numGoodRounds++;
                }
                else
                {
                    numEvilRounds++;
                }
            }

            return numGoodRounds >= 3 || numEvilRounds >= 3;
        }

        public bool ContainsPlayer(string connectionId)
        {
            return this.players.Find(player => player.connectionId.Equals(connectionId)) != null;
        }

        public Player GetNextClockwisePlayer(Player player)
        {
            for (int i = 0; i < this.players.Count; i++)
            {
                if (player.Equals(this.players[i]))
                {
                    return this.players[i + 1 % this.players.Count];
                }
            }

            return null;
        }

        public static int GetTeamCount(int questNumber, int playerCount)
        {
            // Indexed by [Quest# - 1][PlayerCount - MIN_PLAYERS]
            // Table taken from Team Building Phase section of Avalon rulebook
            int[,] teamCountTable = new int[,] {
                {2, 2, 2, 3, 3, 3},
                {3, 3, 3, 4, 4, 4},
                {2, 4, 3, 4, 4, 4},
                {3, 3, 4, 5, 5, 5},
                {3, 4, 4, 5, 5, 5}
            };

            return teamCountTable[questNumber - 1, playerCount - MIN_PLAYERS];
        }
    }
}
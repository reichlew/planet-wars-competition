﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using PlanetWars.DemoAgent;
using PlanetWars.Shared;

namespace PlanetWars.Server
{
    public interface IGame
    {
        LogonResult LogonPlayer(string playerName);

        void Update(DateTime currentTime);

        void Start();

        void Stop();
    }

    public class Game : IGame
    {
        private static int _MAXID = 1;
        private int _MAXPLAYERID = 1;
        private int _MAXFLEETID = 0;

        public static readonly long START_DELAY = 2000; // ms
        public static readonly long MAX_WAIT = 60000; //ms
        public static readonly long PLAYER_TURN_LENGTH = 700; // ms
        public static readonly long SERVER_TURN_LENGTH = 200; // ms
        public static readonly int MAX_TURN = 200; // default 200 turns

        private readonly MapGenerator mapGenerator = new MapGenerator();

        public static bool IsRunningLocally = HttpContext.Current.Request.IsLocal;
        public bool Running { get; private set; }
        public int Id { get; private set; }
        public int Turn { get; private set; }
        public bool Waiting { get; internal set; }
        public bool GameOver { get; private set; }
        public int TIME_TO_WAIT { get; private set; }
        public bool Processing { get; private set; }

        private HighFrequencyTimer _gameLoop = null;
        public ConcurrentDictionary<string, Player> Players = new ConcurrentDictionary<string, Player>();
        public ConcurrentDictionary<string, Player> AuthTokens = new ConcurrentDictionary<string, Player>();

        private List<Planet> _planets = new List<Planet>();
        private IEnumerable<Planet> Planets => _planets.ToList();

        private List<Fleet> _fleets = new List<Fleet>();
        private IEnumerable<Fleet> Fleets => _fleets.ToList();

        public List<int> PlayerAScoreOverTime = new List<int>();
        public List<int> PlayerBScoreOverTime = new List<int>();

        private DateTime gameStart;
        private DateTime endPlayerTurn;
        private DateTime endServerTurn;
        private DateTime gameQuit;

        public string Status { get; set; }

        private object synclock = new object();

        public Game(MapGenerationOption mapGeneration)
        {
            Id = _MAXID++;

            Turn = 0;
            Running = false;
            Waiting = true;
            _gameLoop = new HighFrequencyTimer(60, this.Update);
            GenerateMap(mapGeneration);

            gameQuit = DateTime.UtcNow.AddMilliseconds(MAX_WAIT);
            UpdateTimeInfo(DateTime.UtcNow.AddMilliseconds(START_DELAY));
        }

        public void UpdateTimeInfo(DateTime gameStart)
        {
            this.gameStart = gameStart;
            endPlayerTurn = gameStart.AddMilliseconds(PLAYER_TURN_LENGTH);
            endServerTurn = endPlayerTurn.AddMilliseconds(SERVER_TURN_LENGTH);
        }

        public void SetPlanets(List<Planet> planets)
        {
            this._planets = planets;
        }

        public void SetFleets(List<Fleet> fleets)
        {
            this._fleets = fleets;
        }

        public List<Fleet> GetFleets()
        {
            return this._fleets;
        }

        private void GenerateMap(MapGenerationOption mapGeneration)
        {
            _planets = mapGenerator.GenerateMap(mapGeneration);
        }

        public MoveResult MoveFleet(MoveRequest request)
        {
            var result = new MoveResult();

            // non-zero ships
            if (request.NumberOfShips <= 0)
            {
                result.Success = false;
                result.Message = "Can't send a zero fleet";
                return result;
            }

            var validSourcePlanets = _getPlanetsForPlayer(request.AuthToken);

            // A planet of the requested source ID exists, belongs to that planer, AND it has enough ships
            var sourceValid = validSourcePlanets.FirstOrDefault(p => p.Id == request.SourcePlanetId && request.NumberOfShips <= p.NumberOfShips);

            // A planet of the requested destination ID exists
            var destinationValid = Planets.FirstOrDefault(p => p.Id == request.DestinationPlanetId);

            if (sourceValid != null && destinationValid != null)
            {
                lock (synclock)
                {
                    // Subtract ships from planet
                    sourceValid.NumberOfShips -= request.NumberOfShips;

                    // Build fleet
                    var newFleet = new Fleet()
                    {
                        Id = _MAXFLEETID++,
                        OwnerId = _authTokenToId(request.AuthToken),
                        Source = sourceValid,
                        Destination = destinationValid,
                        NumberOfShips = request.NumberOfShips,
                        NumberOfTurnsToDestination = (int)Math.Ceiling(sourceValid.Position.Distance(destinationValid.Position))
                    };

                    _fleets.Add(newFleet);

                    result.Fleet = Mapper.Map<Shared.Fleet>(newFleet);
                    result.Success = true;
                }
            }
            else
            {
                result.Success = false;
                result.Message = "Invalid move command, check if the planet of the requested source/dest ID exists, belongs to that player, AND it has enough ships.";
            }

            return result;
        }

        private int _authTokenToId(string authToken)
        {
            var player = Players.Values.Where(p => p.AuthToken == authToken).FirstOrDefault();
            if (player != null)
            {
                return player.Id;
            }
            else
            {
                throw new ArgumentException("Invalid auth token, no player exists for that auth token!!!!");
            }
        }

        private IEnumerable<Planet> _getPlanetsForPlayer(string authToken)
        {
            var id = _authTokenToId(authToken);
            return _getPlanetsForPlayer(id);
        }

        private IEnumerable<Planet> _getPlanetsForPlayer(int id)
        {
            return Planets.Where(p => p.OwnerId == id);
        }

        private IEnumerable<Fleet> _getFleetsForPlayer(string authToken)
        {
            var id = _authTokenToId(authToken);
            return _getFleetsForPlayer(id);
        }

        private Player _getPlayerForId(int id)
        {
            return Players.Values.FirstOrDefault(p => p.Id == id);
        }

        private IEnumerable<Fleet> _getFleetsForPlayer(int id)
        {
            return Fleets.Where(f => f.OwnerId == id);
        }

        private int _getPlayerScore(int id)
        {
            return _getFleetsForPlayer(id).Sum(f => f.NumberOfShips) + _getPlanetsForPlayer(id).Sum(p => p.NumberOfShips);
        }

        public LogonResult LogonPlayer(string playerName)
        {
            var result = new LogonResult();
            var newPlayer = new Player()
            {
                AuthToken = System.Guid.NewGuid().ToString(),
                PlayerName = playerName,
                Id = _MAXPLAYERID++
            };

            var playerAdded = Players.TryAdd(playerName + newPlayer.AuthToken, newPlayer);
            var authTokenAdded = AuthTokens.TryAdd(newPlayer.AuthToken, newPlayer);

            if (playerAdded && authTokenAdded)
            {
                System.Diagnostics.Debug.WriteLine("Player logon [{0}]:[{1}]", newPlayer.PlayerName, newPlayer.AuthToken);
            }

            result.AuthToken = newPlayer.AuthToken;
            result.Id = newPlayer.Id;
            result.GameId = Id;
            result.GameStart = this.gameStart;
            result.Success = true;

            return result;
        }

        public void StartDemoAgent(int gameId, bool advanced)
        {
            if (advanced)
            {
                var agentTask = Task.Factory.StartNew(async () =>
                {
                    var sweetDemoAgent = new AdvancedAgent(gameId);
                    await sweetDemoAgent.Start();
                });
            }
            else
            {
                var agentTask = Task.Factory.StartNew(async () =>
                {
                    var sweetDemoAgent = new Agent(gameId);
                    await sweetDemoAgent.Start();
                });
            }
        }

        public void Start()
        {
            Running = true;
            _gameLoop.Start();
        }

        public void Stop()
        {
            Running = false;
            _gameLoop.Stop();
        }

        public void Update(DateTime currentTime)
        {
            if (this.Waiting)
            {
                if (currentTime > gameStart)
                {
                    if (Players.Count == 2)
                    {
                        this.Waiting = false;
                    }
                    else
                    {
                        if (currentTime > gameQuit)
                        {
                            this.Waiting = false;
                            this.Status = "Nobody joined your game, I guess that means you win.";
                            GameOver = true;
                        }
                        else
                        {
                            UpdateTimeInfo(gameStart.AddSeconds(2));
                        }
                    }
                }
                return;
            }

            if (GameOver)
            {
                this.Stop();
                return;
            }

            // check if we are in the server window
            if (currentTime >= endPlayerTurn)
            {
                // server processing
                Processing = true;

                // Grow ships on planets
                foreach (var planet in Planets)
                {
                    // if the planet is not controlled by neutral update
                    if (planet.OwnerId != -1)
                    {
                        planet.NumberOfShips += planet.GrowthRate;
                    }
                }

                // Send fleets
                foreach (var fleet in Fleets)
                {
                    // travel 1 unit distance each turn
                    fleet.NumberOfTurnsToDestination--;
                }

                // Resolve planet battles
                foreach (var planet in Planets)
                {
                    var combatants = new Dictionary<int, int>();
                    combatants.Add(planet.OwnerId, planet.NumberOfShips);
                    // find fleets destined for this planet
                    var fleets = Fleets.Where(f => f.Destination.Id == planet.Id && f.NumberOfTurnsToDestination <= 0);
                    foreach (var fleet in fleets)
                    {
                        if (combatants.ContainsKey(fleet.OwnerId))
                        {
                            combatants[fleet.OwnerId] += fleet.NumberOfShips;
                        }
                        else
                        {
                            combatants.Add(fleet.OwnerId, fleet.NumberOfShips);
                        }
                    }

                    if (fleets.Count() <= 0)
                    {
                        continue;
                    }

                    KeyValuePair<int, int> second = new KeyValuePair<int, int>(1, 0);
                    KeyValuePair<int, int> winner = new KeyValuePair<int, int>(2, 0);
                    foreach (var keyval in combatants)
                    {
                        if (keyval.Value > second.Value)
                        {
                            if (keyval.Value > winner.Value)
                            {
                                second = winner;
                                winner = keyval;
                            }
                            else
                            {
                                second = keyval;
                            }
                        }
                    }

                    if (winner.Value > second.Value)
                    {
                        planet.NumberOfShips = winner.Value - second.Value;
                        planet.OwnerId = winner.Key;
                    }
                    else
                    {
                        planet.NumberOfShips = 0;
                    }
                }

                // remove finished fleets
                lock (synclock)
                {
                    _fleets.RemoveAll(f => f.NumberOfTurnsToDestination <= 0);
                }

                // Check game over conditions
                var player1NoPlanets = !Planets.Any(p => p.OwnerId == 1);
                var player1NoShips = !Fleets.Any(f => f.OwnerId == 1);

                var player2NoPlanet = !Planets.Any(p => p.OwnerId == 2);
                var player2NoShips = !Fleets.Any(f => f.OwnerId == 2);

                if (player1NoPlanets && player1NoShips)
                {
                    // player 2 has won
                    var winner = Players.Values.FirstOrDefault(p => p.Id == 2);
                    var loser = Players.Values.FirstOrDefault(p => p.Id == 1);
                    this.Status = $"{winner?.PlayerName} has defeated {loser?.PlayerName}!";
                    this.GameOver = true;
                }

                if (player2NoPlanet && player2NoShips)
                {
                    // player 1 has won
                    var winner = Players.Values.FirstOrDefault(p => p.Id == 1);
                    var loser = Players.Values.FirstOrDefault(p => p.Id == 2);
                    this.Status = $"{winner?.PlayerName} has defeated {loser?.PlayerName}!";
                    this.GameOver = true;
                }

                PlayerAScoreOverTime.Add(_getPlayerScore(1));
                PlayerBScoreOverTime.Add(_getPlayerScore(2));

                // Turn complete
                Turn++;
                endPlayerTurn = currentTime.AddMilliseconds(PLAYER_TURN_LENGTH);
                endServerTurn = endPlayerTurn.AddMilliseconds(SERVER_TURN_LENGTH);
                Processing = false;
                System.Diagnostics.Debug.WriteLine($"Game {Id} : Turn {Turn} : Next Turn Start {endServerTurn.Subtract(DateTime.UtcNow).TotalMilliseconds}ms");
            }

            if (Turn >= MAX_TURN)
            {
                var player1 = _getPlayerForId(1)?.PlayerName;
                var player2 = _getPlayerForId(2)?.PlayerName;

                var player1Score = _getPlayerScore(1);
                var player2Score = _getPlayerScore(2);

                if (player1Score == player2Score)
                {
                    this.Status = $"{player1} has tied {player2}!";
                }
                else if (player1Score > player2Score)
                {
                    this.Status = $"{player1} has defeated {player2}!";
                }
                else
                {
                    this.Status = $"{player2} has defeated {player1}!";
                }

                this.GameOver = true;
            }
        }

        public StatusResult GetStatus(StatusRequest request)
        {
            var status = new StatusResult()
            {
                Success = true,
                Status = this.Status,
                IsGameOver = this.GameOver,
                Waiting = this.Waiting,
                CurrentTurn = Turn,
                NextTurnStart = endServerTurn,
                EndOfCurrentTurn = endPlayerTurn,
                PlayerTurnLength = (int)PLAYER_TURN_LENGTH,
                ServerTurnLength = (int)SERVER_TURN_LENGTH,
                Planets = Planets.Select(p => Mapper.Map<Shared.Planet>(p)).ToList(),
                Fleets = Fleets.Select(f => Mapper.Map<Shared.Fleet>(f)).ToList(),
                PlayerA = 1,
                PlayerAScore = _getPlayerScore(1),
                PlayerAScoreOverTime = PlayerAScoreOverTime,
                PlayerB = 2,
                PlayerBScore = _getPlayerScore(2),
                PlayerBScoreOverTime = PlayerBScoreOverTime
            };
            return status;
        }
    }
}
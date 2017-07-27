using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using PlanetWars.Shared;

namespace CSharpAgent20
{
    public class AgentBase
    {
        private bool _isRunning = false;
        private readonly WebClient _client = null;
        private readonly string _endpoint;

        private List<MoveRequest> _pendingMoveRequests = new List<MoveRequest>();

        protected long TimeToNextTurn { get; set; }
        protected int CurrentTurn { get; set; }
        protected int GameId { get; set; }
        
        // string guid that acts as an authorization token, definitely not crypto secure
        public string AuthToken { get; set; }
        public string Name { get; set; }
        public int LastTurn { get; private set; }
        public int MyId { get; private set; }

        public AgentBase(string name, string endpoint, int gameId)
        {
            Name = name;
            _endpoint = endpoint;
            GameId = gameId;

            _client = new WebClient();
        }

        public void SendFleet(int sourcePlanetId, int destinationPlanetId, int numShips)
        {
            var moveRequest = new MoveRequest()
            {
                AuthToken = AuthToken,
                GameId = GameId,
                SourcePlanetId = sourcePlanetId,
                DestinationPlanetId = destinationPlanetId,
                NumberOfShips = numShips
            };
            _pendingMoveRequests.Add(moveRequest);
        }

        protected LogonResult Logon()
        {
            var request = new LogonRequest()
            {
                AgentName = Name,
                GameId = GameId
            };

            _client.Headers[HttpRequestHeader.ContentType] = "application/json";
            var htmlResult = _client.UploadString(string.Concat(_endpoint, "api/logon"), JsonConvert.SerializeObject(request));
            var result = JsonConvert.DeserializeObject<LogonResult>(htmlResult);

            if (!result.Success)
            {
                Console.WriteLine(string.Format("Error talking to server: {0}, {1}", result.Message, string.Join(",", result.Errors)));
                throw new Exception("Could not talk to sever");
            }

            AuthToken = result.AuthToken;
            GameId = result.GameId;
            MyId = result.Id;
            TimeToNextTurn = (long)result.GameStart.Subtract(DateTime.UtcNow).TotalMilliseconds;
            Console.WriteLine(string.Format("Logged in as {0}: Game Id {1} starts in {2}ms", Name, result.GameId, TimeToNextTurn));
            return result;
        }

        protected StatusResult UpdateGameState()
        {
            var request = new StatusRequest()
            {
                GameId = GameId
            };

            _client.Headers[HttpRequestHeader.ContentType] = "application/json";
            var htmlResult = _client.UploadString(string.Concat(_endpoint, "api/status"), JsonConvert.SerializeObject(request));
            var result = JsonConvert.DeserializeObject<StatusResult>(htmlResult);

            if (!result.Success)
            {
                Console.WriteLine(string.Format("Error talking to server: {0}, {1}", result.Message, string.Join(",", result.Errors)));
                throw new Exception("Could not talk to sever");
            }

            TimeToNextTurn = (long)result.NextTurnStart.Subtract(DateTime.UtcNow).TotalMilliseconds;
            CurrentTurn = result.CurrentTurn;
            Console.WriteLine(string.Format("Next turn in {0}ms", TimeToNextTurn));
            return result;
        }

        protected List<MoveResult> SendUpdate(List<MoveRequest> moveCommands)
        {
            _client.Headers[HttpRequestHeader.ContentType] = "application/json";
            var htmlResult = _client.UploadString(string.Concat(_endpoint, "api/move"), JsonConvert.SerializeObject(moveCommands));
            var results = JsonConvert.DeserializeObject<List<MoveResult>>(htmlResult);

            foreach (var result in results)
            {
                Console.WriteLine(result.Message);
            }
            return results;
        }

        public void Start()
        {
            Logon();
            if (!_isRunning)
            {
                _isRunning = true;
                while (_isRunning)
                {
                    if (TimeToNextTurn > 0)
                    {
                        Thread.Sleep((int)(TimeToNextTurn));
                    }

                    var gs = UpdateGameState();
                    if (gs.IsGameOver)
                    {
                        _isRunning = false;
                        Console.WriteLine("Game Over!");
                        Console.WriteLine(gs.Status);
                        _client.Dispose();
                        break;
                    }

                    Update(gs);
                    var ur = SendUpdate(this._pendingMoveRequests);
                    this._pendingMoveRequests.Clear();
                }
            }
        }

        public virtual void Update(StatusResult gs)
        {
            // override me
        }
    }
}

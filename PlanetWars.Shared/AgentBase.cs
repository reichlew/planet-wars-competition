using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PlanetWars.Shared
{
    public class AgentBase
    {
        private readonly HttpClient _client = null;

        private List<MoveRequest> _pendingMoveRequests = new List<MoveRequest>();

        protected long TimeToNextTurn { get; set; }
        protected int CurrentTurn { get; set; }
        protected int GameId { get; set; }
        protected MapGenerationOption MapGeneration { get; set; }

        // string guid that acts as an authorization token, definitely not crypto secure
        public string AuthToken { get; set; }

        public string Name { get; set; }
        public int LastTurn { get; private set; }
        public int MyId { get; private set; }

        public AgentBase(string name, int gameId, MapGenerationOption mapGeneration)
        {
            Name = name;
            GameId = gameId;
            MapGeneration = mapGeneration;

            _client = new HttpClient() { BaseAddress = new Uri("http://localhost/PlanetWars/") };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

        protected async Task<LogonResult> Logon()
        {
            var response = await _client.PostAsJsonAsync("api/logon", new LogonRequest()
            {
                AgentName = Name,
                GameId = GameId,
                MapGeneration = MapGeneration
            });

            var result = await response.Content.ReadAsAsync<LogonResult>();
            if (!result.Success)
            {
                Console.WriteLine($"Error talking to server: {result.Message}, {string.Join(",", result.Errors)}");
                throw new Exception("Could not talk to sever");
            }

            AuthToken = result.AuthToken;
            GameId = result.GameId;
            MyId = result.Id;
            TimeToNextTurn = (long)result.GameStart.Subtract(DateTime.UtcNow).TotalMilliseconds;
            Console.WriteLine($"Logged in as {Name}: Game Id {result.GameId} starts in {TimeToNextTurn}ms");
            return result;
        }

        protected async Task<StatusResult> UpdateGameState()
        {
            var response = await _client.PostAsJsonAsync("api/status", new StatusRequest()
            {
                GameId = GameId
            });

            var result = await response.Content.ReadAsAsync<StatusResult>();
            if (!result.Success)
            {
                Console.WriteLine($"Error talking to server: {result.Message}, {string.Join(",", result.Errors)}");
                throw new Exception("Could not talk to sever");
            }

            TimeToNextTurn = (long)result.NextTurnStart.Subtract(DateTime.UtcNow).TotalMilliseconds;
            CurrentTurn = result.CurrentTurn;
            Console.WriteLine($"Next turn in {TimeToNextTurn}ms");
            return result;
        }

        protected async Task<List<MoveResult>> SendUpdate(List<MoveRequest> moveCommands)
        {
            var response = await _client.PostAsJsonAsync("api/move", moveCommands);
            var results = await response.Content.ReadAsAsync<List<MoveResult>>();
            foreach (var result in results)
            {
                if (result.Message == "Failure")
                {
                    Console.WriteLine($"Error talking to server: {result.Message}, {string.Join(",", result.Errors)}");
                }
                else
                {
                    Console.WriteLine(result.Message);
                }
            }
            return results;
        }

        public async Task Start()
        {
            await Logon();

            while (true)
            {
                if (TimeToNextTurn > 0)
                {
                    await Task.Delay((int)(TimeToNextTurn));
                }

                var gs = await UpdateGameState();

                if (gs.IsGameOver)
                {
                    Console.WriteLine("Game Over!");
                    Console.WriteLine(gs.Status);
                    _client.Dispose();
                    break;
                }

                if (!gs.Waiting)
                {
                    Update(gs);
                    var ur = await SendUpdate(this._pendingMoveRequests);
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
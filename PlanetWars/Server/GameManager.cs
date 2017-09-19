﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanetWars.Shared;

namespace PlanetWars.Server
{
    public class GameManager
    {
        private static readonly GameManager _instance = new GameManager();
        public Dictionary<int, Game> Games { get; set; }

        public static GameManager Instance
        {
            get { return _instance; }
        }

        private GameManager()
        {
            if (Games == null)
            {
                Games = new Dictionary<int, Game>();
            }
        }

        public Game GetNewGame(LogonRequest request)
        {
            var game = new Game(request.MapGeneration);
            Games.Add(game.Id, game);
            return game;
        }

        public Game GetWaitingGameById(int gameId)
        {
            return Games.Values.Where(g => g.Id == gameId && g.Waiting).FirstOrDefault();
        }

        public StatusResult GetGameStatus(int gameId)
        {
            if (!Games.ContainsKey(gameId))
            {
                return null;
            }
            var game = Games[gameId];
            var result = game.GetStatus(null);
            return result;
        }

        public List<string> GetAllAuthTokens()
        {
            return Games.Values.SelectMany(g => g.AuthTokens.Keys).ToList();
        }

        public List<Game> GetAllActiveGames()
        {
            return Games.Values.Where(g => g.Running == true || g.GameOver == true).ToList();
        }

        public LogonResult Execute(LogonRequest request)
        {
            Game game;

            switch (request.GameId)
            {
                case -2:
                    game = GetNewGame(request);
                    game.Start();
                    game.StartDemoAgent("Advanced CPU", game.Id, true);
                    return game.LogonPlayer(request.AgentName);
                case -1:
                    game = GetNewGame(request);
                    game.Start();
                    game.StartDemoAgent("CPU", game.Id, false);
                    return game.LogonPlayer(request.AgentName);
                case 0:
                    game = GetNewGame(request);
                    game.Start();
                    return game.LogonPlayer(request.AgentName);
                default:
                    game = GetWaitingGameById(request.GameId);
                    if (game != null)
                    {
                        return game.LogonPlayer(request.AgentName);
                    }
                    else
                    {
                        return new LogonResult() { Id = request.GameId, Success = false, Message = "The game id specified is not available.", Errors = new string[] { } };
                    }
            }
        }

        public StatusResult Execute(StatusRequest request)
        {
            var game = Games[request.GameId];
            return game.GetStatus(request);
        }

        public MoveResult Execute(MoveRequest request)
        {
            var game = Games[request.GameId];
            return game.MoveFleet(request);
        }
    }
}
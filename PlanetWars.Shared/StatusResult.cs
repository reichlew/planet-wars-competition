﻿using System;
using System.Collections.Generic;

namespace PlanetWars.Shared
{
    public class StatusResult : BaseResult<StatusResult>
    {
        public bool Waiting { get; set; }
        public bool IsGameOver { get; set; }
        public string Status { get; set; }
        public int PlayerA { get; set; }
        public int PlayerB { get; set; }
        public int PlayerAScore { get; set; }
        public int PlayerBScore { get; set; }

        public int CurrentTurn { get; set; }
        public DateTime EndOfCurrentTurn { get; set; }
        public DateTime NextTurnStart { get; set; }

        public int PlayerTurnLength { get; set; }
        public int ServerTurnLength { get; set; }

        public List<Planet> Planets { get; set; }
        public List<Fleet> Fleets { get; set; }
        public List<int> PlayerAScoreOverTime { get; set; }
        public List<int> PlayerBScoreOverTime { get; set; }
    }
}
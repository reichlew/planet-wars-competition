using System;
using System.Linq;
using PlanetWars.Shared;

namespace CSharpAgent
{
    public class Agent : AgentBase
    {
        private const string TeamName = "YOUR_TEAM_NAME";

        public Agent(int gameId, MapGenerationOption mapGeneration) : base(TeamName, gameId, mapGeneration)
        {
        }

        public override void Update(StatusResult gs)
        {
            // do cool ai stuff
            Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]Current Turn: {gs.CurrentTurn}");
            Console.WriteLine($"Owned Planet Id's: {string.Join(", ", gs.Planets.Where(p => p.OwnerId == MyId).Select(p => p.Id))}");

            // find the first planet we don't own
            var targetPlanet = gs.Planets.FirstOrDefault(p => p.OwnerId != MyId);
            if (targetPlanet == null) return; // WE OWN IT ALLLLLLLLL

            Console.WriteLine(string.Format("Targeting planet {0} which has {1} ships", targetPlanet.Id, targetPlanet.NumberOfShips));

            // send half rounded down of our ships from each planet we do own
            foreach (var planet in gs.Planets.Where(p => p.OwnerId == MyId))
            {
                var ships = (int)Math.Floor(planet.NumberOfShips / 2.0);
                if (ships > 0)
                {
                    Console.WriteLine(string.Format("Sending {0} ships from {1}", ships, planet.Id));
                    SendFleet(planet.Id, targetPlanet.Id, ships);
                }
            }
        }
    }
}
using System;
using System.Linq;
using PlanetWars.Shared;

namespace CSharpAgent
{
    public class Agent : AgentBase
    {
        public Agent() : base("YOUR_TEAM_NAME", "http://localhost:52802", -1){}

        public override void Update(StatusResult gs)
        {
            // do cool ai stuff
            Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]Current Turn: {gs.CurrentTurn}");
            Console.WriteLine($"Owned Planet Id's: {string.Join(", ", gs.Planets.Where(p => p.OwnerId == MyId).Select(p =>  p.Id))}");

            // find the first planet we don't own
            var targetPlanet = gs.Planets.FirstOrDefault(p => p.OwnerId != MyId);
            if (targetPlanet == null) return; // WE OWN IT ALLLLLLLLL

            Console.WriteLine($"Targeting planet {targetPlanet.Id} with {targetPlanet.NumberOfShips} ships");                       

            // send half rounded down of our ships from each planet we do own
            foreach (var planet in gs.Planets.Where(p => p.OwnerId == MyId))
            {
                var ships = (int)Math.Floor(planet.NumberOfShips / 2.0);
                if (ships > 0)
                {
                    SendFleet(planet.Id, targetPlanet.Id, ships);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PlanetWars.Shared;

namespace PlanetWars.DemoAgent
{
    public class AdvancedAgent : AgentBase
    {
        public AdvancedAgent(string name, string endpoint, int gameId) : base(name, endpoint, gameId)
        {
        }

        public override void Update(StatusResult gs)
        {
            var enemyId = gs.PlayerA == MyId ? gs.PlayerB : gs.PlayerA;

            var myPlanets = gs.Planets.Where(x => x.OwnerId == MyId);
            var unowned = gs.Planets.Where(x => x.OwnerId == -1);
            var enemyPlanets = gs.Planets.Where(x => x.OwnerId == enemyId);

            var targetPlanet = unowned.Union(enemyPlanets).OrderBy(x => (double)x.NumberOfShips / (double)x.GrowthRate).FirstOrDefault();
            if (targetPlanet == null) return;

            foreach (var planet in myPlanets)
            {
                var incomingEnemy = gs.Fleets.Where(x => x.DestinationPlanetId == planet.Id);

                if (incomingEnemy.Any())
                {
                    var timeTillAttack = incomingEnemy.OrderBy(x => x.NumberOfTurnsToDestination).First().NumberOfTurnsToDestination;
                    var totalEnemy = incomingEnemy.Sum(x => x.NumberOfShips);

                    if (totalEnemy < planet.NumberOfShips + (timeTillAttack * planet.GrowthRate))
                    {
                        var ships = planet.NumberOfShips - totalEnemy;
                        if (ships > 0)
                        {
                            SendFleet(planet.Id, targetPlanet.Id, ships);
                        }
                    }
                }
                else
                {
                    var ships = (int)Math.Floor(planet.NumberOfShips / 2.0);
                    SendFleet(planet.Id, targetPlanet.Id, ships);
                }
            }
        }
    }
}
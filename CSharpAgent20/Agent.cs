using System;
using System.Collections;
using System.Collections.Generic;
using PlanetWars.Shared;

namespace CSharpAgent20
{
    public class Agent : AgentBase
    {
        public Agent() : base("YOUR_TEAM_NAME", "http://localhost/planetwwars/", -1) { }

        public override void Update(StatusResult gs)
        {
            // do cool ai stuff
            Console.WriteLine(string.Format("[{0}]Current Turn: {1}", DateTime.Now.ToShortTimeString(), gs.CurrentTurn));
            Console.WriteLine(string.Format("Owned Planet Id's: {0}", string.Join(", ", MyPlanetIds(gs.Planets, MyId).ToArray())));

            // find the first planet we don't own
            var targetPlanet = FirstUnowned(gs.Planets, MyId);
            if (targetPlanet == null) return; // WE OWN IT ALLLLLLLLL

            Console.WriteLine(string.Format("Targeting planet {0} which has {1} ships", targetPlanet.Id, targetPlanet.NumberOfShips));

            // send half rounded down of our ships from each planet we do own
            foreach (var planet in MyPlanets(gs.Planets, MyId))
            {
                var ships = (int)Math.Floor(planet.NumberOfShips / 2.0);
                if (ships > 0)
                {
                    Console.WriteLine(string.Format("Sending {0} ships from {1}", ships, planet.Id));
                    SendFleet(planet.Id, targetPlanet.Id, ships);
                }
            }
        }

        public List<Planet> MyPlanets(List<Planet> planets, int MyId)
        {
            var result = new List<Planet>();
            foreach (var planet in planets)
            {
                if (planet.OwnerId == MyId)
                {
                    result.Add(planet);
                }
            }

            return result;
        }

        public List<string> MyPlanetIds(List<Planet> planets, int MyId)
        {
            var myPlanets = MyPlanets(planets, MyId);
            var result = new List<string>();
            foreach (var planet in myPlanets)
            {
                result.Add(planet.Id.ToString());
            }

            return result;
        }

        public Planet FirstUnowned(List<Planet> planets, int MyId)
        {
            foreach (var planet in planets)
            {
                if (planet.OwnerId != MyId)
                {
                    return planet;
                }
            }

            return null;
        }
    }
}

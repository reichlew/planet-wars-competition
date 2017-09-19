using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PlanetWars.Shared;

namespace PlanetWars.Server
{
    public class MapGenerator
    {
        private readonly Random randomizer;
        private readonly Dictionary<MapGenerationOption, Func<List<Planet>>> generators;

        public MapGenerator()
        {
            randomizer = new Random();
            generators = new Dictionary<MapGenerationOption, Func<List<Planet>>>() {
                { MapGenerationOption.Basic, () => GenerateBasicMap() },
                { MapGenerationOption.Random, () => GenerateRandomMap() }
            };
        }

        public List<Planet> GenerateMap(MapGenerationOption mapGeneration)
        {
            var planets = generators[mapGeneration]();
            planets.First().OwnerId = 1;
            planets.Last().OwnerId = 2;

            return planets;
        }

        private List<Planet> GenerateBasicMap()
        {
            var planets = new List<Planet>();
            var planetId = 0;

            planets.Add(new Planet()
            {
                Id = planetId++,
                OwnerId = -1,
                Position = new Point(0, 0),
                NumberOfShips = 40,
                GrowthRate = 5
            });

            planets.Add(new Planet()
            {
                Id = planetId++,
                OwnerId = -1,
                Position = new Point(2, 2),
                NumberOfShips = 10,
                GrowthRate = 2
            });

            planets.Add(new Planet()
            {
                Id = planetId++,
                OwnerId = -1,
                Position = new Point(8, 0),
                NumberOfShips = 20,
                GrowthRate = 3
            });

            planets.Add(new Planet()
            {
                Id = planetId++,
                OwnerId = -1,
                Position = new Point(0, 8),
                NumberOfShips = 20,
                GrowthRate = 3
            });

            planets.Add(new Planet()
            {
                Id = planetId++,
                OwnerId = -1,
                Position = new Point(4, 4),
                NumberOfShips = 100,
                GrowthRate = 7
            });

            planets.Add(new Planet()
            {
                Id = planetId++,
                OwnerId = -1,
                Position = new Point(6, 6),
                NumberOfShips = 10,
                GrowthRate = 2
            });

            planets.Add(new Planet()
            {
                Id = planetId++,
                OwnerId = -1,
                Position = new Point(8, 8),
                NumberOfShips = 40,
                GrowthRate = 5
            });

            return planets;
        }

        private List<Planet> GenerateRandomMap()
        {
            var planets = new List<Planet>();
            var planetId = 0;

            var XAxis = Enumerable.Range(0, 9);
            var halfYAxis = Enumerable.Range(0, 5);

            // All of the points on the X axis, combined with half of the Y axis, minus the exact center as we mirror the the results
            var potentialPoints = XAxis.SelectMany(xa => halfYAxis, (x, y) => new Point(x, y)).Where(p => !(p.X == 4 && p.Y == 4)).ToList();

            var planetsPerSide = randomizer.Next(3, 8);

            for (int i = 0; i < planetsPerSide; i++)
            {
                var planetPoint = i == 0 ? 0 : randomizer.Next(0, potentialPoints.Count);

                planets.Add(new Planet()
                {
                    Id = planetId++,
                    OwnerId = -1,
                    Position = potentialPoints[planetPoint],
                    NumberOfShips = randomizer.Next(10, 51),
                    GrowthRate = randomizer.Next(2, 6)
                });

                potentialPoints.RemoveAt(planetPoint);
            }

            var planetIndex = planets.Count - 1;

            if (planets.Count % 2 != 0) // If we generated an odd number
            {
                planets.Last().Position = new Point(4, 4); // Move the last planet to the center
                planetIndex--; // and don't mirror it
            }

            for (int i = planetIndex; i >= 0; i--)
            {
                planets.Add(new Planet()
                {
                    Id = planetId++,
                    OwnerId = -1,
                    Position = new Point(8 - planets[i].Position.X, 8 - planets[i].Position.Y),
                    NumberOfShips = planets[i].NumberOfShips,
                    GrowthRate = planets[i].GrowthRate
                });
            }

            return planets;
        }
    }
}
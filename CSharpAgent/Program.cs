using System;
using System.Threading.Tasks;
using PlanetWars.Shared;

namespace CSharpAgent
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("New game = 0, New game against the CPU = -1, or join an existing game by entering that Id:");
            var gameId = ParseGameId(Console.ReadLine());
            var mapType = MapGenerationOption.Basic;

            if (gameId <= 0)
            {
                Console.WriteLine("Choose a map type 0 = Basic, 1 = Random:");
                mapType = ParseMapResponse(Console.ReadLine());
            }

            StartAgent(gameId, mapType).Wait();
            Console.ReadLine();
        }

        private static async Task StartAgent(int gameId, MapGenerationOption mapGeneration)
        {
            try
            {
                var agent = new Agent(gameId, mapGeneration);
                await agent.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured: {ex}");
            }
        }

        private static int ParseGameId(string value)
        {
            var parsed = 0;
            return int.TryParse(value, out parsed) ? parsed : -1;
        }

        private static MapGenerationOption ParseMapResponse(string value)
        {
            var parsed = 0;
            return int.TryParse(value, out parsed) ? (MapGenerationOption)parsed : MapGenerationOption.Basic;
        }
    }
}
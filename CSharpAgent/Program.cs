using System;
using System.Threading.Tasks;

namespace CSharpAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            StartAgent().Wait();
            Console.ReadLine();
        }

        static async Task StartAgent()
        {
            try
            {
                var agent = new Agent();
                await agent.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured: {ex}");
            }
        }
    }
}

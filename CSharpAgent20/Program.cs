using System;

namespace CSharpAgent20
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var agent = new Agent();
                agent.Start();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured: {ex}");
                Console.ReadLine();
            }
        }
    }
}

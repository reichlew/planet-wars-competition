namespace PlanetWars.Shared
{
    public class Planet
    {
        public int Id { get; set; }
        public int NumberOfShips { get; set; }
        public int GrowthRate { get; set; }
        public int Size { get; set; } = 50;
        public Point Position { get; set; }
        public int OwnerId { get; set; } = -1;
    }
}

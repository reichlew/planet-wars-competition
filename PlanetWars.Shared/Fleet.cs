namespace PlanetWars.Shared
{
    public class Fleet
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int NumberOfShips { get; set; }
        public int NumberOfTurnsToDestination { get; set;}
        public int SourcePlanetId { get; set; }
        public int DestinationPlanetId { get; set; }
    }
}

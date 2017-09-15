namespace PlanetWars.Shared
{
    public class MoveRequest
    {
        public MoveRequest() { }

        public string AuthToken { get; set; }
        public int DestinationPlanetId { get; set; }
        public int GameId { get; set; }
        public int NumberOfShips { get; set; }
        public int SourcePlanetId { get; set; }
    }
}

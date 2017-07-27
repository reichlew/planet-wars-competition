namespace PlanetWars.Shared
{
    public class Player
    {
        // this is the public ID not used for authentication
        public int Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
    }
}

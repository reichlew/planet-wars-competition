using System;

namespace PlanetWars.Shared
{
    public class LogonResult : BaseResult<LogonResult>
    {
        public string AuthToken { get; set; }
        public int Id { get; set; }
        public int GameId { get; set; }
        // Game Start Date Time in UTC
        public DateTime GameStart { get; set; }
    }
}

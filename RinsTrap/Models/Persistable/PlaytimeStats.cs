namespace RinsTrap.Models.Persistable
{
    public class PlaytimeStats
    {
        /// <summary>
        /// Seconds played, keyed by day ("yyyy-MM-dd")
        /// </summary>
        public Dictionary<string, long> SecondsByDay { get; set; } = new();

        /// <summary>
        /// Total amount of play sessions recorded
        /// </summary>
        public int SessionCount { get; set; } = 0;

        /// <summary>
        /// Seconds tracked for the currently running session
        /// </summary>
        public long SessionSeconds { get; set; } = 0;

        /// <summary>
        /// Playtime per game, keyed by universe id
        /// </summary>
        public Dictionary<long, GamePlaytimeEntry> Games { get; set; } = new();
    }

    public class GamePlaytimeEntry
    {
        public long UniverseId { get; set; } = 0;

        public long PlaceId { get; set; } = 0;

        public string Name { get; set; } = "";

        public long SecondsPlayed { get; set; } = 0;

        public int SessionCount { get; set; } = 0;

        public DateTime FirstPlayed { get; set; }

        public DateTime LastPlayed { get; set; }
    }
}
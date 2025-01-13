namespace KothBackend.Models
{
    public class ListPlayerProfile
    {
        public List<PlayerProfile> m_list { get; set; } = new();
    }

    public class ListPlayerStats
    {
        public List<PlayerStats> m_list { get; set; } = new();
    }

    public class ListPlayerBan
    {
        public List<string> m_list { get; set; } = new();
    }
}
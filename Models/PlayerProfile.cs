using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace KothBackend.Models
{
    public class PlayerProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string m_playerUID { get; set; }
        public string? m_playerName { get; set; }
        public string? m_profileName { get; set; }
        public string? m_platformName { get; set; }
        public string? m_machineName { get; set; }
        public string? m_adapterName { get; set; }
        public int m_money { get; set; } = 10000;
        public int m_level { get; set; } = 1;
        public int m_xp { get; set; } = 0;
        public int m_kills { get; set; } = 0;
        public int m_deaths { get; set; } = 0;
        public int m_friendlyKills { get; set; } = 0;
        public List<string> m_unlockedItems { get; set; } = new();
        public List<PlayerPreset> m_playerPresets { get; set; } = new();
    }
}
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace KothBackend.Models
{
    public class PlayerStats
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string m_playerUID { get; set; }
        public int m_bulletsShot { get; set; }
        public int m_grenadesThrown { get; set; }
        public int m_maxKillStreak { get; set; }
        public int m_maxKillDistance { get; set; }
        public int m_insertionBonus { get; set; }
        public int m_killStreakX3 { get; set; }
        public int m_killStreakX5 { get; set; }
        public int m_killStreakX10 { get; set; }
        public int m_killStreakX20 { get; set; }
        public int m_killStreakX30 { get; set; }
    }
}
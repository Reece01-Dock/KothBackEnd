using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KothBackend.Models
{
    public class BonusCode
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Code { get; set; }
        public string Name { get; set; }
        public double Multiplier { get; set; }
        public DateTime DateEnd { get; set; }
        public HashSet<string> UsedByPlayers { get; set; } = new HashSet<string>();
    }
}
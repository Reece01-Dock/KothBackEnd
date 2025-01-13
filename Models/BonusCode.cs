﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KothBackend.Models
{
    public class BonusCode
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? PlayerUID { get; set; }
        public string? Multiplier { get; set; }
        public string? DateEnd { get; set; }
        public bool IsUsed { get; set; }
        public HashSet<string> UsedByPlayers { get; set; } = new HashSet<string>();
    }
}
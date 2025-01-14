using System.Text.Json.Serialization;

namespace KothBackend.Models
{
    public class BonusCodeRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("playerUID")]
        public string PlayerUID { get; set; }
    }
}

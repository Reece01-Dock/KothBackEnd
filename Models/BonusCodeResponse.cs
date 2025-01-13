namespace KothBackend.Models
{
    public class BonusCodeResponse
    {
        public string? name { get; set; }
        public string? code { get; set; }
        public string? playerUID { get; set; }
        public string? multiplier { get; set; }
        public string? dateEnd { get; set; }
        public bool error { get; set; }
        public string? errorReason { get; set; }
    }
}
namespace KothBackend.Models
{
    public class BonusCodeCreateRequest
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Multiplier { get; set; }
        public int ValidDays { get; set; }
    }
}

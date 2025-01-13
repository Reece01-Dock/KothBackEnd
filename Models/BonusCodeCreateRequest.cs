namespace KothBackend.Models
{
    public class BonusCodeCreateRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public double Multiplier { get; set; }
        public int ValidDays { get; set; }
    }
}

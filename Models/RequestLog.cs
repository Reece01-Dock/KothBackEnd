namespace KothBackend.Models
{
    public class RequestLog
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string? Body { get; set; }
        public int ResponseStatusCode { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; }
        public TimeSpan Duration { get; set; }

        public RequestLog()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            Headers = new Dictionary<string, string>();
            ResponseHeaders = new Dictionary<string, string>();
        }
    }
}
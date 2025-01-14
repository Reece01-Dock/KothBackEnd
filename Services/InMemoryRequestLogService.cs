using KothBackend.Models;
using System.Collections.Concurrent;

namespace KothBackend.Services
{
    public class InMemoryRequestLogService : IRequestLogService
    {
        private readonly ConcurrentQueue<RequestLog> _logs;
        private readonly int _maxLogs;

        public InMemoryRequestLogService(int maxLogs = 1000)
        {
            _logs = new ConcurrentQueue<RequestLog>();
            _maxLogs = maxLogs;
        }

        public void AddLog(RequestLog log)
        {
            _logs.Enqueue(log);

            // Remove old logs if we exceed the maximum
            while (_logs.Count > _maxLogs)
            {
                _logs.TryDequeue(out _);
            }
        }

        public IEnumerable<RequestLog> GetLogs(int? limit = null)
        {
            var logs = _logs.Reverse();
            if (limit.HasValue)
            {
                logs = logs.Take(limit.Value);
            }
            return logs;
        }

        public void ClearLogs()
        {
            while (_logs.TryDequeue(out _)) { }
        }
    }
}

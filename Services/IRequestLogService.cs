using KothBackend.Models;

namespace KothBackend.Services
{
    public interface IRequestLogService
    {
        void AddLog(RequestLog log);
        IEnumerable<RequestLog> GetLogs(int? limit = null);
        void ClearLogs();
    }
}

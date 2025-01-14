using KothBackend.Models;
using KothBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KothBackend.Pages
{
    public class LogsModel : PageModel
    {
        private readonly IRequestLogService _logService;

        public IEnumerable<RequestLog> Logs { get; private set; } = new List<RequestLog>();

        public LogsModel(IRequestLogService logService)
        {
            _logService = logService;
        }

        public void OnGet()
        {
            Logs = _logService.GetLogs();
        }

        public IActionResult OnPostClear()
        {
            _logService.ClearLogs();
            return RedirectToPage();
        }
    }
}

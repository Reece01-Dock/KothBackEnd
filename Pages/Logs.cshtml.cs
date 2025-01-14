using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Antiforgery;
using KothBackend.Services;
using KothBackend.Models;

namespace KothBackend.Pages
{
    public class LogsModel : PageModel
    {
        private readonly IRequestLogService _logService;
        private readonly IAntiforgery _antiforgery;

        public IEnumerable<RequestLog> Logs { get; private set; } = new List<RequestLog>();

        public LogsModel(IRequestLogService logService, IAntiforgery antiforgery)
        {
            _logService = logService;
            _antiforgery = antiforgery;
        }

        public void OnGet()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            Response.Headers.Add("X-CSRF-TOKEN", tokens.RequestToken);
            Logs = _logService.GetLogs();
        }

        public IActionResult OnPostClear()
        {
            _logService.ClearLogs();
            return RedirectToPage();
        }
    }
}
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
        private readonly ILogger<LogsModel> _logger;

        public IEnumerable<RequestLog> Logs { get; private set; } = new List<RequestLog>();

        public LogsModel(IRequestLogService logService, IAntiforgery antiforgery, ILogger<LogsModel> logger)
        {
            _logService = logService;
            _antiforgery = antiforgery;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            try
            {
                _logger.LogInformation("Loading logs");
                Logs = _logService.GetLogs().Reverse();
                var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
                Response.Headers["X-CSRF-TOKEN"] = tokens.RequestToken!;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading logs");
                throw;
            }
        }

        public IActionResult OnPostClear()
        {
            try
            {
                _logger.LogInformation("Clearing logs");
                _logService.ClearLogs();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing logs");
                throw;
            }
        }
    }
}

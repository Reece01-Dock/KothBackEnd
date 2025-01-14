using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Antiforgery;
using KothBackend.Services;
using KothBackend.Models;
using System.Text.Json;

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

        public IActionResult OnGet()
        {
            Logs = _logService.GetLogs().Reverse(); // Get logs in reverse chronological order
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            Response.Headers["X-CSRF-TOKEN"] = tokens.RequestToken!;
            return Page();
        }

        public async Task OnGetStream()
        {
            var response = Response;
            response.ContentType = "text/event-stream";
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["Connection"] = "keep-alive";

            // Send initial logs
            var initialLogs = _logService.GetLogs().Reverse();
            foreach (var log in initialLogs)
            {
                var json = JsonSerializer.Serialize(log);
                await response.WriteAsync($"data: {json}\n\n");
                await response.Body.FlushAsync();
            }

            var lastLogCount = _logService.GetLogs().Count();

            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                var currentLogs = _logService.GetLogs();
                var currentCount = currentLogs.Count();

                if (currentCount > lastLogCount)
                {
                    var newLogs = currentLogs.Take(currentCount - lastLogCount);
                    foreach (var log in newLogs.Reverse())
                    {
                        var json = JsonSerializer.Serialize(log);
                        await response.WriteAsync($"data: {json}\n\n");
                        await response.Body.FlushAsync();
                    }
                    lastLogCount = currentCount;
                }

                await Task.Delay(1000);
            }
        }

        public IActionResult OnPostClear()
        {
            _logService.ClearLogs();
            return RedirectToPage();
        }
    }
}
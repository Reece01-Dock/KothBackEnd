using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Antiforgery;
using System.Text.Json;
using System.Collections.Concurrent;
using KothBackend.Models;
using KothBackend.Services;

namespace KothBackend.Pages
{
    public class LogsModel : PageModel
    {
        private readonly IRequestLogService _logService;
        private readonly IAntiforgery _antiforgery;
        private static readonly ConcurrentDictionary<string, StreamWriter> _clients = new();

        public IEnumerable<RequestLog> Logs { get; private set; } = new List<RequestLog>();

        public LogsModel(IRequestLogService logService, IAntiforgery antiforgery)
        {
            _logService = logService;
            _antiforgery = antiforgery;
        }

        public IActionResult OnGet()
        {
            // Initially get the logs for first page load
            Logs = _logService.GetLogs().Reverse();
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            Response.Headers["X-CSRF-TOKEN"] = tokens.RequestToken!;
            return Page();
        }

        public async Task OnGetStream()
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            try
            {
                // Send initial logs
                var initialLogs = _logService.GetLogs().Reverse();
                foreach (var log in initialLogs)
                {
                    var json = JsonSerializer.Serialize(log);
                    await Response.WriteAsync($"data: {json}\n\n");
                    await Response.Body.FlushAsync();
                }

                // Setup event handler for new logs
                _logService.OnLogAdded += async (log) =>
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(log);
                        await Response.WriteAsync($"data: {json}\n\n");
                        await Response.Body.FlushAsync();
                    }
                    catch
                    {
                        // Connection might be closed
                    }
                };

                // Keep connection alive
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
            catch
            {
                // Connection closed
            }
        }

        public IActionResult OnPostClear()
        {
            _logService.ClearLogs();
            return RedirectToPage();
        }
    }
}
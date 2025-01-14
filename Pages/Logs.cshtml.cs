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
            Logs = _logService.GetLogs();
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

            // Generate a unique client ID
            string clientId = Guid.NewGuid().ToString();

            try
            {
                using var writer = new StreamWriter(response.Body);
                _clients[clientId] = writer;

                // Send initial logs
                var initialLogs = _logService.GetLogs();
                foreach (var log in initialLogs)
                {
                    await SendLogToClient(writer, log);
                }

                // Subscribe to new logs
                _logService.OnLogAdded += async (log) =>
                {
                    if (_clients.TryGetValue(clientId, out var clientWriter))
                    {
                        await SendLogToClient(clientWriter, log);
                    }
                };

                // Keep the connection alive
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
            }
        }

        private async Task SendLogToClient(StreamWriter writer, RequestLog log)
        {
            try
            {
                var json = JsonSerializer.Serialize(log);
                await writer.WriteAsync($"data: {json}\n\n");
                await writer.FlushAsync();
            }
            catch
            {
                // Client might have disconnected
            }
        }

        public IActionResult OnPostClear()
        {
            _logService.ClearLogs();
            return RedirectToPage();
        }
    }
}
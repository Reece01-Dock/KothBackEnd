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
                _logger.LogInformation("Loading initial logs");
                Logs = _logService.GetLogs().Reverse();
                var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
                Response.Headers["X-CSRF-TOKEN"] = tokens.RequestToken!;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading initial logs");
                throw;
            }
        }

        public async Task OnGetStream()
        {
            try
            {
                // Set response headers before writing any data
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                Response.ContentType = "text/event-stream";

                // Keep track of connection status
                var isConnected = true;
                HttpContext.RequestAborted.Register(() => isConnected = false);

                // Send initial logs
                var initialLogs = _logService.GetLogs().Reverse();
                foreach (var log in initialLogs)
                {
                    if (!isConnected) break;

                    var json = JsonSerializer.Serialize(log);
                    await Response.WriteAsync($"data: {json}\n\n");
                    await Response.Body.FlushAsync();
                }

                // Handler for new logs
                void NewLogHandler(RequestLog log)
                {
                    if (!isConnected) return;

                    var json = JsonSerializer.Serialize(log);
                    Response.WriteAsync($"data: {json}\n\n").Wait();
                    Response.Body.FlushAsync().Wait();
                }

                try
                {
                    // Subscribe to new logs
                    _logService.OnLogAdded += NewLogHandler;

                    // Keep connection alive while client is connected
                    while (isConnected && !HttpContext.RequestAborted.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    _logService.OnLogAdded -= NewLogHandler;
                    _logger.LogInformation("SSE connection ended");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SSE stream");
            }
        }

        private async Task SendLogToClient(RequestLog log)
        {
            try
            {
                var json = JsonSerializer.Serialize(log);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending individual log");
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
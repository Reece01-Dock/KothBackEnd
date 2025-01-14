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
                var response = Response;
                response.Headers["Cache-Control"] = "no-cache";
                response.Headers["Connection"] = "keep-alive";
                response.ContentType = "text/event-stream";

                // Send initial logs
                _logger.LogInformation("Sending initial logs through SSE");
                var initialLogs = _logService.GetLogs().Reverse();
                foreach (var log in initialLogs)
                {
                    await SendLogToClient(log);
                }

                // Handler for new logs
                void NewLogHandler(RequestLog log)
                {
                    try
                    {
                        SendLogToClient(log).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending log through SSE");
                    }
                }

                // Subscribe to new logs
                _logService.OnLogAdded += NewLogHandler;

                try
                {
                    _logger.LogInformation("Starting SSE connection");
                    // Keep the connection alive
                    while (!HttpContext.RequestAborted.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    _logger.LogInformation("SSE connection ended");
                    _logService.OnLogAdded -= NewLogHandler;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SSE stream");
                throw;
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
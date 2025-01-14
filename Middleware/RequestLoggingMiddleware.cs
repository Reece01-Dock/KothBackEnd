using Microsoft.AspNetCore.Http;
using System.Text;
using System.Diagnostics;
using KothBackend.Models;
using KothBackend.Services;

namespace KothBackend.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly IRequestLogService _logService;

        // Paths to exclude from logging
        private readonly HashSet<string> _excludedPaths = new()
        {
            "/logs",              // Main logs page
            "/favicon.ico"        // Browser favicon requests
        };

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger,
            IRequestLogService logService)
        {
            _next = next;
            _logger = logger;
            _logService = logService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a path we should exclude
            bool shouldLog = !_excludedPaths.Any(path =>
                context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase));

            RequestLog? log = null;
            var sw = Stopwatch.StartNew();

            if (shouldLog)
            {
                log = new RequestLog();
                await CaptureRequest(context.Request, log);
            }

            try
            {
                await _next(context);
            }
            finally
            {
                if (shouldLog && log != null)
                {
                    sw.Stop();
                    log.Duration = sw.Elapsed;
                    await CaptureResponseBody(context.Response, log);
                    _logService.AddLog(log);
                }
            }
        }

        private async Task CaptureRequest(HttpRequest request, RequestLog log)
        {
            log.Method = request.Method;
            log.Path = request.Path;
            log.QueryString = request.QueryString.ToString();

            foreach (var (key, value) in request.Headers)
            {
                log.Headers[key] = string.Join(", ", value.Select(v => v ?? string.Empty));
            }

            if (request.ContentLength > 0 && IsTextBasedContentType(request.ContentType))
            {
                try
                {
                    request.EnableBuffering();
                    using var reader = new StreamReader(
                        request.Body,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        bufferSize: -1,
                        leaveOpen: true);

                    log.Body = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to read request body");
                    log.Body = "[Error reading body]";
                }
            }
        }

        private async Task CaptureResponseBody(HttpResponse response, RequestLog log)
        {
            try
            {
                // Create a custom response stream that captures the content
                var captureStream = new ResponseCaptureStream(response.Body);
                response.Body = captureStream;

                // Read the response body
                await response.CompleteAsync();
                log.ResponseBody = captureStream.GetCapturedContent();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to read response body");
                log.ResponseBody = "[Error reading body]";
            }
        }

        private void CaptureResponse(HttpResponse response, RequestLog log)
        {
            log.ResponseStatusCode = response.StatusCode;

            foreach (var (key, value) in response.Headers)
            {
                log.ResponseHeaders[key] = string.Join(", ", value.Select(v => v ?? string.Empty));
            }
        }

        private bool IsTextBasedContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return false;

            return contentType.Contains("json") ||
                   contentType.Contains("xml") ||
                   contentType.Contains("text") ||
                   contentType.Contains("form-data") ||
                   contentType.Contains("form-urlencoded");
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
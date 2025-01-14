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

        private readonly HashSet<string> _excludedPaths = new()
        {
            "/logs",
            "/favicon.ico"
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
            bool shouldLog = !_excludedPaths.Any(path =>
                context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase));

            if (!shouldLog)
            {
                await _next(context);
                return;
            }

            var log = new RequestLog();
            var sw = Stopwatch.StartNew();

            // Capture the original body stream
            var originalBody = context.Response.Body;

            try
            {
                // Create a new memory stream
                using (var memStream = new MemoryStream())
                {
                    // Set the response body to the memory stream
                    context.Response.Body = memStream;

                    // Capture request details
                    await CaptureRequest(context.Request, log);

                    // Call the next middleware
                    await _next(context);

                    // Capture response details
                    memStream.Position = 0;
                    using (var reader = new StreamReader(memStream))
                    {
                        log.ResponseBody = await reader.ReadToEndAsync();
                    }

                    // Copy the response to the original stream
                    memStream.Position = 0;
                    await memStream.CopyToAsync(originalBody);
                }
            }
            finally
            {
                // Always restore the original stream
                context.Response.Body = originalBody;

                // Complete the log
                sw.Stop();
                log.Duration = sw.Elapsed;
                CaptureResponseDetails(context.Response, log);
                _logService.AddLog(log);
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

        private void CaptureResponseDetails(HttpResponse response, RequestLog log)
        {
            log.ResponseStatusCode = response.StatusCode;

            foreach (var (key, value) in response.Headers)
            {
                if (!log.ResponseHeaders.ContainsKey(key))
                {
                    log.ResponseHeaders[key] = string.Join(", ", value.Select(v => v ?? string.Empty));
                }
            }

            // Don't try to read the response body here - it's already been captured
        }

        private bool IsTextBasedContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return false;

            return contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Contains("text", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Contains("form-data", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Contains("form-urlencoded", StringComparison.OrdinalIgnoreCase);
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
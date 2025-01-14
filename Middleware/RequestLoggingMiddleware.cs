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

            if (!shouldLog)
            {
                await _next(context);
                return;
            }

            var log = new RequestLog();
            var sw = Stopwatch.StartNew();

            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                // Capture request
                await CaptureRequest(context.Request, log);

                // Process request
                await _next(context);

                // Capture response
                await CaptureResponse(context.Response, responseBodyStream, log);

                // Copy the response to the original stream
                responseBodyStream.Position = 0;
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            finally
            {
                sw.Stop();
                log.Duration = sw.Elapsed;
                context.Response.Body = originalBodyStream;

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

        private async Task CaptureResponse(HttpResponse response, MemoryStream responseBodyStream, RequestLog log)
        {
            log.ResponseStatusCode = response.StatusCode;

            foreach (var (key, value) in response.Headers)
            {
                log.ResponseHeaders[key] = string.Join(", ", value.Select(v => v ?? string.Empty));
            }

            if (IsTextBasedContentType(response.ContentType))
            {
                try
                {
                    responseBodyStream.Position = 0;
                    using var reader = new StreamReader(responseBodyStream, Encoding.UTF8);
                    log.ResponseBody = await reader.ReadToEndAsync();
                    responseBodyStream.Position = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to read response body");
                    log.ResponseBody = "[Error reading response body]";
                }
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
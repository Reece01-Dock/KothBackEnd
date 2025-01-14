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
            "/favicon.ico",       // Browser favicon requests
            "/css",              // Static CSS files
            "/js"                // Static JS files
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

            var log = shouldLog ? new RequestLog() : null;
            var sw = shouldLog ? Stopwatch.StartNew() : null;

            try
            {
                if (shouldLog)
                {
                    await CaptureRequest(context.Request, log!);
                }

                // Create response capture stream if needed
                Stream originalBody = context.Response.Body;
                ResponseCaptureStream? responseStream = null;

                if (shouldLog)
                {
                    responseStream = new ResponseCaptureStream(originalBody);
                    context.Response.Body = responseStream;
                }

                // Call the next middleware in the pipeline
                await _next(context);

                // Capture response if needed
                if (shouldLog && responseStream != null)
                {
                    CaptureResponse(context.Response, responseStream, log!);
                    context.Response.Body = originalBody;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request");
                throw;
            }
            finally
            {
                if (shouldLog && sw != null && log != null)
                {
                    sw.Stop();
                    log.Duration = sw.Elapsed;
                    _logService.AddLog(log);
                }
            }
        }

        private async Task CaptureRequest(HttpRequest request, RequestLog log)
        {
            log.Method = request.Method;
            log.Path = request.Path;
            log.QueryString = request.QueryString.ToString();

            // Capture headers
            foreach (var (key, value) in request.Headers)
            {
                log.Headers[key] = string.Join(", ", value);
            }

            // Only try to read the body for specific content types and if it has content
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

        private void CaptureResponse(HttpResponse response, ResponseCaptureStream responseStream, RequestLog log)
        {
            log.ResponseStatusCode = response.StatusCode;

            foreach (var (key, value) in response.Headers)
            {
                log.ResponseHeaders[key] = string.Join(", ", value);
            }

            // Capture response body if it's a text-based content type
            if (IsTextBasedContentType(response.ContentType))
            {
                try
                {
                    log.ResponseBody = responseStream.GetCapturedContent();
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
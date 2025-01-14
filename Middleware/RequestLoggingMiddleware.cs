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
            var log = new RequestLog();
            var sw = Stopwatch.StartNew();

            try
            {
                // Capture request details
                await CaptureRequest(context.Request, log);

                // Capture the response body
                var originalBodyStream = context.Response.Body;
                using var responseBodyStream = new ResponseCaptureStream(originalBodyStream);
                context.Response.Body = responseBodyStream;

                // Call the next middleware in the pipeline
                await _next(context);

                // Capture response details including body
                CaptureResponse(context.Response, responseBodyStream, log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request");
                throw;
            }
            finally
            {
                sw.Stop();
                log.Duration = sw.Elapsed;
                _logService.AddLog(log);
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
                    // Enable buffering for the request body
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
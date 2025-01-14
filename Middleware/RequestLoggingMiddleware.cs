using Microsoft.AspNetCore.Http;
using System.Text;

namespace KothBackend.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Log the request
                await LogRequest(context.Request);

                // Enable buffering for the request body so it can be read multiple times
                context.Request.EnableBuffering();

                // Call the next middleware in the pipeline
                await _next(context);

                // Log the response
                await LogResponse(context.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request");
                throw;
            }
        }

        private async Task LogRequest(HttpRequest request)
        {
            var message = new StringBuilder();
            message.AppendLine("HTTP Request Information:");
            message.AppendLine($"Schema:{request.Scheme}");
            message.AppendLine($"Host: {request.Host}");
            message.AppendLine($"Path: {request.Path}");
            message.AppendLine($"QueryString: {request.QueryString}");
            message.AppendLine($"Method: {request.Method}");

            // Log headers
            message.AppendLine("Headers:");
            foreach (var (headerKey, headerValue) in request.Headers)
            {
                message.AppendLine($"    {headerKey}: {headerValue}");
            }

            // Log request body for specific content types
            if (request.ContentLength > 0 && IsTextBasedContentType(request.ContentType))
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                message.AppendLine($"Body: {body}");
                request.Body.Position = 0;
            }

            _logger.LogInformation(message.ToString());
        }

        private async Task LogResponse(HttpResponse response)
        {
            var message = new StringBuilder();
            message.AppendLine("HTTP Response Information:");
            message.AppendLine($"StatusCode: {response.StatusCode}");

            // Log response headers
            message.AppendLine("Headers:");
            foreach (var (headerKey, headerValue) in response.Headers)
            {
                message.AppendLine($"    {headerKey}: {headerValue}");
            }

            _logger.LogInformation(message.ToString());
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

    // Extension method to make it easier to add the middleware
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
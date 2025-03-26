using Microsoft.AspNetCore.Http.Extensions;

namespace GatewayService.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate requestDelegate, ILogger<RequestLoggingMiddleware> logger)
        {
            _requestDelegate = requestDelegate;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("Logfile запроса:");
            _logger.LogInformation($"Метод - {context.Request.Method}");
            _logger.LogInformation($"URL - {context.Request.GetDisplayUrl}");

            await _requestDelegate(context);

            _logger.LogInformation($"Статус ответа - {context.Response.StatusCode}");
            _logger.LogInformation($"Время ответа - {DateTime.Now}");
        }
    }
}

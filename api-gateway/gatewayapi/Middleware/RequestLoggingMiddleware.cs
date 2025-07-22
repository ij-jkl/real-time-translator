using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Model;
using System.Text;
using System.Linq;

namespace gatewayapi.Middleware
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
            var method = context.Request.Method;
            var path = context.Request.Path;
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
            var headers = string.Join("; ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));

            // Try to read the body if it's a small request and can be read (not for WebSocket/streaming)
            string body = "";
            if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
                using (var reader = new System.IO.StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    body = await reader.ReadToEndAsync();
                }
                context.Request.Body.Position = 0;
            }

            _logger.LogInformation("Incoming request: {method} {path}{query} from {ip}\nHeaders: {headers}\nBody: {body}", method, path, queryString, ip, headers, body);

            await _next(context);

            // Log YARP routing details
            if (context.Items.TryGetValue("ReverseProxy.Route", out var routeObj) && routeObj is RouteModel route)
            {
                _logger.LogInformation("Matched YARP route: {routeId}", route.Config.RouteId);
            }
            if (context.Items.TryGetValue("ReverseProxy.Cluster", out var clusterObj) && clusterObj is ClusterModel cluster)
            {
                _logger.LogInformation("Matched YARP cluster: {clusterId}", cluster.Config.ClusterId);
            }
            if (context.Items.TryGetValue("ReverseProxy.Destination", out var destObj) && destObj is DestinationModel dest)
            {
                _logger.LogInformation("Proxied to destination address: {address}", dest.Config.Address);
            }

            // Log response status code
            _logger.LogInformation("Response status code: {statusCode}", context.Response.StatusCode);
        }
    }
}

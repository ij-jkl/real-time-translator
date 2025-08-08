using gatewayapi.Services;
using System.Text;
using System.Text.Json;

namespace gatewayapi.Middleware
{
    public class TranscriptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TranscriptionLoggingMiddleware> _logger;

        public TranscriptionLoggingMiddleware(RequestDelegate next, ILogger<TranscriptionLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITranscriptionLogService transcriptionLogService)
        {
            // Skip WebSocket requests and non-transcription endpoints
            if (context.WebSockets.IsWebSocketRequest || !IsTranscriptionRequest(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var originalResponseBody = context.Response.Body;
            
            using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            try
            {
                await _next(context);

                responseBuffer.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBuffer).ReadToEndAsync();

                // Non-blocking logging
                _ = TryLogTranscriptionAsync(context, responseBody, transcriptionLogService);

                responseBuffer.Seek(0, SeekOrigin.Begin);
                await responseBuffer.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transcription logging middleware");
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }

        private static bool IsTranscriptionRequest(PathString path)
        {
            return path.Value?.Contains("/transcribe", StringComparison.OrdinalIgnoreCase) == true;
        }

        private async Task TryLogTranscriptionAsync(HttpContext context, string responseBody, ITranscriptionLogService transcriptionLogService)
        {
            try
            {
                if (string.IsNullOrEmpty(responseBody) || context.Response.StatusCode != 200)
                    return;

                var transcriptionResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                
                if (!transcriptionResponse.TryGetProperty("Transcription", out var transcriptionElement))
                    return;

                var transcriptionText = transcriptionElement.GetString();
                if (string.IsNullOrEmpty(transcriptionText))
                    return;

                string? language = null;
                double? transcriptionTime = null;

                if (transcriptionResponse.TryGetProperty("Language", out var langElement))
                    language = langElement.GetString();

                if (transcriptionResponse.TryGetProperty("TranscriptionTimeSeconds", out var timeElement))
                    transcriptionTime = timeElement.GetDouble();

                var sessionId = ExtractSessionId(context);

                await transcriptionLogService.LogTranscriptionAsync(
                    transcriptionText: transcriptionText,
                    sessionId: sessionId,
                    language: language,
                    transcriptionTimeSeconds: transcriptionTime,
                    clientIp: context.Connection.RemoteIpAddress?.ToString(),
                    responseStatusCode: context.Response.StatusCode
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log transcription response");
            }
        }

        private static string ExtractSessionId(HttpContext context)
        {
            // Try query parameters first
            if (context.Request.Query.TryGetValue("sessionId", out var sessionIdQuery))
                return sessionIdQuery.FirstOrDefault() ?? GenerateSessionId(context);

            // Try headers
            if (context.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdHeader))
                return sessionIdHeader.FirstOrDefault() ?? GenerateSessionId(context);

            return GenerateSessionId(context);
        }

        private static string GenerateSessionId(HttpContext context)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = Random.Shared.Next(1000, 9999);
            return $"session_{timestamp}_{random}";
        }
    }
}

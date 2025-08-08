using gatewayapi.Data;
using gatewayapi.Models;
using Microsoft.EntityFrameworkCore;

namespace gatewayapi.Services
{
    public interface ITranscriptionLogService
    {
        Task LogTranscriptionAsync(
            string transcriptionText,
            string? sessionId = null,
            string? language = null,
            double? transcriptionTimeSeconds = null,
            string? clientIp = null,
            int? responseStatusCode = null);
            
        Task<IEnumerable<TranscriptionLog>> GetTranscriptionsAsync(
            string? sessionId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int skip = 0,
            int take = 100);
            
        Task<TranscriptionLog?> GetTranscriptionByIdAsync(int id);
    }

    public class TranscriptionLogService : ITranscriptionLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TranscriptionLogService> _logger;

        public TranscriptionLogService(IServiceScopeFactory scopeFactory, ILogger<TranscriptionLogService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task LogTranscriptionAsync(
            string transcriptionText,
            string? sessionId = null,
            string? language = null,
            double? transcriptionTimeSeconds = null,
            string? clientIp = null,
            int? responseStatusCode = null)
        {
            
            // Runiing task async
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<TranscriptionDbContext>();
                    
                    var log = new TranscriptionLog
                    {
                        TranscriptionText = transcriptionText,
                        Timestamp = DateTime.UtcNow,
                        SessionId = sessionId,
                        Language = language,
                        TranscriptionTimeSeconds = transcriptionTimeSeconds,
                        ClientIpHash = TranscriptionLog.HashIpAddress(clientIp),
                        ResponseStatusCode = responseStatusCode
                    };

                    context.TranscriptionLogs.Add(log);
                    await context.SaveChangesAsync();
                    
                    _logger.LogDebug("Transcription logged for session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log transcription for session {SessionId}", sessionId);
                }
            });
        }

        public async Task<IEnumerable<TranscriptionLog>> GetTranscriptionsAsync(
            string? sessionId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int skip = 0,
            int take = 100)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TranscriptionDbContext>();
            
            var query = context.TranscriptionLogs.AsQueryable();

            if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(t => t.SessionId == sessionId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Timestamp <= toDate.Value);

            return await query
                .OrderByDescending(t => t.Timestamp)
                .Skip(skip)
                .Take(Math.Min(take, 500))
                .ToListAsync();
        }

        public async Task<TranscriptionLog?> GetTranscriptionByIdAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TranscriptionDbContext>();
            
            return await context.TranscriptionLogs.FindAsync(id);
        }
    }
}

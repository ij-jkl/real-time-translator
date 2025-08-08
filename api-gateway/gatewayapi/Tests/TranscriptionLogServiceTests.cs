using gatewayapi.Data;
using gatewayapi.Models;
using gatewayapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace gatewayapi.Tests
{
    public class TranscriptionLogServiceTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly TranscriptionDbContext _context;
        private readonly ITranscriptionLogService _service;

        public TranscriptionLogServiceTests()
        {
            var services = new ServiceCollection();
            
            services.AddDbContext<TranscriptionDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            
            services.AddLogging();
            services.AddScoped<ITranscriptionLogService, TranscriptionLogService>();
            
            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<TranscriptionDbContext>();
            _service = _serviceProvider.GetRequiredService<ITranscriptionLogService>();
        }

        [Fact]
        public async Task LogTranscriptionAsync_ShouldSaveToDatabase()
        {
            // Arrange
            var transcriptionText = "Hello world test";
            var sessionId = "test-session-123";
            var language = "en";

            // Act
            await _service.LogTranscriptionAsync(
                transcriptionText: transcriptionText,
                sessionId: sessionId,
                language: language,
                transcriptionTimeSeconds: 2.5,
                clientIp: "192.168.1.100",
                responseStatusCode: 200);

            // Allow background task to complete
            await Task.Delay(100);

            // Assert
            var logs = await _context.TranscriptionLogs.ToListAsync();
            Assert.Single(logs);
            
            var log = logs.First();
            Assert.Equal(transcriptionText, log.TranscriptionText);
            Assert.Equal(sessionId, log.SessionId);
            Assert.Equal(language, log.Language);
            Assert.Equal(2.5, log.TranscriptionTimeSeconds);
            Assert.NotNull(log.ClientIpHash);
            Assert.Equal(200, log.ResponseStatusCode);
        }

        [Fact]
        public async Task GetTranscriptionsAsync_ShouldReturnFilteredResults()
        {
            // Arrange
            await SeedTestData();

            // Act
            var results = await _service.GetTranscriptionsAsync(
                sessionId: "session-1",
                fromDate: DateTime.UtcNow.AddMinutes(-10),
                toDate: DateTime.UtcNow.AddMinutes(10));

            // Assert
            Assert.Equal(2, results.Count());
            Assert.All(results, r => Assert.Equal("session-1", r.SessionId));
        }

        [Fact]
        public async Task GetTranscriptionByIdAsync_ShouldReturnCorrectRecord()
        {
            // Arrange
            await SeedTestData();
            var existingLog = await _context.TranscriptionLogs.FirstAsync();

            // Act
            var result = await _service.GetTranscriptionByIdAsync(existingLog.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingLog.TranscriptionText, result.TranscriptionText);
        }

        private async Task SeedTestData()
        {
            var logs = new[]
            {
                new TranscriptionLog 
                { 
                    TranscriptionText = "Test 1", 
                    SessionId = "session-1", 
                    Timestamp = DateTime.UtcNow.AddMinutes(-5) 
                },
                new TranscriptionLog 
                { 
                    TranscriptionText = "Test 2", 
                    SessionId = "session-1", 
                    Timestamp = DateTime.UtcNow.AddMinutes(-3) 
                },
                new TranscriptionLog 
                { 
                    TranscriptionText = "Test 3", 
                    SessionId = "session-2", 
                    Timestamp = DateTime.UtcNow.AddMinutes(-1) 
                }
            };

            _context.TranscriptionLogs.AddRange(logs);
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}

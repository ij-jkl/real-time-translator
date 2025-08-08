using gatewayapi.Data;

namespace gatewayapi.Services
{
    public class DataRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataRetentionService> _logger;
        private readonly IConfiguration _configuration;

        public DataRetentionService(
            IServiceScopeFactory scopeFactory, 
            ILogger<DataRetentionService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var retentionDays = _configuration.GetValue<int>("Database:RetentionDays", 90);
            var intervalHours = _configuration.GetValue<int>("Database:CleanupIntervalHours", 24);
            
            _logger.LogInformation("Data retention service started: {RetentionDays} days retention, {IntervalHours}h interval", 
                retentionDays, intervalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
                    
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<TranscriptionDbContext>();
                    
                    await context.CleanupOldRecordsAsync(retentionDays);
                    _logger.LogInformation("Data retention cleanup completed");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during data retention cleanup");
                }
            }
        }
    }
}

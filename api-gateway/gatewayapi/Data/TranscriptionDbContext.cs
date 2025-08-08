using Microsoft.EntityFrameworkCore;
using gatewayapi.Models;

namespace gatewayapi.Data
{
    public class TranscriptionDbContext : DbContext
    {
        public TranscriptionDbContext(DbContextOptions<TranscriptionDbContext> options) : base(options)
        {
        }

        public DbSet<TranscriptionLog> TranscriptionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TranscriptionLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
        
        public async Task CleanupOldRecordsAsync(int retentionDays = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var oldRecords = TranscriptionLogs.Where(t => t.CreatedAt < cutoffDate);
            
            TranscriptionLogs.RemoveRange(oldRecords);
            await SaveChangesAsync();
        }
    }
}

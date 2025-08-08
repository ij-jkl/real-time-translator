using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace gatewayapi.Models
{
    public class TranscriptionLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(10000)]
        public string TranscriptionText { get; set; } = string.Empty;
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [MaxLength(100)]
        public string? SessionId { get; set; }
        
        [MaxLength(10)]
        public string? Language { get; set; }
        
        public double? TranscriptionTimeSeconds { get; set; }
        
        [MaxLength(64)] // Hashed ip fro the client
        public string? ClientIpHash { get; set; }
        
        public int? ResponseStatusCode { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public static string HashIpAddress(string? ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress)) return null;
            
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(ipAddress));
            return Convert.ToBase64String(hash);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models.Configuration
{
    public class ApiCredentials
    {
        public int Id { get; set; }
        
        [Required]
        public string Service { get; set; } = string.Empty;
        
        [Required]
        public string EncryptedClientId { get; set; } = string.Empty;
        
        [Required]        public string EncryptedClientSecret { get; set; } = string.Empty;
        
        public string Environment { get; set; } = "production";
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
    
    public class ApiCredentialsRequest
    {
        [Required]
        public string Service { get; set; } = string.Empty;
        
        [Required]
        public string ClientId { get; set; } = string.Empty;
        
        [Required]        public string ClientSecret { get; set; } = string.Empty;
        
        public string Environment { get; set; } = "production";
    }
    
    public class ApiCredentialsResponse
    {
        public string Service { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public string Environment { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
    }
}

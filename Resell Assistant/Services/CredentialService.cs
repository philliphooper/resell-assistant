using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models.Configuration;

namespace Resell_Assistant.Services
{    public interface ICredentialService
    {
        Task<bool> SaveCredentialsAsync(string service, string clientId, string clientSecret, string environment = "production");
        Task<(string clientId, string clientSecret)> GetCredentialsAsync(string service);
        Task<bool> HasCredentialsAsync(string service);
        Task<ApiCredentialsResponse> GetCredentialStatusAsync(string service);
        Task<bool> DeleteCredentialsAsync(string service);
        Task<(string encryptedValue, string decryptedValue)> TestEncryptionAsync(string testValue);
    }
      public class CredentialService : ICredentialService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<CredentialService> _logger;
        
        public CredentialService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IEncryptionService encryptionService,
            ILogger<CredentialService> logger)
        {
            _contextFactory = contextFactory;
            _encryptionService = encryptionService;
            _logger = logger;
        }
          public async Task<bool> SaveCredentialsAsync(string service, string clientId, string clientSecret, string environment = "production")
        {
            try
            {
                _logger.LogInformation("Saving credentials for service: {Service}", service);
                
                // Encrypt the credentials
                var encryptedClientId = _encryptionService.Encrypt(clientId);
                var encryptedClientSecret = _encryptionService.Encrypt(clientSecret);
                
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Find existing credentials or create new
                var existingCredentials = await context.ApiCredentials
                    .FirstOrDefaultAsync(c => c.Service == service);
                
                if (existingCredentials != null)
                {
                    existingCredentials.EncryptedClientId = encryptedClientId;
                    existingCredentials.EncryptedClientSecret = encryptedClientSecret;
                    existingCredentials.Environment = environment;
                    existingCredentials.UpdatedAt = DateTime.UtcNow;
                    existingCredentials.IsActive = true;
                }
                else
                {
                    var credentials = new ApiCredentials
                    {
                        Service = service,
                        EncryptedClientId = encryptedClientId,
                        EncryptedClientSecret = encryptedClientSecret,
                        Environment = environment,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    
                    context.ApiCredentials.Add(credentials);
                }
                
                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved credentials for service: {Service}", service);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save credentials for service: {Service}", service);
                return false;
            }
        }        public async Task<(string clientId, string clientSecret)> GetCredentialsAsync(string service)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                var credentials = await context.ApiCredentials
                    .FirstOrDefaultAsync(c => c.Service == service && c.IsActive);
                
                if (credentials == null)
                {
                    _logger.LogWarning("No credentials found for service: {Service}", service);
                    return (string.Empty, string.Empty);
                }
                
                _logger.LogDebug("Found credentials for service: {Service}, attempting decryption", service);
                
                var clientId = _encryptionService.Decrypt(credentials.EncryptedClientId) ?? string.Empty;
                var clientSecret = _encryptionService.Decrypt(credentials.EncryptedClientSecret) ?? string.Empty;
                
                _logger.LogDebug("Decrypted credentials for service: {Service} - ClientId length: {ClientIdLength}, ClientSecret length: {ClientSecretLength}", 
                    service, clientId.Length, clientSecret.Length);
                
                return (clientId, clientSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve credentials for service: {Service}", service);
                return (string.Empty, string.Empty);
            }
        }
          public async Task<bool> HasCredentialsAsync(string service)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                return await context.ApiCredentials
                    .AnyAsync(c => c.Service == service && c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check credentials for service: {Service}", service);
                return false;
            }
        }
          public async Task<ApiCredentialsResponse> GetCredentialStatusAsync(string service)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                var credentials = await context.ApiCredentials
                    .FirstOrDefaultAsync(c => c.Service == service && c.IsActive);
                
                return new ApiCredentialsResponse
                {
                    Service = service,
                    IsConfigured = credentials != null,
                    Environment = credentials?.Environment ?? "production",
                    LastUpdated = credentials?.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credential status for service: {Service}", service);
                return new ApiCredentialsResponse
                {
                    Service = service,
                    IsConfigured = false,
                    Environment = "production"
                };
            }
        }
        
        public async Task<bool> DeleteCredentialsAsync(string service)
        {            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                var credentials = await context.ApiCredentials
                    .FirstOrDefaultAsync(c => c.Service == service);
                
                if (credentials != null)
                {
                    credentials.IsActive = false;
                    credentials.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
                
                _logger.LogInformation("Successfully deleted credentials for service: {Service}", service);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete credentials for service: {Service}", service);
                return false;
            }        }        /// <summary>
        /// Test encryption and decryption with a provided value
        /// </summary>
        public Task<(string encryptedValue, string decryptedValue)> TestEncryptionAsync(string testValue)
        {
            try
            {
                var encrypted = _encryptionService.Encrypt(testValue);
                var decrypted = _encryptionService.Decrypt(encrypted);
                
                _logger.LogInformation("Encryption test - Original: {OriginalLength} chars, Encrypted: {EncryptedLength} chars, Decrypted: {DecryptedLength} chars", 
                    testValue.Length, encrypted.Length, decrypted.Length);
                
                return Task.FromResult((encrypted, decrypted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption test failed");
                throw;
            }
        }
    }
}

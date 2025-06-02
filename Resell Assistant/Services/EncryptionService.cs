using System.Security.Cryptography;
using System.Text;

namespace Resell_Assistant.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
    
    public class EncryptionService : IEncryptionService
    {
        private readonly string _key;
        
        public EncryptionService(IConfiguration configuration)
        {
            // Use a machine-specific key based on the machine name + a secret
            _key = GenerateKey(Environment.MachineName + "ResellAssistant2024");
        }        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
                
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key.Substring(0, 32)); // AES-256 requires 32 bytes
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Store the IV first
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            
            // Then encrypt the data
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            } // This will properly flush and finalize the encryption
            
            var result = msEncrypt.ToArray();
            return Convert.ToBase64String(result);
        }        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;
                
            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);
                
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_key.Substring(0, 32));
                
                var iv = new byte[aes.BlockSize / 8]; // Should be 16 bytes for AES
                var encrypted = new byte[fullCipher.Length - iv.Length];
                
                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, encrypted, 0, encrypted.Length);
                
                aes.IV = iv;
                
                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(encrypted);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                
                return srDecrypt.ReadToEnd();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        
        private static string GenerateKey(string input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }
    }
}

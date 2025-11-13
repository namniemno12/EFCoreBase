using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MyProject.Helper.Utils
{
    public class CryptoHelperUtil
    {
        private readonly string _secretKey;

        public CryptoHelperUtil(IConfiguration configuration)
        {
        // ✅ Try multiple config paths for backward compatibility
            _secretKey = configuration["Crypto:SecretKey"] 
 ?? configuration["EncryptionSettings:SecretKey"]
               ?? throw new InvalidOperationException("Encryption secret key not found in configuration. Please add 'Crypto:SecretKey' or 'EncryptionSettings:SecretKey' to appsettings.json");
        }

        public string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(_secretKey));
                byte[] iv = new byte[16];
                RandomNumberGenerator.Fill(iv);
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }
                    return Base64UrlEncode(ms.ToArray());
                }
            }
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
            }

            byte[] encryptedBytes = Base64UrlDecode(encryptedText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(_secretKey));

                byte[] iv = new byte[16];
                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        private string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private byte[] Base64UrlDecode(string input)
        {
            string base64 = input.Replace("-", "+").Replace("_", "/");
            while (base64.Length % 4 != 0)
            {
                base64 += "=";
            }
            return Convert.FromBase64String(base64);
        }





    }
}

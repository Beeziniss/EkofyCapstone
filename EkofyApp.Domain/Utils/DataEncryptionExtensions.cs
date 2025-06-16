using System.Security.Cryptography;
using System.Text;

namespace EkofyApp.Domain.Utils
{
    public class DataEncryptionExtensions
    {
        public static string Encrypt(string plainText)
        {
            string key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
            string iv = Environment.GetEnvironmentVariable("ENCRYPTION_IV");

            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);

            using MemoryStream ms = new();
            using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                using StreamWriter sw = new(cs);
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}

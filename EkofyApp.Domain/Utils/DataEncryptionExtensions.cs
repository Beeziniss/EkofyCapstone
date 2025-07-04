using System.IO.Compression;
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

        public static byte[] CompressIntArray(int[] data)
        {
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionLevel.Optimal))
            using (var bw = new BinaryWriter(gzip))
            {
                foreach (var val in data)
                    bw.Write(val);
            }
            return ms.ToArray();
        }

        public static int[] DecompressToIntArray(byte[] compressed)
        {
            using var ms = new MemoryStream(compressed);
            using var gzip = new GZipStream(ms, CompressionMode.Decompress);
            using var br = new BinaryReader(gzip);
            var list = new List<int>();
            try
            {
                while (true)
                    list.Add(br.ReadInt32());
            }
            catch (EndOfStreamException)
            {
                return list.ToArray();
            }
        }
    }
}

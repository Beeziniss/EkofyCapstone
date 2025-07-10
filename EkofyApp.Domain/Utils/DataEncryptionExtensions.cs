using EkofyApp.Domain.Exceptions;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace EkofyApp.Domain.Utils;

public sealed class DataEncryptionExtensions
{
    public static string Encrypt(string plainText)
    {
        string key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? throw new UnconfiguredEnvironmentCustomException("ENCRYPTION_KEY is not set in the environment");
        string iv = Environment.GetEnvironmentVariable("ENCRYPTION_IV") ?? throw new UnconfiguredEnvironmentCustomException("ENCRYPTION_IV is not set in the environment");

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
        using MemoryStream ms = new();
        using (GZipStream gzip = new(ms, CompressionLevel.Optimal))
        using (BinaryWriter bw = new(gzip))
        {
            foreach (int val in data)
            {
                bw.Write(val);
            }
        }
        return ms.ToArray();
    }

    public static int[] DecompressToIntArray(byte[] compressed)
    {
        using MemoryStream ms = new(compressed);
        using GZipStream gzip = new(ms, CompressionMode.Decompress);
        using BinaryReader br = new(gzip);
        List<int> list = [];
        try
        {
            while (true)
            {
                list.Add(br.ReadInt32());
            }
        }
        catch (EndOfStreamException)
        {
            return list.ToArray();
        }
    }
}

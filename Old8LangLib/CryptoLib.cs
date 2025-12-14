using System.Security.Cryptography;
using System.Text;

namespace Old8LangLib;

public static class CryptoLib
{
    public static string AesDecrypt(string cipherText, string key, string iv)
    {
        byte[] cipherBytes = Convert.FromBase64String(cipherText);
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

        using Aes aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;

        ICryptoTransform decryptor = aes.CreateDecryptor();
        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Write);

        cs.Write(cipherBytes, 0, cipherBytes.Length);
        cs.FlushFinalBlock();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
    
    public static string AesEncrypt(string plainText, string key, string iv)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
        using Aes aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        ICryptoTransform encryptor = aes.CreateEncryptor();
        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(plainBytes, 0, plainBytes.Length);
        cs.FlushFinalBlock();
        return Convert.ToBase64String(ms.ToArray());
    }
    
    public static string RsaDecrypt(string cipherText, string privateKey)
    {
        byte[] cipherBytes = Convert.FromBase64String(cipherText);
        using RSA rsa = RSA.Create();
        rsa.FromXmlString(privateKey);
        return Encoding.UTF8.GetString(rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256));
    }
    
    public static string RsaEncrypt(string plainText, string publicKey)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        using RSA rsa = RSA.Create();
        rsa.FromXmlString(publicKey);
        return Convert.ToBase64String(rsa.Encrypt(plainBytes, RSAEncryptionPadding.OaepSHA256));
    }
    
    public static string Sha256Hash(string input)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    public static string Sha512Hash(string input)
    {
        using SHA512 sha512 = SHA512.Create();
        byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    public static string HmacSha256Hash(string input, string key)
    {
        using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(key));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    public static string HmacSha512Hash(string input, string key)
    {
        using HMACSHA512 hmac = new(Encoding.UTF8.GetBytes(key));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    public static string Base64Encode(string input)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    }
    
    public static string Base64Decode(string input)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(input));
    }
    
    public static string XorEncrypt(string input, string key)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] outputBytes = new byte[inputBytes.Length];
        for (int i = 0; i < inputBytes.Length; i++)
        {
            outputBytes[i] = (byte)(inputBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }
        return Convert.ToBase64String(outputBytes);
    }
    
    public static string XorDecrypt(string input, string key)
    {
        byte[] inputBytes = Convert.FromBase64String(input);
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] outputBytes = new byte[inputBytes.Length];
        for (int i = 0; i < inputBytes.Length; i++)
        {
            outputBytes[i] = (byte)(inputBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }
        return Encoding.UTF8.GetString(outputBytes);
    }
}
using System.Security.Cryptography;
using System.Text;

namespace Utility.AuthProvider.AESEncryption
{
    public class CustomAESEncryption : ICustomAESEncryption
    {
        private readonly byte[] _key;

        public CustomAESEncryption()
        {
            var aesKey = Environment.GetEnvironmentVariable("AES_KEY");
            ArgumentException.ThrowIfNullOrWhiteSpace(aesKey, "AES_KEY environment variable is required.");
            _key = Convert.FromBase64String(aesKey);
        }

        public string Encrypt(string plainText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV: [16-byte IV][ciphertext]
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherTextBase64)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cipherTextBase64);

            var fullBytes = Convert.FromBase64String(cipherTextBase64);
            var iv = fullBytes[..16];
            var cipher = fullBytes[16..];

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }
    }
}

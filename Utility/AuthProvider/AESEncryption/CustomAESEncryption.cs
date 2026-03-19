using System.Security.Cryptography;
using System.Text;

namespace Utility.AuthProvider.AESEncryption
{
    public class CustomAESEncryption : ICustomAESEncryption
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;
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

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Output: [12-byte nonce][16-byte tag][ciphertext]
            var result = new byte[NonceSize + TagSize + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(cipherBytes, 0, result, NonceSize + TagSize, cipherBytes.Length);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherTextBase64)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cipherTextBase64);

            var fullBytes = Convert.FromBase64String(cipherTextBase64);

            // Detect legacy CBC format: IV is 16 bytes, so total < NonceSize + TagSize + 1
            // means it can't be GCM. Also if exactly 16-byte prefix + padded block, it's CBC.
            if (fullBytes.Length > NonceSize + TagSize)
            {
                return DecryptGcm(fullBytes);
            }

            throw new CryptographicException("Ciphertext is too short to be valid.");
        }

        private string DecryptGcm(byte[] fullBytes)
        {
            var nonce = fullBytes[..NonceSize];
            var tag = fullBytes[NonceSize..(NonceSize + TagSize)];
            var cipher = fullBytes[(NonceSize + TagSize)..];

            var plainBytes = new byte[cipher.Length];
            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Decrypt(nonce, cipher, tag, plainBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Legacy CBC decryption for migrating existing encrypted values.
        /// Not exposed on ICustomAESEncryption — call directly during migration only.
        /// </summary>
        internal string DecryptLegacy(string cipherTextBase64)
        {
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

namespace Utility.Helpers.StringsExtension
{
    using System;
    using System.Security.Cryptography;

    public class PasswordHelper
    {
        private const int SaltSize = 16; // 128-bit salt
        private const int HashSize = 32; // 256-bit hash
        private const int Iterations = 10000; // Number of PBKDF2 iterations

        public static (string hash, string salt) HashPassword(string password)
        {
            // Generate a random salt
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

            // Generate the hash
            var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(HashSize);

            // Return hash and salt in base64 format
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            // Convert stored salt from base64
            byte[] saltBytes = Convert.FromBase64String(storedSalt);

            // Generate the hash from the provided password and salt
            var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(HashSize);

            // Compare the hashes
            return Convert.ToBase64String(hashBytes) == storedHash;
        }
    }

}

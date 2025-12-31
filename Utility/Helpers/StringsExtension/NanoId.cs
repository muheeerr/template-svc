using System.Security.Cryptography;

namespace Utility.Helpers.StringsExtension
{
    public class Genertor
    {
        public static string Generate(int size = 6, string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            int alphabetLength = alphabet.Length;
            using var rng = RandomNumberGenerator.Create();

            char[] id = new char[size];
            byte[] randomBytes = new byte[1];

            for (int i = 0; i < size; i++)
            {
                do
                {
                    rng.GetBytes(randomBytes);
                } while (randomBytes[0] >= alphabetLength * (256 / alphabetLength));

                id[i] = alphabet[randomBytes[0] % alphabetLength];
            }
            return new string(id);
        }
        public static string GenerateQrCodeId()
        {
            string letters = Generate(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            string numbers = Generate(3, "0123456789");
            return $"QR-{letters}{numbers}";
        }
    }
}

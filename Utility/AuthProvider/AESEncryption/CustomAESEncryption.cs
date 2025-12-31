using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Utility.AuthProvider.AESEncryption
{
    public class CustomAESEncryption:ICustomAESEncryption
    {
        readonly  string customKey;
        readonly string customIV;
        readonly string _aes;
        readonly CipherMode encryptionMode;
        readonly PaddingMode paddingMode;
        readonly Aes myAes;
        public CustomAESEncryption(IConfiguration configuration)
        {
            string? secretKey = configuration.GetSection("TOTP:Key").Value;
            string? ivString = configuration.GetSection("TOTP:IV").Value;
            ArgumentNullException.ThrowIfNull(secretKey, nameof(secretKey));
            ArgumentNullException.ThrowIfNull(ivString, nameof(ivString));
            customKey = secretKey;
            customIV = ivString;
            encryptionMode = CipherMode.CBC;
            paddingMode= PaddingMode.PKCS7;
            myAes = Aes.Create();
            myAes.Key = Encoding.UTF8.GetBytes(customKey); ;
            myAes.IV = Encoding.UTF8.GetBytes(customIV);
            myAes.Mode = encryptionMode;
            myAes.Padding = paddingMode;
        }
        public byte[] Encrypt(string plainText)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            byte[] encrypted;


            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = myAes.Key;
                aesAlg.IV = myAes.IV;
                aesAlg.Mode = myAes.Mode;
                aesAlg.Padding = myAes.Padding;


                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);


                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            swEncrypt.Write(plainText);
                        }
                    }

                    encrypted = msEncrypt.ToArray();
                }
            }


            return encrypted;
        }

        public string Decrypt(byte[] cipherText)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
           


            string plaintext = null;


            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = myAes.Key;
                aesAlg.IV = myAes.IV;
                aesAlg.Mode = myAes.Mode;
                aesAlg.Padding = myAes.Padding;


                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);


                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}

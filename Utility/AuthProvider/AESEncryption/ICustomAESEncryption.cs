namespace Utility.AuthProvider.AESEncryption
{
    public interface ICustomAESEncryption
    {
        string Decrypt(byte[] cipherText);
        byte[] Encrypt(string plainText);

    }
}

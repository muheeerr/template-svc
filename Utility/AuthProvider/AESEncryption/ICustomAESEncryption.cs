namespace Utility.AuthProvider.AESEncryption
{
    public interface ICustomAESEncryption
    {
        string Decrypt(string cipherTextBase64);
        string Encrypt(string plainText);
    }
}

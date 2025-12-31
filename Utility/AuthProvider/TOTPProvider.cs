using OtpSharp;

namespace Utility.AuthProvider
{
    public static class TOTPProvider
    {


        public static string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoder.Encode(key);
        }

        public static string GenerateTotp(string secret, int step, int digits)
        {
            var key = Base32Encoder.Decode(secret);
            var totp = new Totp(key, step: step, totpSize: digits);
            
            return totp.ComputeTotp(DateTime.UtcNow);
        }
        public static int GetRemainingSeconds(string secret, int step, int digits)
        {
            var key = Base32Encoder.Decode(secret);
            var totp = new Totp(key, step: step, totpSize: digits);

            return totp.RemainingSeconds();
        }

        public static  bool ValidateTotp(string secret, string code, out long timeStepMatched, int step, int digits)
        {
            var key = Base32Encoder.Decode(secret);
            var totp = new Totp(key, step: step, totpSize: digits);
            return totp.VerifyTotp(DateTime.UtcNow, code, out timeStepMatched, new VerificationWindow(1, 1));
        }
    }
}
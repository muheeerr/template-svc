namespace Utility.AuthProvider
{
    public static class Base32Encoder
    {
        private const int encodedBitCount = 5;

        private const int byteBitCount = 8;

        private const string encodingChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        private const char paddingCharacter = '=';

        public static string Encode(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Length == 0)
            {
                throw new ArgumentException("data must not be empty");
            }

            int num = (int)decimal.Ceiling((decimal)data.Length / 5m) * 8;
            char[] array = new char[num];
            byte b = 0;
            short num2 = 5;
            int num3 = 0;
            foreach (byte b2 in data)
            {
                b = (byte)(b | (b2 >> 8 - num2));
                array[num3++] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[b];
                if (num2 <= 3)
                {
                    b = (byte)((uint)(b2 >> 3 - num2) & 0x1Fu);
                    array[num3++] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[b];
                    num2 += 5;
                }

                num2 -= 3;
                b = (byte)((uint)(b2 << (int)num2) & 0x1Fu);
            }

            if (num3 != num)
            {
                array[num3++] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[b];
            }

            while (num3 < num)
            {
                array[num3++] = '=';
            }

            return new string(array);
        }

        public static byte[] Decode(string base32)
        {
            if (string.IsNullOrEmpty(base32))
            {
                throw new ArgumentNullException("base32");
            }

            string text = base32.ToUpperInvariant().TrimEnd(new char[1] { '=' });
            string text2 = text;
            foreach (char value in text2)
            {
                if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(value) < 0)
                {
                    throw new ArgumentException("base32 contains illegal characters");
                }
            }

            int num = text.Length * 5 / 8;
            byte[] array = new byte[num];
            byte b = 0;
            short num2 = 8;
            int num3 = 0;
            int num4 = 0;
            string text3 = text;
            foreach (char value2 in text3)
            {
                int num5 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(value2);
                if (num2 > 5)
                {
                    num3 = num5 << num2 - 5;
                    b = (byte)(b | num3);
                    num2 -= 5;
                }
                else
                {
                    num3 = num5 >> 5 - num2;
                    b = (byte)(b | num3);
                    array[num4++] = b;
                    b = (byte)(num5 << 3 + num2);
                    num2 += 3;
                }
            }

            return array;
        }
    }

}

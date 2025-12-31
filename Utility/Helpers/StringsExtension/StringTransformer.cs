namespace Utility.Helpers.StringsExtension
{
    public static class StringTransformer
    {

        public static string GenerateRandomOTP()
        {
            // Create a new instance of the Random class
            Random random = new Random();

            // Generate a random number between 1000 and 9999
            int otp = random.Next(1000, 10000);

            // Convert the number to a string and return it
            return otp.ToString("D4");
        }
        public static string ToShortenUrl(this string input)
        {
            
            var segments = input.Split('/');

            
            if (segments.Length < 4)
            {
                throw new ArgumentException("Input string must have at least 4 segments.");
            }
             
         
           
            var secondSegment = segments[1];
            var thirdSegment = segments[2];
            var fourthSegment = segments.Length > 3 ? segments[3] : string.Empty;

            var firstTransformed = secondSegment.Length > 0
                ? $"{secondSegment.First()}{secondSegment.Last()}"
                : string.Empty;

            var secondTransformed = thirdSegment.Length >= 2
                ? $"{thirdSegment.Substring(0, 2)}{thirdSegment.Substring(thirdSegment.Length - 2, 2)}"
                : string.Empty;

            var thirdTransformed = fourthSegment.Length >= 3
                ? $"{fourthSegment.Substring(0, 3)}{fourthSegment.Substring(fourthSegment.Length - 3, 3)}"
                : thirdSegment; // If less than 6, take all
            
            // Concatenate the results
            var result = $"{firstTransformed}{secondTransformed}{thirdTransformed}";

            return result;
        }
    }
}

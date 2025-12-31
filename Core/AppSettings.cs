using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Core
{
    internal static class AppSettings
    {
        public static readonly Regex PhoneNumber =new Regex(@"^\+?\d[\d\s\-]{6,14}$", RegexOptions.Compiled);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Constants
{
    public static class ApiConfigs
    {
        public static string AuthServiceUrl = Environment.GetEnvironmentVariable("AUTH_SERVICE_URL") ?? "http://localhost:7089";
    }
}

namespace Utility.GenericRepository.CommonModels
{
    public class OrderRule
    {
        public static string Asc = "asc";
        public static string Desc = "desc";
    }
    public class PaginatedResponse: PaginationParameters
    {
       public int TotalCount { get; set; }
    }
    public class PaginationParameters
    {
        public int PageSize { get; set; } = 10; // Default page size
        public string Order { get; set; } = OrderRule.Asc; // Default order
        public string Cursor { get; set; } = "NA"; // Last cursor value (for continuation)
        public string Property { get; set; } = "Id"; // Default property to sort by
        public string FilterPropertyName { get; set; } = "NA";
        public string FilterPropertyValue { get; set; } = "NA";

        public static bool TryParse(string value, out PaginationParameters result)
        {
            result = null;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var parameters = new PaginationParameters();
            var keyValuePairs = value.Split('&');

            foreach (var pair in keyValuePairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length != 2)
                {
                    continue;
                }

                var key = keyValue[0].ToLower();
                var val = keyValue[1];

                switch (key)
                {
                    case "pagesize":
                        if (int.TryParse(val, out int pageSize))
                        {
                            parameters.PageSize = pageSize;
                        }
                        break;
                    case "order":
                        parameters.Order = val;
                        break;
                    case "cursor":
                        parameters.Cursor = val;
                        break;
                    case "property":
                        parameters.Property = val;
                        break;
                    case "filterpropertyname":
                        parameters.FilterPropertyName = val;
                        break;
                    case "filterpropertyvalue":
                        parameters.FilterPropertyValue = val;
                        break;
                    default:
                        break;
                }
            }

            result = parameters;
            return true;
        }
    }

    public class PageInformationResponse<T>
    {
        public PaginatedResponse Page { get; set; }
        public T Data { get; set; }
    }
}

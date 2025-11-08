namespace SimpleLauncher.Infrastructure.MonitorAPI.Utils
{
    public static class UriUtils
    {
        public static string BuildUrlWithQuery(string baseUrl, object parameters)
        {
            var properties = parameters.GetType().GetProperties();
            var queryParams = properties
                .Where(p => p.GetValue(parameters) != null)
                .Select(p => $"{p.Name}={Uri.EscapeDataString(p.GetValue(parameters)?.ToString() ?? "")}");

            return $"{baseUrl}?{string.Join("&", queryParams)}";
        }
    }
}

namespace TanoDevClip.DevTools.Tools
{
    public static class UrlTool
    {
        public static DevToolResult Encode(string? value)
        {
            return DevToolResult.Success(Uri.EscapeDataString(value ?? string.Empty));
        }

        public static DevToolResult Decode(string? value)
        {
            try
            {
                return DevToolResult.Success(Uri.UnescapeDataString((value ?? string.Empty).Replace("+", " ", StringComparison.Ordinal)));
            }
            catch (Exception exception) when (exception is ArgumentException or UriFormatException)
            {
                return DevToolResult.Error(exception.Message);
            }
        }
    }
}


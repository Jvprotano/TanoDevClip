using System.Text;

namespace TanoDevClip.DevTools
{
    public static class Base64Tool
    {
        public static DevToolResult Encode(string? value)
        {
            return DevToolResult.Success(Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        public static DevToolResult Decode(string? value)
        {
            try
            {
                var bytes = Convert.FromBase64String((value ?? string.Empty).Trim());
                return DevToolResult.Success(Encoding.UTF8.GetString(bytes));
            }
            catch (FormatException exception)
            {
                return DevToolResult.Error(exception.Message);
            }
        }
    }
}


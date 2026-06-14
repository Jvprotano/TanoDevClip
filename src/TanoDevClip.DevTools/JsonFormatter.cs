using System.Text.Json;

namespace TanoDevClip.DevTools
{
    public static class JsonFormatter
    {
        public static DevToolResult Format(string? value, bool minify)
        {
            try
            {
                using var document = JsonDocument.Parse(value ?? string.Empty);
                return DevToolResult.Success(JsonSerializer.Serialize(
                    document.RootElement,
                    new JsonSerializerOptions { WriteIndented = !minify }));
            }
            catch (JsonException exception)
            {
                return DevToolResult.Error(exception.Message);
            }
        }
    }
}


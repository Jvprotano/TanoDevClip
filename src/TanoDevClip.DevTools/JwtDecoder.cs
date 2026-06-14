using System.Text;
using System.Text.Json;

namespace TanoDevClip.DevTools
{
    public static class JwtDecoder
    {
        private static readonly JsonSerializerOptions IndentedJsonOptions = new()
        {
            WriteIndented = true
        };

        public static DevToolResult Decode(string? token)
        {
            var parts = (token ?? string.Empty).Trim().Split('.');

            if (parts.Length is < 2 or > 3)
            {
                return DevToolResult.Error("JWT must have header.payload or header.payload.signature.");
            }

            try
            {
                var header = JsonSerializer.Deserialize<JsonElement>(DecodeBase64Url(parts[0]));
                var payload = JsonSerializer.Deserialize<JsonElement>(DecodeBase64Url(parts[1]));
                var result = new
                {
                    header,
                    payload,
                    signature = parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]) ? "present" : "missing"
                };

                return DevToolResult.Success(JsonSerializer.Serialize(result, IndentedJsonOptions));
            }
            catch (Exception exception) when (exception is FormatException or JsonException)
            {
                return DevToolResult.Error(exception.Message);
            }
        }

        private static string DecodeBase64Url(string value)
        {
            var normalized = value.Replace('-', '+').Replace('_', '/');
            var padded = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
            return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }
    }
}


using System.Text.Json;
using System.Text.RegularExpressions;

namespace TanoDevClip.DevTools
{
    public static class RegexTool
    {
        private static readonly JsonSerializerOptions IndentedJsonOptions = new()
        {
            WriteIndented = true
        };

        public static DevToolResult Evaluate(
            string? pattern,
            string? flags,
            string? sample,
            string? replacement)
        {
            try
            {
                var options = BuildOptions(flags);
                var regex = new Regex(pattern ?? string.Empty, options, TimeSpan.FromMilliseconds(250));
                var matches = regex.Matches(sample ?? string.Empty)
                    .Take(100)
                    .Select((match, index) => new
                    {
                        name = $"match {index + 1}",
                        index = match.Index,
                        value = match.Value,
                        groups = match.Groups
                            .Values
                            .Skip(1)
                            .Select(group => group.Success ? group.Value : null)
                    })
                    .ToList();

                var result = new
                {
                    valid = true,
                    flags = flags ?? string.Empty,
                    matches,
                    replacement = string.IsNullOrEmpty(replacement)
                        ? null
                        : regex.Replace(sample ?? string.Empty, replacement)
                };

                return DevToolResult.Success(JsonSerializer.Serialize(result, IndentedJsonOptions));
            }
            catch (Exception exception) when (exception is ArgumentException or RegexMatchTimeoutException)
            {
                return DevToolResult.Error(exception.Message);
            }
        }

        private static RegexOptions BuildOptions(string? flags)
        {
            var options = RegexOptions.None;
            var seenFlags = new HashSet<char>();

            foreach (var flag in flags ?? string.Empty)
            {
                if (!seenFlags.Add(flag))
                {
                    throw new ArgumentException("Regex flags cannot be repeated.");
                }

                options |= flag switch
                {
                    'i' => RegexOptions.IgnoreCase,
                    'm' => RegexOptions.Multiline,
                    's' => RegexOptions.Singleline,
                    'n' => RegexOptions.ExplicitCapture,
                    _ => throw new ArgumentException("Supported flags: i, m, s, n.")
                };
            }

            return options;
        }
    }
}


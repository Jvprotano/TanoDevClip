using System.Text.Json;
using System.Text.RegularExpressions;
using TanoDevClip.Core.Clipboard;

namespace TanoDevClip.Core.Classification;

public sealed class DefaultClipboardClassifier : IClipboardClassifier
{
    public ClipType Classify(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ClipType.Unknown;

        var trimmed = content.Trim();

        if (IsJwt(trimmed))
            return ClipType.Jwt;

        if (Guid.TryParse(trimmed, out _))
            return ClipType.Guid;

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return ClipType.Url;

        if (IsJson(trimmed))
            return ClipType.Json;

        if (LooksLikeSql(trimmed))
            return ClipType.Sql;

        if (LooksLikeEmail(trimmed))
            return ClipType.Email;

        return ClipType.Text;
    }

    private static bool IsJson(string value)
    {
        if (!value.StartsWith('{') && !value.StartsWith('['))
            return false;

        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsJwt(string value)
    {
        var parts = value.Split('.');
        return parts.Length == 3 &&
               parts.All(p => p.Length > 0) &&
               parts.All(p => Regex.IsMatch(p, "^[A-Za-z0-9_-]+$"));
    }

    private static bool LooksLikeSql(string value)
    {
        var upper = value.ToUpperInvariant();

        return upper.StartsWith("SELECT ") ||
               upper.StartsWith("INSERT ") ||
               upper.StartsWith("UPDATE ") ||
               upper.StartsWith("DELETE ") ||
               upper.StartsWith("CREATE ") ||
               upper.StartsWith("ALTER ") ||
               upper.StartsWith("DROP ");
    }

    private static bool LooksLikeEmail(string value)
    {
        return Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
namespace TanoDevClip.Core.Settings;

public sealed class AppSettings
{
    public string HotKey { get; set; } = AppSettingsDefaults.HotKey;
    public List<string> EnabledTools { get; set; } = [.. AppSettingsDefaults.EnabledTools];

    public AppSettings Clone()
    {
        return new AppSettings
        {
            HotKey = HotKey,
            EnabledTools = [.. EnabledTools]
        };
    }
}

public static class AppSettingsDefaults
{
    public const string HotKey = "Ctrl+Alt+Space";

    public static readonly string[] EnabledTools =
    [
        "guid",
        "cpf",
        "cnpj",
        "lorem",
        "string",
        "jwt",
        "json",
        "base64",
        "url",
        "regex"
    ];

    public static AppSettings Create()
    {
        return new AppSettings();
    }
}

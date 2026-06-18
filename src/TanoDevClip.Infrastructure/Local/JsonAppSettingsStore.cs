using System.Text.Json;
using TanoDevClip.Core.Settings;

namespace TanoDevClip.Infrastructure.Local;

public sealed class JsonAppSettingsStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(path))
        {
            var defaults = AppSettingsDefaults.Create();
            await SaveAsync(defaults);
            return defaults;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions);
            return Normalize(settings);
        }
        catch
        {
            var defaults = AppSettingsDefaults.Create();
            await SaveAsync(defaults);
            return defaults;
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);
        var normalized = Normalize(settings);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions);
    }

    public Task<AppSettings> ResetAsync()
    {
        var defaults = AppSettingsDefaults.Create();
        return SaveAndReturnAsync(defaults);
    }

    private async Task<AppSettings> SaveAndReturnAsync(AppSettings settings)
    {
        await SaveAsync(settings);
        return settings;
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        var normalized = settings?.Clone() ?? AppSettingsDefaults.Create();

        if (string.IsNullOrWhiteSpace(normalized.HotKey))
        {
            normalized.HotKey = AppSettingsDefaults.HotKey;
        }

        var knownTools = AppSettingsDefaults.EnabledTools.ToHashSet(StringComparer.OrdinalIgnoreCase);
        normalized.EnabledTools = (normalized.EnabledTools ?? [])
            .Where(tool => knownTools.Contains(tool))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized;
    }
}

using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using TanoDevClip.Core.Classification;
using TanoDevClip.Core.Clipboard;
using TanoDevClip.Core.Repositories;
using TanoDevClip.DevTools;

namespace TanoDevClip.App;

public partial class MainWindow : Window
{
    private const string DevServerUrl = "http://localhost:5173";
    private const int HotKeyId = 0x5443;
    private const int WmClipboardUpdate = 0x031D;
    private const int WmHotKey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkV = 0x56;

    private readonly IClipRepository _clipRepository;
    private readonly IClipboardClassifier _clipboardClassifier;
    private readonly GuidGenerator _guidGenerator;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private HwndSource? _hwndSource;
    private bool _hotKeyRegistered;
    private string? _lastCapturedHash;
    private string? _ignoreNextClipboardHash;

    public MainWindow(
        IClipRepository clipRepository,
        IClipboardClassifier clipboardClassifier,
        GuidGenerator guidGenerator)
    {
        _clipRepository = clipRepository;
        _clipboardClassifier = clipboardClassifier;
        _guidGenerator = guidGenerator;

        InitializeComponent();

        Loaded += MainWindow_Loaded;
        SourceInitialized += MainWindow_SourceInitialized;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _hwndSource?.AddHook(WndProc);

        if (_hwndSource is not null)
        {
            AddClipboardFormatListener(_hwndSource.Handle);
            _hotKeyRegistered = RegisterHotKey(_hwndSource.Handle, HotKeyId, ModControl | ModShift, VkV);
        }
    }

    private async Task InitializeWebViewAsync()
    {
        await AppWebView.EnsureCoreWebView2Async();

        AppWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        AppWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        AppWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

        AppWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        AppWebView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            await SendMessageToUiAsync(new
            {
                type = "app:info",
                payload = CreateAppInfoPayload()
            });
            await SendClipsListAsync(new ClipSearchFilter());
            await FocusSearchAsync();
        };

        AppWebView.Source = await ResolveUiEntryPointAsync();
    }

    private async Task<Uri> ResolveUiEntryPointAsync()
    {
        if (await IsDevServerAvailableAsync())
        {
            return new Uri(DevServerUrl);
        }

        var distIndex = FindUiDistIndex();
        return distIndex is not null
            ? new Uri(distIndex)
            : new Uri(DevServerUrl);
    }

    private static async Task<bool> IsDevServerAvailableAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));
            using var http = new HttpClient();
            using var response = await http.GetAsync(DevServerUrl, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindUiDistIndex()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "TanoDevClip.UI",
                "dist",
                "index.html");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private async void CoreWebView2_WebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            var type = root.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : null;

            switch (type)
            {
                case "app:get-info":
                    await SendMessageToUiAsync(new
                    {
                        type = "app:info",
                        payload = CreateAppInfoPayload()
                    });
                    break;

                case "clips:list":
                    await SendClipsListAsync(ReadClipSearchFilter(root));
                    break;

                case "clips:copy":
                    await CopyClipAsync(ReadPayloadString(root, "id"));
                    break;

                case "clips:toggle-pin":
                    await TogglePinAsync(ReadPayloadString(root, "id"));
                    break;

                case "devtools:generate-guid":
                    await GenerateGuidAsync(root);
                    break;

                case "devtools:copy-guid":
                    await CopyGeneratedGuidAsync(ReadPayloadString(root, "content"));
                    break;
            }
        }
        catch (Exception exception)
        {
            await SendErrorAsync(exception.Message);
        }
    }

    private async Task SendClipsListAsync(ClipSearchFilter filter)
    {
        var clips = await _clipRepository.SearchAsync(filter);

        await SendMessageToUiAsync(new
        {
            type = "clips:list-result",
            payload = new
            {
                clips = clips.Select(ToPayload)
            }
        });
    }

    private async Task CopyClipAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var clip = await _clipRepository.GetByIdAsync(id);
        if (clip is null)
        {
            await SendErrorAsync("Clip not found.");
            return;
        }

        CopyTextToClipboard(clip.Content);
        await _clipRepository.IncrementUseAsync(id);
        await NotifyClipsUpdatedAsync("copy");
        await SendClipsListAsync(new ClipSearchFilter());
        Hide();
    }

    private async Task TogglePinAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        await _clipRepository.TogglePinAsync(id);
        await NotifyClipsUpdatedAsync("pin");
        await SendClipsListAsync(new ClipSearchFilter());
    }

    private async Task GenerateGuidAsync(JsonElement root)
    {
        var format = ReadPayloadString(root, "format");
        var parsedFormat = format switch
        {
            "no-hyphens" => GuidFormat.NoHyphens,
            "uppercase" => GuidFormat.Uppercase,
            _ => GuidFormat.Default
        };

        await SendMessageToUiAsync(new
        {
            type = "devtools:generate-guid-result",
            payload = new
            {
                value = _guidGenerator.Generate(parsedFormat)
            }
        });
    }

    private async Task CopyGeneratedGuidAsync(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var clip = CreateClipItem(content, ClipType.Guid, "Generated GUID", "TanoDev Clip", null);
        await _clipRepository.SaveAsync(clip);
        CopyTextToClipboard(content);
        await NotifyClipsUpdatedAsync("devtool");
        await SendClipsListAsync(new ClipSearchFilter());
    }

    private async Task CaptureClipboardTextAsync()
    {
        string content;
        try
        {
            if (!System.Windows.Clipboard.ContainsText())
            {
                return;
            }

            content = System.Windows.Clipboard.GetText();
        }
        catch
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var hash = ComputeSha256(content);
        if (hash == _ignoreNextClipboardHash)
        {
            _ignoreNextClipboardHash = null;
            return;
        }

        if (hash == _lastCapturedHash)
        {
            return;
        }

        _lastCapturedHash = hash;
        var source = GetForegroundWindowSource();
        var clipType = _clipboardClassifier.Classify(content);
        var clip = CreateClipItem(content, clipType, CreateClipTitle(content), source.ProcessName, source.WindowTitle);

        await _clipRepository.SaveAsync(clip);
        await NotifyClipsUpdatedAsync("clipboard");
        await SendClipsListAsync(new ClipSearchFilter());
    }

    private ClipItem CreateClipItem(
        string content,
        ClipType clipType,
        string? title,
        string? sourceApp,
        string? sourceWindowTitle)
    {
        return new ClipItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Content = content,
            ContentHash = ComputeSha256(content),
            ClipType = clipType,
            Title = title,
            SourceApp = sourceApp,
            SourceWindowTitle = sourceWindowTitle,
            SourceUrl = null,
            IsPinned = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedAt = null,
            UseCount = 0
        };
    }

    private void CopyTextToClipboard(string content)
    {
        _ignoreNextClipboardHash = ComputeSha256(content);
        System.Windows.Clipboard.SetText(content);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmClipboardUpdate)
        {
            _ = Dispatcher.InvokeAsync(CaptureClipboardTextAsync);
            handled = true;
        }
        else if (msg == WmHotKey && wParam.ToInt32() == HotKeyId)
        {
            ToggleWindowFromHotKey();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private async void ToggleWindowFromHotKey()
    {
        if (IsVisible && IsActive)
        {
            Hide();
            return;
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
        await FocusSearchAsync();
    }

    private Task NotifyClipsUpdatedAsync(string reason)
    {
        return SendMessageToUiAsync(new
        {
            type = "clips:updated",
            payload = new { reason }
        });
    }

    private Task FocusSearchAsync()
    {
        return SendMessageToUiAsync(new
        {
            type = "app:focus-search"
        });
    }

    private Task SendErrorAsync(string message)
    {
        return SendMessageToUiAsync(new
        {
            type = "app:error",
            payload = new { message }
        });
    }

    private Task SendMessageToUiAsync(object message)
    {
        if (AppWebView.CoreWebView2 is null)
        {
            return Task.CompletedTask;
        }

        var json = JsonSerializer.Serialize(message, _jsonOptions);
        AppWebView.CoreWebView2.PostWebMessageAsJson(json);
        return Task.CompletedTask;
    }

    private static object CreateAppInfoPayload()
    {
        return new
        {
            name = "TanoDev Clip",
            version = "0.1.0",
            environment = "Development",
            hotkey = "Ctrl+Shift+J"
        };
    }

    private static ClipSearchFilter ReadClipSearchFilter(JsonElement root)
    {
        var query = ReadPayloadString(root, "query");
        var type = ReadPayloadString(root, "clipType");

        return new ClipSearchFilter
        {
            Query = query,
            ClipType = Enum.TryParse<ClipType>(type, ignoreCase: true, out var clipType)
                ? clipType
                : null,
            Limit = ReadPayloadInt(root, "limit") ?? 100
        };
    }

    private static string? ReadPayloadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty("payload", out var payload) ||
            !payload.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.GetString();
    }

    private static int? ReadPayloadInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty("payload", out var payload) ||
            !payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.TryGetInt32(out var value) ? value : null;
    }

    private static object ToPayload(ClipItem clip)
    {
        return new
        {
            id = clip.Id,
            content = clip.Content,
            contentHash = clip.ContentHash,
            clipType = clip.ClipType.ToString(),
            title = clip.Title,
            sourceApp = clip.SourceApp,
            sourceWindowTitle = clip.SourceWindowTitle,
            sourceUrl = clip.SourceUrl,
            isPinned = clip.IsPinned,
            createdAt = clip.CreatedAt,
            lastUsedAt = clip.LastUsedAt,
            useCount = clip.UseCount
        };
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CreateClipTitle(string content)
    {
        var compact = string.Join(
            ' ',
            content.Trim()
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return compact.Length <= 90 ? compact : $"{compact[..90]}...";
    }

    private static ForegroundSource GetForegroundWindowSource()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return new ForegroundSource(null, null);
            }

            var titleBuilder = new StringBuilder(512);
            _ = GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
            _ = GetWindowThreadProcessId(hwnd, out var processId);

            string? processName = null;
            if (processId > 0)
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }

            return new ForegroundSource(
                processName,
                titleBuilder.Length > 0 ? titleBuilder.ToString() : null);
        }
        catch
        {
            return new ForegroundSource(null, null);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_hwndSource is not null)
        {
            RemoveClipboardFormatListener(_hwndSource.Handle);

            if (_hotKeyRegistered)
            {
                UnregisterHotKey(_hwndSource.Handle, HotKeyId);
            }

            _hwndSource.RemoveHook(WndProc);
        }
    }

    private sealed record ForegroundSource(string? ProcessName, string? WindowTitle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}

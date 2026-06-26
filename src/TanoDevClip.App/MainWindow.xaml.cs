using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using TanoDevClip.Core.Classification;
using TanoDevClip.Core.Clipboard;
using TanoDevClip.Core.Repositories;
using TanoDevClip.Core.Settings;
using TanoDevClip.DevTools;
using TanoDevClip.Infrastructure.Local;

using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace TanoDevClip.App
{
    public partial class MainWindow : Window
    {
        private const string DevServerUrl = "http://localhost:5173";
        private const string UiVirtualHost = "app.tanodevclip.local";
        private const int HotKeyId = 0x5443;
        private const int WmClipboardUpdate = 0x031D;
        private const int WmHotKey = 0x0312;
        private const int WmNcHitTest = 0x0084;
        private const int WmNcLButtonDown = 0x00A1;
        private const int HtCaption = 2;
        private const int HtLeft = 10;
        private const int HtRight = 11;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int HtBottom = 15;
        private const int HtBottomLeft = 16;
        private const int HtBottomRight = 17;
        private const double ResizeGripThickness = 8;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModShift = 0x0004;
        private const uint VkSpace = 0x20;
        private const uint VkV = 0x56;
        private const uint VkD = 0x44;
        private const int SwRestore = 9;
        private const int ImagePreviewMaxPixelSize = 360;

        private readonly IClipRepository _clipRepository;
        private readonly IClipboardClassifier _clipboardClassifier;
        private readonly GuidGenerator _guidGenerator;
        private readonly DevToolRunner _devToolRunner;
        private readonly JsonAppSettingsStore _settingsStore;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        private HwndSource? _hwndSource;
        private Forms.NotifyIcon? _trayIcon;
        private bool _hotKeyRegistered;
        private bool _isExiting;
        private bool _isPreloadingInBackground;
        private bool _centerOnNextVisibleShow;
        private string? _lastCapturedHash;
        private string? _ignoreNextClipboardHash;
        private int _ignoreNextClipboardChanges;
        private AppSettings _settings;
        private IntPtr _returnWindowHandle;

        public MainWindow(
            IClipRepository clipRepository,
            IClipboardClassifier clipboardClassifier,
            GuidGenerator guidGenerator,
            DevToolRunner devToolRunner,
            JsonAppSettingsStore settingsStore,
            AppSettings settings)
        {
            _clipRepository = clipRepository;
            _clipboardClassifier = clipboardClassifier;
            _guidGenerator = guidGenerator;
            _devToolRunner = devToolRunner;
            _settingsStore = settingsStore;
            _settings = NormalizeSettings(settings);

            InitializeComponent();
            InitializeTrayIcon();

            Loaded += MainWindow_Loaded;
            SourceInitialized += MainWindow_SourceInitialized;
        }

        public void BeginStartupPreload()
        {
            _isPreloadingInBackground = true;
            _centerOnNextVisibleShow = true;

            ShowActivated = false;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Opacity = 0;
            Left = -32000;
            Top = -32000;

            Show();
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
                _hotKeyRegistered = TryRegisterConfiguredHotKey(_settings.HotKey);
            }
        }

        private async Task InitializeWebViewAsync()
        {
            await AppWebView.EnsureCoreWebView2Async();
            AppWebView.DefaultBackgroundColor = Drawing.Color.FromArgb(0x07, 0x0B, 0x0D);

#if DEBUG
            AppWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            AppWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

#else
            AppWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            AppWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
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

                if (_isPreloadingInBackground)
                {
                    CompleteStartupPreload();
                    return;
                }

                if (IsVisible)
                {
                    await FocusSearchAsync();
                }
            };

            AppWebView.Source = await ResolveUiEntryPointAsync();
        }

        private Task<Uri> ResolveUiEntryPointAsync()
        {
#if DEBUG
            return ResolveDevelopmentUiEntryPointAsync();
#else
    var packagedUiDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "ui");

    return Task.FromResult(
        MapUiDirectory(packagedUiDirectory));
#endif
        }

#if DEBUG
        private async Task<Uri> ResolveDevelopmentUiEntryPointAsync()
        {
            if (await IsDevServerAvailableAsync())
            {
                return new Uri(DevServerUrl);
            }

            var developmentUiDirectory =
                FindDevelopmentUiDistDirectory();

            if (developmentUiDirectory is not null)
            {
                return MapUiDirectory(developmentUiDirectory);
            }

            var packagedUiDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "ui");

            return MapUiDirectory(packagedUiDirectory);
        }
#endif

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

#if DEBUG
        private static string? FindDevelopmentUiDistDirectory()
        {
            var directory = new DirectoryInfo(
                AppContext.BaseDirectory);

            while (directory is not null)
            {
                var candidate = Path.Combine(
                    directory.FullName,
                    "src",
                    "TanoDevClip.UI",
                    "dist");

                var indexPath = Path.Combine(
                    candidate,
                    "index.html");

                if (File.Exists(indexPath))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
#endif

        private Uri MapUiDirectory(string uiDirectory)
        {
            var indexPath = Path.Combine(
                uiDirectory,
                "index.html");

            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException(
                    $"TanoDev Clip UI was not found at '{indexPath}'. " +
                    "Build the React UI and include it in the application publish directory.",
                    indexPath);
            }

            AppWebView.CoreWebView2
                .SetVirtualHostNameToFolderMapping(
                    UiVirtualHost,
                    uiDirectory,
                    CoreWebView2HostResourceAccessKind.DenyCors);

            return new Uri(
                $"https://{UiVirtualHost}/index.html");
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

                    case "app:hide":
                        Hide();
                        break;

                    case "app:drag-window":
                        DragBorderlessWindow();
                        break;

                    case "settings:save":
                        await SaveSettingsAsync(root);
                        break;

                    case "settings:reset":
                        await ResetSettingsAsync();
                        break;

                    case "clips:list":
                        await SendClipsListAsync(ReadClipSearchFilter(root));
                        break;

                    case "clips:copy":
                        await CopyClipAsync(ReadPayloadString(root, "id"));
                        break;

                    case "clips:paste":
                        await PasteClipAsync(ReadPayloadString(root, "id"));
                        break;

                    case "clips:toggle-pin":
                        await TogglePinAsync(ReadPayloadString(root, "id"));
                        break;

                    case "devtools:generate-guid":
                        await GenerateGuidAsync(root);
                        break;

                    case "devtools:run":
                        await RunDevToolAsync(root);
                        break;

                    case "devtools:copy-guid":
                        await CopyGeneratedGuidAsync(ReadPayloadString(root, "content"));
                        break;

                    case "devtools:generate-string":
                        await GenerateStringAsync(root);
                        break;

                    case "devtools:generate-lorem":
                        await GenerateLoremAsync(root);
                        break;

                    case "devtools:copy-generated":
                        await CopyGeneratedTextAsync(
                            ReadPayloadString(root, "content"),
                            ReadPayloadString(root, "kind"));
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

            if (!await CopyClipToClipboardAsync(clip))
            {
                return;
            }

            await _clipRepository.IncrementUseAsync(id);
            await NotifyClipsUpdatedAsync("copy");
            await SendClipsListAsync(new ClipSearchFilter());
            Hide();
        }

        private async Task PasteClipAsync(string? id)
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

            if (!await CopyClipToClipboardAsync(clip))
            {
                return;
            }

            await _clipRepository.IncrementUseAsync(id);
            await NotifyClipsUpdatedAsync("paste");
            await SendClipsListAsync(new ClipSearchFilter());

            Hide();
            await PasteIntoReturnWindowAsync();
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

        private async Task RunDevToolAsync(JsonElement root)
        {
            if (!root.TryGetProperty("payload", out var payload))
            {
                await SendErrorAsync("Missing dev tool payload.");
                return;
            }

            var request = payload.Deserialize<DevToolRequest>(_jsonOptions);
            if (request is null)
            {
                await SendErrorAsync("Invalid dev tool payload.");
                return;
            }

            if (!_settings.EnabledTools.Contains(request.Tool, StringComparer.OrdinalIgnoreCase))
            {
                await SendMessageToUiAsync(new
                {
                    type = "devtools:run-result",
                    payload = new
                    {
                        status = "error",
                        value = "This tool is disabled in settings."
                    }
                });
                return;
            }

            var result = _devToolRunner.Run(request);
            await SendMessageToUiAsync(new
            {
                type = "devtools:run-result",
                payload = new
                {
                    status = result.IsSuccess ? "ok" : "error",
                    value = result.Value
                }
            });
        }

        private async Task SaveSettingsAsync(JsonElement root)
        {
            if (!root.TryGetProperty("payload", out var payload))
            {
                await SendErrorAsync("Missing settings payload.");
                return;
            }

            var nextSettings = NormalizeSettings(payload.Deserialize<AppSettings>(_jsonOptions));
            if (!TryParseHotKey(nextSettings.HotKey, out _))
            {
                await SendErrorAsync("Invalid hotkey.");
                await SendAppInfoAsync();
                return;
            }

            var previousSettings = _settings.Clone();
            if (!string.Equals(previousSettings.HotKey, nextSettings.HotKey, StringComparison.OrdinalIgnoreCase) &&
                !TryApplyConfiguredHotKey(nextSettings.HotKey))
            {
                _ = TryApplyConfiguredHotKey(previousSettings.HotKey);
                await SendErrorAsync("Hotkey is already in use or could not be registered.");
                await SendAppInfoAsync();
                return;
            }

            _settings = nextSettings;
            await _settingsStore.SaveAsync(_settings);
            await SendSettingsUpdatedAsync();
        }

        private async Task ResetSettingsAsync()
        {
            var defaults = AppSettingsDefaults.Create();
            var previousSettings = _settings.Clone();

            if (!TryApplyConfiguredHotKey(defaults.HotKey))
            {
                _ = TryApplyConfiguredHotKey(previousSettings.HotKey);
                await SendErrorAsync("Default hotkey could not be registered.");
                await SendAppInfoAsync();
                return;
            }

            _settings = await _settingsStore.ResetAsync();
            await SendSettingsUpdatedAsync();
        }

        private async Task GenerateStringAsync(JsonElement root)
        {
            var length = Math.Clamp(ReadPayloadInt(root, "length") ?? 32, 1, 512);
            var includeUppercase = ReadPayloadBool(root, "includeUppercase") ?? true;
            var includeLowercase = ReadPayloadBool(root, "includeLowercase") ?? true;
            var includeNumbers = ReadPayloadBool(root, "includeNumbers") ?? true;
            var includeSymbols = ReadPayloadBool(root, "includeSymbols") ?? false;

            var value = StringGenerator.GenerateRandomString(
                length,
                includeUppercase,
                includeLowercase,
                includeNumbers,
                includeSymbols);

            await SendGeneratedValueAsync(value);
        }

        private async Task GenerateLoremAsync(JsonElement root)
        {
            var mode = ReadPayloadString(root, "mode");
            var amount = Math.Clamp(ReadPayloadInt(root, "amount") ?? 46, 1, 5000);
            var value = mode == "characters"
                ? LoremGenerator.GenerateByCharacters(amount)
                : LoremGenerator.GenerateByWords(Math.Min(amount, 500));

            await SendGeneratedValueAsync(value);
        }

        private Task SendGeneratedValueAsync(string value)
        {
            return SendMessageToUiAsync(new
            {
                type = "devtools:generate-result",
                payload = new { value }
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

        private async Task CopyGeneratedTextAsync(string? content, string? kind)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var clipType = kind == "guid" ? ClipType.Guid : ClipType.Text;
            var title = kind switch
            {
                "guid" => "Generated GUID",
                "cpf" => "Generated or validated CPF",
                "cnpj" => "Generated or validated CNPJ",
                "string" => "Generated random string",
                "lorem" => "Generated lorem ipsum",
                "jwt" => "Decoded JWT",
                "json" => "Formatted JSON",
                "base64" => "Base64 result",
                "url" => "URL encoding result",
                "regex" => "Regex helper result",
                _ => "Generated text"
            };

            var clip = CreateClipItem(content, clipType, title, "TanoDev Clip", null);
            await _clipRepository.SaveAsync(clip);
            CopyTextToClipboard(content);
            await NotifyClipsUpdatedAsync("devtool");
            await SendClipsListAsync(new ClipSearchFilter());
        }

        private async Task CaptureClipboardAsync()
        {
            ClipItem? clip;
            try
            {
                if (System.Windows.Clipboard.ContainsImage())
                {
                    clip = CreateImageClipFromClipboard();
                }
                else if (System.Windows.Clipboard.ContainsText())
                {
                    clip = CreateTextClipFromClipboard();
                }
                else
                {
                    clip = null;
                }
            }
            catch
            {
                return;
            }

            if (clip is null)
            {
                return;
            }

            if (clip.ContentHash == _ignoreNextClipboardHash ||
                _ignoreNextClipboardChanges > 0)
            {
                _ignoreNextClipboardHash = null;
                _ignoreNextClipboardChanges = Math.Max(0, _ignoreNextClipboardChanges - 1);
                return;
            }

            if (clip.ContentHash == _lastCapturedHash)
            {
                return;
            }

            _lastCapturedHash = clip.ContentHash;
            await _clipRepository.SaveAsync(clip);
            await NotifyClipsUpdatedAsync("clipboard");
            await SendClipsListAsync(new ClipSearchFilter());
        }

        private ClipItem? CreateTextClipFromClipboard()
        {
            var content = System.Windows.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var source = GetForegroundWindowSource();
            var clipType = _clipboardClassifier.Classify(content);
            return CreateClipItem(
                content,
                clipType,
                CreateClipTitle(content),
                source.ProcessName,
                source.WindowTitle);
        }

        private ClipItem? CreateImageClipFromClipboard()
        {
            var image = System.Windows.Clipboard.GetImage();
            if (image is null || image.PixelWidth <= 0 || image.PixelHeight <= 0)
            {
                return null;
            }

            var pngBytes = EncodeBitmapToPng(image);
            var previewBytes = EncodeBitmapToPng(CreatePreviewBitmap(image));
            var content = $"Image {image.PixelWidth}x{image.PixelHeight}";
            var source = GetForegroundWindowSource();

            return CreateClipItem(
                content,
                ClipType.Image,
                content,
                source.ProcessName,
                source.WindowTitle,
                pngBytes,
                previewBytes,
                "image/png",
                image.PixelWidth,
                image.PixelHeight);
        }

        private ClipItem CreateClipItem(
            string content,
            ClipType clipType,
            string? title,
            string? sourceApp,
            string? sourceWindowTitle,
            byte[]? binaryContent = null,
            byte[]? previewContent = null,
            string? contentMimeType = null,
            int? imageWidth = null,
            int? imageHeight = null)
        {
            var contentHash = binaryContent is null
                ? ComputeSha256(content)
                : ComputeSha256(binaryContent);

            return new ClipItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Content = content,
                ContentHash = contentHash,
                ClipType = clipType,
                BinaryContent = binaryContent,
                PreviewContent = previewContent,
                ContentMimeType = contentMimeType,
                ImageWidth = imageWidth,
                ImageHeight = imageHeight,
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
            _ignoreNextClipboardChanges = 1;
            System.Windows.Clipboard.SetText(content);
        }

        private async Task<bool> CopyClipToClipboardAsync(ClipItem clip)
        {
            if (clip.ClipType == ClipType.Image)
            {
                if (clip.BinaryContent is null || clip.BinaryContent.Length == 0)
                {
                    await SendErrorAsync("Image data not found.");
                    return false;
                }

                CopyImageToClipboard(clip.BinaryContent, clip.ContentHash);
                return true;
            }

            CopyTextToClipboard(clip.Content);
            return true;
        }

        private void CopyImageToClipboard(byte[] pngBytes, string contentHash)
        {
            _ignoreNextClipboardHash = contentHash;
            _ignoreNextClipboardChanges = 1;
            System.Windows.Clipboard.SetImage(DecodeBitmapFromPng(pngBytes));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmNcHitTest)
            {
                var hitTest = HitTestResizeBorder(lParam);
                if (hitTest is not null)
                {
                    handled = true;
                    return new IntPtr(hitTest.Value);
                }
            }
            else if (msg == WmClipboardUpdate)
            {
                _ = Dispatcher.InvokeAsync(CaptureClipboardAsync);
                handled = true;
            }
            else if (msg == WmHotKey && wParam.ToInt32() == HotKeyId)
            {
                ToggleWindowFromHotKey();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private int? HitTestResizeBorder(IntPtr lParam)
        {
            if (WindowState == WindowState.Maximized || ResizeMode == ResizeMode.NoResize)
            {
                return null;
            }

            var x = unchecked((short)((long)lParam & 0xFFFF));
            var y = unchecked((short)(((long)lParam >> 16) & 0xFFFF));
            var point = PointFromScreen(new System.Windows.Point(x, y));

            var isLeft = point.X <= ResizeGripThickness;
            var isRight = point.X >= ActualWidth - ResizeGripThickness;
            var isTop = point.Y <= ResizeGripThickness;
            var isBottom = point.Y >= ActualHeight - ResizeGripThickness;

            if (isTop && isLeft)
            {
                return HtTopLeft;
            }

            if (isTop && isRight)
            {
                return HtTopRight;
            }

            if (isBottom && isLeft)
            {
                return HtBottomLeft;
            }

            if (isBottom && isRight)
            {
                return HtBottomRight;
            }

            if (isLeft)
            {
                return HtLeft;
            }

            if (isRight)
            {
                return HtRight;
            }

            if (isTop)
            {
                return HtTop;
            }

            if (isBottom)
            {
                return HtBottom;
            }

            return null;
        }

        private async void ToggleWindowFromHotKey()
        {
            if (IsVisible && IsActive)
            {
                Hide();
                return;
            }

            RememberReturnWindow();
            ShowWindowForSearch();
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

        private async Task FocusSearchAsync()
        {
            FocusWebViewForKeyboardInput();
            await Dispatcher.InvokeAsync(FocusWebViewForKeyboardInput, DispatcherPriority.Input);

            await SendMessageToUiAsync(new
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

        private Task SendAppInfoAsync()
        {
            return SendMessageToUiAsync(new
            {
                type = "app:info",
                payload = CreateAppInfoPayload()
            });
        }

        private async Task SendSettingsUpdatedAsync()
        {
            await SendMessageToUiAsync(new
            {
                type = "settings:updated",
                payload = CreateSettingsPayload()
            });
            await SendAppInfoAsync();
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

        private object CreateAppInfoPayload()
        {
            return new
            {
                name = "TanoDev Clip",
                version = "0.1.0",
                environment = "Development",
                hotkey = _settings.HotKey,
                settings = CreateSettingsPayload()
            };
        }

        private object CreateSettingsPayload()
        {
            return new
            {
                hotKey = _settings.HotKey,
                enabledTools = _settings.EnabledTools,
                defaults = new
                {
                    hotKey = AppSettingsDefaults.HotKey,
                    enabledTools = AppSettingsDefaults.EnabledTools
                }
            };
        }

        private static AppSettings NormalizeSettings(AppSettings? settings)
        {
            var normalized = settings?.Clone() ?? AppSettingsDefaults.Create();

            if (string.IsNullOrWhiteSpace(normalized.HotKey))
            {
                normalized.HotKey = AppSettingsDefaults.HotKey;
            }

            normalized.HotKey = NormalizeHotKeyText(normalized.HotKey) ?? AppSettingsDefaults.HotKey;

            var knownTools = AppSettingsDefaults.EnabledTools.ToHashSet(StringComparer.OrdinalIgnoreCase);
            normalized.EnabledTools = (normalized.EnabledTools ?? [])
                .Where(tool => knownTools.Contains(tool))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return normalized;
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

        private static bool? ReadPayloadBool(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty("payload", out var payload) ||
                !payload.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }

        private static object ToPayload(ClipItem clip)
        {
            return new
            {
                id = clip.Id,
                content = clip.Content,
                contentHash = clip.ContentHash,
                clipType = clip.ClipType.ToString(),
                contentMimeType = clip.ContentMimeType,
                imageWidth = clip.ImageWidth,
                imageHeight = clip.ImageHeight,
                imagePreviewDataUrl = CreateImagePreviewDataUrl(clip),
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

        private static string? CreateImagePreviewDataUrl(ClipItem clip)
        {
            if (clip.PreviewContent is null || clip.PreviewContent.Length == 0)
            {
                return null;
            }

            var mimeType = string.IsNullOrWhiteSpace(clip.ContentMimeType)
                ? "image/png"
                : clip.ContentMimeType;

            return $"data:{mimeType};base64,{Convert.ToBase64String(clip.PreviewContent)}";
        }

        private static string ComputeSha256(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string ComputeSha256(byte[] content)
        {
            var bytes = SHA256.HashData(content);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static byte[] EncodeBitmapToPng(BitmapSource source)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        private static BitmapSource DecodeBitmapFromPng(byte[] pngBytes)
        {
            using var stream = new MemoryStream(pngBytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static BitmapSource CreatePreviewBitmap(BitmapSource source)
        {
            var largestSide = Math.Max(source.PixelWidth, source.PixelHeight);
            if (largestSide <= ImagePreviewMaxPixelSize)
            {
                return source;
            }

            var scale = ImagePreviewMaxPixelSize / (double)largestSide;
            var preview = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            preview.Freeze();
            return preview;
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
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();
                return;
            }

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

        private void InitializeTrayIcon()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => ShowFromTray());
            menu.Items.Add("Exit", null, (_, _) => ExitFromTray());

            var iconResource = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.Absolute)
            ) ?? throw new InvalidOperationException("Tray icon resource not found.");

            _trayIcon = new Forms.NotifyIcon
            {
                Icon = new Drawing.Icon(iconResource.Stream),
                Text = "TanoDev Clip",
                ContextMenuStrip = menu,
                Visible = true
            };

            _trayIcon.DoubleClick += (_, _) => ShowFromTray();
        }

        public void DisposeTrayIcon()
        {
            if (_trayIcon is null)
            {
                return;
            }

            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        private void ShowFromTray()
        {
            RememberReturnWindow();
            ShowWindowForSearch();
            _ = FocusSearchAsync();
        }

        private void ShowWindowForSearch()
        {
            if (_isPreloadingInBackground)
            {
                CompleteStartupPreload();
            }

            if (_centerOnNextVisibleShow)
            {
                CenterOnPrimaryWorkArea();
                _centerOnNextVisibleShow = false;
            }

            ShowActivated = true;
            ShowInTaskbar = true;
            Opacity = 1;
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();

            var handle = new WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero)
            {
                SetForegroundWindow(handle);
            }
        }

        private void CompleteStartupPreload()
        {
            if (!_isPreloadingInBackground)
            {
                return;
            }

            Hide();
            Opacity = 1;
            ShowActivated = true;
            ShowInTaskbar = true;
            _isPreloadingInBackground = false;
        }

        private void CenterOnPrimaryWorkArea()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
            Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
        }

        private void FocusWebViewForKeyboardInput()
        {
            if (!IsVisible)
            {
                return;
            }

            Activate();
            Focus();
            AppWebView.Focus();
            Keyboard.Focus(AppWebView);
        }

        private async Task PasteIntoReturnWindowAsync()
        {
            var target = _returnWindowHandle;
            if (target == IntPtr.Zero || !IsWindow(target) || IsCurrentAppWindow(target))
            {
                return;
            }

            if (IsIconic(target))
            {
                ShowWindow(target, SwRestore);
            }

            SetForegroundWindow(target);

            await Task.Delay(120);
            Forms.SendKeys.SendWait("^v");
        }

        private void RememberReturnWindow()
        {
            var target = GetForegroundWindow();
            if (target == IntPtr.Zero || IsCurrentAppWindow(target))
            {
                return;
            }

            _returnWindowHandle = target;
        }

        private static bool IsCurrentAppWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            _ = GetWindowThreadProcessId(hwnd, out var processId);
            return processId == Environment.ProcessId;
        }

        private void ExitFromTray()
        {
            _isExiting = true;
            DisposeTrayIcon();
            System.Windows.Application.Current.Shutdown();
        }

        private void DragBorderlessWindow()
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(handle, WmNcLButtonDown, HtCaption, 0);
        }

        private bool TryRegisterConfiguredHotKey(string hotKey)
        {
            if (_hwndSource is null || !TryParseHotKey(hotKey, out var parsed))
            {
                return false;
            }

            return RegisterHotKey(_hwndSource.Handle, HotKeyId, parsed.Modifiers, parsed.VirtualKey);
        }

        private bool TryApplyConfiguredHotKey(string hotKey)
        {
            if (_hwndSource is null)
            {
                return true;
            }

            if (!TryParseHotKey(hotKey, out var parsed))
            {
                return false;
            }

            if (_hotKeyRegistered)
            {
                UnregisterHotKey(_hwndSource.Handle, HotKeyId);
                _hotKeyRegistered = false;
            }

            _hotKeyRegistered = RegisterHotKey(_hwndSource.Handle, HotKeyId, parsed.Modifiers, parsed.VirtualKey);
            return _hotKeyRegistered;
        }

        private static bool TryParseHotKey(string hotKey, out ParsedHotKey parsed)
        {
            parsed = default;
            var modifiers = 0u;
            uint? virtualKey = null;

            foreach (var part in hotKey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= ModControl;
                    continue;
                }

                if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= ModAlt;
                    continue;
                }

                if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= ModShift;
                    continue;
                }

                virtualKey = ParseVirtualKey(part);
            }

            if (modifiers == 0 || virtualKey is null)
            {
                return false;
            }

            parsed = new ParsedHotKey(modifiers, virtualKey.Value);
            return true;
        }

        private static string? NormalizeHotKeyText(string hotKey)
        {
            if (!TryParseHotKey(hotKey, out var parsed))
            {
                return null;
            }

            var parts = new List<string>();
            if ((parsed.Modifiers & ModControl) != 0)
            {
                parts.Add("Ctrl");
            }

            if ((parsed.Modifiers & ModAlt) != 0)
            {
                parts.Add("Alt");
            }

            if ((parsed.Modifiers & ModShift) != 0)
            {
                parts.Add("Shift");
            }

            parts.Add(FormatVirtualKey(parsed.VirtualKey));
            return string.Join("+", parts);
        }

        private static uint? ParseVirtualKey(string key)
        {
            if (key.Equals("Space", StringComparison.OrdinalIgnoreCase))
            {
                return VkSpace;
            }

            if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
            {
                return char.ToUpperInvariant(key[0]);
            }

            if (key.Length is 2 or 3 &&
                key[0] is 'F' or 'f' &&
                int.TryParse(key[1..], out var functionKey) &&
                functionKey is >= 1 and <= 12)
            {
                return (uint)(0x70 + functionKey - 1);
            }

            return null;
        }

        private static string FormatVirtualKey(uint virtualKey)
        {
            if (virtualKey == VkSpace)
            {
                return "Space";
            }

            if (virtualKey is >= 0x30 and <= 0x5A)
            {
                return ((char)virtualKey).ToString();
            }

            if (virtualKey is >= 0x70 and <= 0x7B)
            {
                return $"F{virtualKey - 0x70 + 1}";
            }

            return "Space";
        }

        private sealed record ForegroundSource(string? ProcessName, string? WindowTitle);

        private readonly record struct ParsedHotKey(uint Modifiers, uint VirtualKey);

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

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    }
}

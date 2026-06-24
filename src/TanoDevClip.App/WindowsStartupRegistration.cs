using System.IO;
using Microsoft.Win32;
using Velopack.Locators;

namespace TanoDevClip.App
{
    internal static class WindowsStartupRegistration
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "TanoDev Clip";
        private const string StartupArgument = "--startup";
        private const string MainExecutableName = "TanoDevClip.App.exe";

        public static bool IsStartupLaunch(string[] args)
        {
            return args.Any(arg => string.Equals(
                arg,
                StartupArgument,
                StringComparison.OrdinalIgnoreCase));
        }

        public static void EnableForCurrentUser()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var executablePath = ResolveInstalledExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return;
            }

            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            key?.SetValue(
                ValueName,
                $"{Quote(executablePath)} {StartupArgument}",
                RegistryValueKind.String);
        }

        public static void DisableForCurrentUser()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        private static string? ResolveInstalledExecutablePath()
        {
            try
            {
                if (!VelopackLocator.IsCurrentSet)
                {
                    return null;
                }

                var locator = VelopackLocator.Current;
                if (locator.CurrentlyInstalledVersion is null)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(locator.RootAppDir))
                {
                    return null;
                }

                var rootExecutablePath = Path.Combine(
                    locator.RootAppDir,
                    MainExecutableName);

                if (File.Exists(rootExecutablePath))
                {
                    return rootExecutablePath;
                }

                if (string.IsNullOrWhiteSpace(locator.AppContentDir)
                    || string.IsNullOrWhiteSpace(locator.ThisExeRelativePath))
                {
                    return null;
                }

                var currentExecutablePath = Path.Combine(
                    locator.AppContentDir,
                    locator.ThisExeRelativePath);

                return File.Exists(currentExecutablePath)
                    ? currentExecutablePath
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string Quote(string value)
        {
            return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }
    }
}

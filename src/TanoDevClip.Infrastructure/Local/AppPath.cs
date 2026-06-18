namespace TanoDevClip.Infrastructure.Local
{
    public static class AppPaths
    {
        public static string GetAppDataDirectory()
        {
            var localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            return Path.Combine(localAppData, "TanoDevClip");
        }

        public static string GetDatabasePath()
        {
            return Path.Combine(GetAppDataDirectory(), "tanodevclip.db");
        }

        public static string GetSettingsPath()
        {
            return Path.Combine(GetAppDataDirectory(), "settings.json");
        }
    }
}

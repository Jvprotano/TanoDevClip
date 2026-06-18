using System.IO;
using System.Windows;
using TanoDevClip.Core.Classification;
using TanoDevClip.Core.Settings;
using TanoDevClip.DevTools;
using TanoDevClip.Infrastructure.Database;
using TanoDevClip.Infrastructure.Local;

namespace TanoDevClip.App
{
    public partial class App : System.Windows.Application
    {
        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            if (MainWindow is MainWindow mainWindow)
            {
                mainWindow.DisposeTrayIcon();
            }

            base.OnExit(e);
        }

        protected override async void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            Directory.CreateDirectory(AppPaths.GetAppDataDirectory());

            var connectionFactory = new DatabaseConnectionFactory(AppPaths.GetDatabasePath());
            var bootstrapper = new DatabaseBootstrapper(connectionFactory);
            await bootstrapper.InitializeAsync();

            var repository = new SqliteClipRepository(connectionFactory);
            var classifier = new DefaultClipboardClassifier();
            var guidGenerator = new GuidGenerator();
            var devToolRunner = new DevToolRunner(guidGenerator);
            var settingsStore = new JsonAppSettingsStore(AppPaths.GetSettingsPath());
            var settings = await settingsStore.LoadAsync();

            MainWindow = new MainWindow(
                repository,
                classifier,
                guidGenerator,
                devToolRunner,
                settingsStore,
                settings);
            MainWindow.Show();
        }
    }
}

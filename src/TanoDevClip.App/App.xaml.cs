using System.IO;
using TanoDevClip.Core.Classification;
using TanoDevClip.DevTools;
using TanoDevClip.Infrastructure.Database;
using TanoDevClip.Infrastructure.Local;
using Velopack;

namespace TanoDevClip.App
{
    public partial class App : System.Windows.Application
    {
        [STAThread]
        private static void Main(string[] args)
        {
            VelopackApp.Build()
                .OnAfterInstallFastCallback(_ => WindowsStartupRegistration.EnableForCurrentUser())
                .OnAfterUpdateFastCallback(_ => WindowsStartupRegistration.EnableForCurrentUser())
                .OnBeforeUninstallFastCallback(_ => WindowsStartupRegistration.DisableForCurrentUser())
                .Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

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
            var startHidden = WindowsStartupRegistration.IsStartupLaunch(e.Args);

            WindowsStartupRegistration.EnableForCurrentUser();

            MainWindow = new MainWindow(
                repository,
                classifier,
                guidGenerator,
                devToolRunner,
                settingsStore,
                settings);
            MainWindow.Show();

            if (startHidden)
            {
                MainWindow.Hide();
            }
        }
    }
}

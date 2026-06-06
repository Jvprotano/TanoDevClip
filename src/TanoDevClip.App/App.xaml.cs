using System.IO;
using System.Windows;
using TanoDevClip.Core.Classification;
using TanoDevClip.DevTools;
using TanoDevClip.Infrastructure.Database;
using TanoDevClip.Infrastructure.Local;

namespace TanoDevClip.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(AppPaths.GetAppDataDirectory());

        var connectionFactory = new DatabaseConnectionFactory(AppPaths.GetDatabasePath());
        var bootstrapper = new DatabaseBootstrapper(connectionFactory);
        await bootstrapper.InitializeAsync();

        var repository = new SqliteClipRepository(connectionFactory);
        var classifier = new DefaultClipboardClassifier();
        var guidGenerator = new GuidGenerator();

        MainWindow = new MainWindow(repository, classifier, guidGenerator);
        MainWindow.Show();
    }
}

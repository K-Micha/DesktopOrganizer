using DesktopOrganizer.Views;
using System.Threading;
using System.Windows;

namespace DesktopOrganizer
{
    public partial class App : Application
    {
        private const string MutexName =
            "DesktopOrganizer_SingleInstance";

        private Mutex? appMutex;
        private bool ownsMutex;

        private WallpaperWindow? wallpaperWindow;
        private TaskbarOverlay? taskbarOverlay;

        protected override void OnStartup(
            StartupEventArgs e)
        {
            appMutex = new Mutex(
                true,
                MutexName,
                out bool createdNew
            );

            ownsMutex = createdNew;

            if (!createdNew)
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            wallpaperWindow =
                new WallpaperWindow();

            wallpaperWindow.Show();

            taskbarOverlay =
                new TaskbarOverlay();

            taskbarOverlay.Show();
        }

        protected override void OnExit(
            ExitEventArgs e)
        {
            taskbarOverlay?.Close();
            taskbarOverlay = null;

            wallpaperWindow?.Close();
            wallpaperWindow = null;

            if (ownsMutex)
            {
                appMutex?.ReleaseMutex();
            }

            appMutex?.Dispose();

            base.OnExit(e);
        }
    }
}
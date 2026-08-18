using System.Windows;
using System.Windows.Input;

namespace DesktopOrganizer
{
    public partial class DesktopOverlay : Window
    {
        public DesktopOverlay()
        {
            InitializeComponent();
        }

        // ESC schließt Overlay
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        // Klick außerhalb der Ordner schließt Overlay
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Wenn außerhalb FolderArea geklickt → schließen
            if (!IsMouseOverFolderArea(e))
            {
                Close();
            }
        }

        private bool IsMouseOverFolderArea(MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(FolderArea);
            return pos.X >= 0 && pos.X <= FolderArea.ActualWidth &&
                   pos.Y >= 0 && pos.Y <= FolderArea.ActualHeight;
        }

        // Klick IN den Ordnerbereich → NICHT schließen
        private void FolderArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}

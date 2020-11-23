using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    public partial class FullscreenReminder : Window
    {

        public FullscreenReminder()
        {
            InitializeComponent();
            Show();
            Focus();
        }
        
        private void DisableOverlayClick(object sender, RoutedEventArgs e)
        {
            Main.AddLog($"[Fullscreen Reminder] User selected \"Disable overlay mode\" - showing Setting window");
            Main.settingsWindow.Show();
            Main.settingsWindow.populate();
            Main.settingsWindow.Left = Left;
            Main.settingsWindow.Top = Top + Height;
            Main.settingsWindow.Show();
            Close();

        }
        private void NoClick(object sender, RoutedEventArgs e)
        {
            Main.AddLog($"[Fullscreen Reminder] User selected \"Do nothing\"");
            Close();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}

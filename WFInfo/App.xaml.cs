using System.Windows;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
        }

        private void Application_Exit(object sender, ExitEventArgs e) { //Make a new tray icon and remove it, updating the old one. Can't acces the other classes here.
            MainWindow mainwin = new MainWindow();
            mainwin.removeTrayIcon();
            mainwin.Close();
        }
    }
}

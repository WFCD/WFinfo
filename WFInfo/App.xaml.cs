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

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (WFInfo.MainWindow.INSTANCE != null)
                WFInfo.MainWindow.INSTANCE.Exit(null, null);
        }
    }
}
using System.Windows;
using AutoUpdaterDotNET;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AutoUpdater.Start("https://github.com/WFCD/WFinfo/releases/latest/download/update.xml");
        }
    }
}

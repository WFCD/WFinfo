using System;
using System.Windows;
using AutoUpdaterDotNET;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Console.WriteLine("Doing the update?");
            AutoUpdater.ReportErrors = true;
            AutoUpdater.Start("https://github.com/WFCD/WFinfo/releases/latest/download/update.xml");
        }
    }
}

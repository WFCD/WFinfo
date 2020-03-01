using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
            AutoUpdater.Start("https://raw.githubusercontent.com/WFCD/WFinfo/master/docs/update.xml");
        }
    }
}

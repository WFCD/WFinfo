using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
            AutoUpdater.Start("https://raw.githubusercontent.com/WFCD/WFinfo/c-sharp/docs/update.xml");
        }
    }
}

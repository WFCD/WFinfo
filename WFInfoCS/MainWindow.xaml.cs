using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window{
        Main main = new Main(); //subscriber

        public MainWindow(){
        LowLevelListener listener = new LowLevelListener(); //publisher
            main.updatedStatus += this.ChangeStatus;
            try
            {

                String thisprocessname = Process.GetCurrentProcess().ProcessName;
                if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1){
                    main.AddLog("Duplicate process found");
                    this.Close();
                }

                LowLevelListener.KeyAction += main.OnKeyAction; 
                listener.Hook();
                InitializeComponent();
                Version.Content = main.BuildVersion;
                ChangeStatus("loaded", 0);
                main.AddLog("Sucsesfully launched");
            }
            catch (Exception e){
                main.AddLog("An error occured at MainWindow()" + e.Message);
            }
            }

        public void ChangeStatus(string status, int serverity){
            Status.Content = "Status: " + status;
            switch (serverity)
            {
                case 0: //default, no problem
                    Status.Foreground = main.LightBlue;
                    break;
                case 1: //severe, red text
                    Status.Foreground = Brushes.Red;
                    break;
                case 2: //warning, orange text
                    Status.Foreground = Brushes.Orange;
                    break;
                default: //Uncaught, big problem
                    Status.Foreground = Brushes.Yellow;
                    break;
            }
        }

        private void Exit(object sender, RoutedEventArgs e){
            App.Current.Shutdown();
        }

        private void Minimise(object sender, RoutedEventArgs e){
            this.WindowState = WindowState.Minimized;
        }

        private void Website_click(object sender, RoutedEventArgs e)
        {
            ChangeStatus("Go go website", 0);
            Process.Start("https://wfinfo.warframestat.us/");
        }

        private void Relics_click(object sender, RoutedEventArgs e){
            //todo, open new window, showing all relics
            ChangeStatus("Relics not implemented", 2);
        }

        private void Gear_click(object sender, RoutedEventArgs e){
            //todo, opens new window, shows all prime items
            ChangeStatus("This should work, big oopsie, scaling teeeeeeeeeeeeeeeeeeeest", 1);

        }
        private void Settings_click(object sender, RoutedEventArgs e){
            Settings settingsWindow = new Settings();
            settingsWindow.Show();
            //ChangeStatus("Something uncaught", -1);
        }

        private void ReloadWikiClick(object sender, RoutedEventArgs e){
            //todo reloads wiki data
        }

        private void ReloadDropClick(object sender, RoutedEventArgs e){
            //todo reloads de's data
        }

        private void ReloadMarketClick(object sender, RoutedEventArgs e){
            //todo reloads warframe.market data
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e){
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
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
        private string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS";
        private Brush LightBlue = new SolidColorBrush(Color.FromRgb(177, 208, 217));

        public string BuildVersion = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();


        public MainWindow(){
            try {
                String thisprocessname = Process.GetCurrentProcess().ProcessName;
                if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1){
                    AddLog("Duplicate process found");
                    this.Close();
                }

                InitializeComponent();
                Version.Text = BuildVersion;

                ChangeStatus("loaded", 0); 
            }
            catch (Exception e){
                AddLog("An error occured at MainWindow()" + e.Message);
            }
            }

        public void AddLog(string argm){ //write to the debug file, includes version and UTCtime
            string path = appPath + @"\Debug";
            Console.WriteLine(argm);
            Directory.CreateDirectory(path);
            using (StreamWriter sw = File.AppendText(path + @"\debug.txt")){
                sw.WriteLineAsync("[" + DateTime.UtcNow + " " + BuildVersion + "] \t" + argm);
            } 
        }

        public void ChangeStatus(string status, int serverity){
            Status.Text = "Status: " + status;
            switch (serverity)
            {
                case 0://default, no problem
                    Status.Foreground = LightBlue;
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
            this.Close();
        }

        private void Minimise(object sender, RoutedEventArgs e){
            this.WindowState = WindowState.Minimized;
        }

        private void Website_click(object sender, RoutedEventArgs e)
        {
            //todo, link to our webiste
        }

        private void Relics_click(object sender, RoutedEventArgs e){
            //todo, open new window, showing all relics
        }

        private void Gear_click(object sender, RoutedEventArgs e){
            //todo, opens new window, shows all prime items
        }
        private void Settings_click(object sender, RoutedEventArgs e){
            //todo, opens new window, shows all settings
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
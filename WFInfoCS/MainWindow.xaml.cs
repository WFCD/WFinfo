using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        readonly Main main; //subscriber
        public static MainWindow INSTANCE;

        public MainWindow()
        {
            INSTANCE = this;
            main = new Main();
            LowLevelListener listener = new LowLevelListener(); //publisher
            try
            {
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\settings.json"))
                {
                    Settings.settingsObj = JObject.Parse(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\settings.json"));
                }
                else
                {
                    Settings.settingsObj = JObject.Parse("{\"Display\":\"Overlay\"," +
                        "\"ActivationKey\":\"Snapshot\"," +
                        "\"Scaling\":100.0," +
                        "\"Auto\":false," +
                        "\"Debug\":false}");
                    Settings.Save();
                }
                Settings.activationKey = (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("ActivationKey").ToString());
                Settings.debug = (bool)Settings.settingsObj.GetValue("Debug");
                Settings.auto = (bool)Settings.settingsObj.GetValue("Auto");
                Settings.scaling = Convert.ToInt32(Settings.settingsObj.GetValue("Scaling"));
                Settings.isOverlaySelected = Settings.settingsObj.GetValue("Display").ToString() == "Overlay";

                string thisprocessname = Process.GetCurrentProcess().ProcessName;
                if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
                {
                    Main.AddLog("Duplicate process found");
                    Close();
                }

                LowLevelListener.KeyAction += main.OnKeyAction;
                listener.Hook();
                InitializeComponent();
                Version.Content = "v" + Main.BuildVersion + "-beta";
            }
            catch (Exception e)
            {
                Main.AddLog("An error occured while loading the main window: " + e.Message);
            }
        }

        public void ChangeStatus(string status, int serverity)
        {
            Console.WriteLine(status);
            Status.Content = "Status: " + status;
            switch (serverity)
            {
                case 0: //default, no problem
                    Status.Foreground = new SolidColorBrush(Color.FromRgb(177, 208, 217));
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

        private void Exit(object sender, RoutedEventArgs e)
        {
            Main.relicWindow.Close();
            Main.equipmentWindow.Close();
            Main.settingsWindow.Close();
            Application.Current.Shutdown();
        }

        private void Minimise(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void websiteClick(object sender, RoutedEventArgs e)
        {
            //ChangeStatus("Go go website", 0);
            Process.Start("https://wfinfo.warframestat.us/");
        }

        private void relicsClick(object sender, RoutedEventArgs e)
        {
            //Main.relicWindow.populate();
            ChangeStatus("Relics not implemented", 2);
        }

        private void equipmentClick(object sender, RoutedEventArgs e)
        {
            //Main.equipmentWindow.populate();
            ChangeStatus("Equipment not implemented", 2);
        }

        private void Settings_click(object sender, RoutedEventArgs e)
        {
            Main.settingsWindow.populate();
            Main.settingsWindow.Show();
        }

        private void ReloadMarketClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadWiki.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            Market_Data.Content = "Market Data: Loading...";
            Task.Factory.StartNew(Main.dataBase.ForceMarketUpdate);
        }

        private void ReloadDropClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadWiki.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            Drop_Data.Content = "Drop Data: Loading...";
            Wiki_Data.Content = "Wiki Data: Loading...";
            Task.Factory.StartNew(Main.dataBase.ForceEquipmentUpdate);
        }

        private void ReloadWikiClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadWiki.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            Wiki_Data.Content = "Wiki Data: Loading...";
            Task.Factory.StartNew(Main.dataBase.ForceWikiUpdate);
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
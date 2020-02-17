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
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json"))
                {
                    Settings.settingsObj = JObject.Parse(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json"));

                }
                else
                {
                    Settings.settingsObj = new JObject();
                }
                if (!Settings.settingsObj.TryGetValue("Display", out _))
                    Settings.settingsObj["Display"] = "Overlay";
                Settings.isOverlaySelected = Settings.settingsObj.GetValue("Display").ToString() == "Overlay";

                if (!Settings.settingsObj.TryGetValue("MainWindowLocation_X", out _))
                    Settings.settingsObj["MainWindowLocation_X"] = 300;
                if (!Settings.settingsObj.TryGetValue("MainWindowLocation_Y", out _))
                    Settings.settingsObj["MainWindowLocation_Y"] = 300;
                Settings.mainWindowLocation = new Point(Settings.settingsObj.GetValue("MainWindowLocation_X").ToObject<Int32>(), Settings.settingsObj.GetValue("MainWindowLocation_Y").ToObject<Int32>());

                if (!Settings.settingsObj.TryGetValue("ActivationKey", out _))
                    Settings.settingsObj["ActivationKey"] = "Snapshot";
                Settings.activationKey = (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("ActivationKey").ToString());

                if (!Settings.settingsObj.TryGetValue("Debug", out _))
                    Settings.settingsObj["Debug"] = false;
                Settings.debug = (bool)Settings.settingsObj.GetValue("Debug");

                if (!Settings.settingsObj.TryGetValue("Auto", out _))
                    Settings.settingsObj["Auto"] = false;
                Settings.auto = (bool)Settings.settingsObj.GetValue("Auto");

                if (!Settings.settingsObj.TryGetValue("AutoDelay", out _))
                    Settings.settingsObj["AutoDelay"] = 250L;
                Settings.autoDelay = (long)Settings.settingsObj.GetValue("AutoDelay");

                if (!Settings.settingsObj.TryGetValue("Scaling", out _))
                    Settings.settingsObj["Scaling"] = 100.0;
                Settings.scaling = Convert.ToInt32(Settings.settingsObj.GetValue("Scaling"));

                if (!Settings.settingsObj.TryGetValue("ImageRetentionTime", out _))
                    Settings.settingsObj["ImageRetentionTime"] = 12;
                Settings.imageRetentionTime = Convert.ToInt32(Settings.settingsObj.GetValue("ImageRetentionTime"));
                Settings.Save();

                string thisprocessname = Process.GetCurrentProcess().ProcessName;
                if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
                {
                    Main.AddLog("Duplicate process found");
                    Close();
                }

                LowLevelListener.KeyAction += main.OnKeyAction;
                listener.Hook();
                InitializeComponent();
                Version.Content = "v" + Main.BuildVersion + "-beta5";

                this.Left = 300;
                this.Top = 300;

                System.Drawing.Rectangle winBounds = new System.Drawing.Rectangle(Convert.ToInt32(Settings.mainWindowLocation.X), Convert.ToInt32(Settings.mainWindowLocation.Y), Convert.ToInt32(Width), Convert.ToInt32(Height));
                foreach (System.Windows.Forms.Screen scr in System.Windows.Forms.Screen.AllScreens)
                {
                    if (scr.Bounds.Contains(winBounds))
                    {
                        this.Left = Settings.mainWindowLocation.X;
                        this.Top = Settings.mainWindowLocation.Y;
                        break;
                    }
                }
                Settings.settingsObj["MainWindowLocation_X"] = Left;
                Settings.settingsObj["MainWindowLocation_Y"] = Top;
                Settings.Save();
            }
            catch (Exception e)
            {
                Main.AddLog("An error occured while loading the main window: " + e.Message);
            }
        }

        public void ChangeStatus(string status, int serverity)
        {
            Console.WriteLine("Status message: " + status);
            Status.Content = status;
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
            if (Main.dataBase.relicData == null) { ChangeStatus("Relic data not yet loaded in", 2); return; }
            Main.relicWindow.Show();
            Main.relicWindow.Focus();
        }

        private void equipmentClick(object sender, RoutedEventArgs e)
        {
            if (Main.dataBase.equipmentData == null) { ChangeStatus("Equipment data not yet loaded in", 2); return; }
            Main.equipmentWindow.Show();
        }

        private void Settings_click(object sender, RoutedEventArgs e)
        {
            if (Main.settingsWindow == null) { ChangeStatus("Settings window not yet loaded in", 2); return; }
            Main.settingsWindow.populate();
            Main.settingsWindow.Left = Left + 320;
            Main.settingsWindow.Top = Top;
            Main.settingsWindow.Show();
        }

        private void ReloadMarketClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            Market_Data.Content = "Loading...";
            Main.StatusUpdate("Market data force reloading", 0);
            Task.Factory.StartNew(Main.dataBase.ForceMarketUpdate);
        }

        private void ReloadDropClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            Drop_Data.Content = "Loading...";
            Main.StatusUpdate("Drop data force reloading", 0);
            Task.Factory.StartNew(Main.dataBase.ForceEquipmentUpdate);
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            Settings.mainWindowLocation = new Point(Left, Top);
            Settings.settingsObj["MainWindowLocation_X"] = Left;
            Settings.settingsObj["MainWindowLocation_Y"] = Top;
            Settings.Save();
        }
    }
}
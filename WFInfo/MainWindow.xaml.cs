using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Main main; //subscriber
        public static MainWindow INSTANCE;
        public static WelcomeDialogue welcomeDialogue;
        public static LowLevelListener listener;
        private static bool updatesupression;
        private RelicsWindow _relicsWindow = new RelicsWindow();

        public MainWindow()
        {

            string thisprocessname = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
            {
                Main.AddLog("Duplicate process found");
                Close();
            }


            INSTANCE = this;
            main = new Main();
            listener = new LowLevelListener(); //publisher
            try
            {
                InitializeSettings();

                LowLevelListener.KeyEvent += main.OnKeyAction;
                LowLevelListener.MouseEvent += main.OnMouseAction;
                listener.Hook();
                InitializeComponent();
                Version.Content = "v" + Main.BuildVersion;

                Left = 300;
                Top = 300;

                System.Drawing.Rectangle winBounds = new System.Drawing.Rectangle(Convert.ToInt32(Settings.mainWindowLocation.X), Convert.ToInt32(Settings.mainWindowLocation.Y), Convert.ToInt32(Width), Convert.ToInt32(Height));
                foreach (System.Windows.Forms.Screen scr in System.Windows.Forms.Screen.AllScreens)
                {
                    if (scr.Bounds.Contains(winBounds))
                    {
                        Left = Settings.mainWindowLocation.X;
                        Top = Settings.mainWindowLocation.Y;
                        break;
                    }
                }
                Settings.settingsObj["MainWindowLocation_X"] = Left;
                Settings.settingsObj["MainWindowLocation_Y"] = Top;

                Settings.Save();

                Closing += new CancelEventHandler(LoggOut);
            }
            catch (Exception e)
            {
                Main.AddLog("An error occured while loading the main window: " + e.Message);
            }
        }



        public void InitializeSettings()
        {
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json") && Settings.settingsObj == null)
            {
                Settings.settingsObj = JObject.Parse(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json"));

            }
            else
            {
                if (Settings.settingsObj == null)
                    Settings.settingsObj = new JObject();
                welcomeDialogue = new WelcomeDialogue();
            }

            if (!Settings.settingsObj.TryGetValue("Display", out _))
                Settings.settingsObj["Display"] = "Overlay";
            Settings.isOverlaySelected = Settings.settingsObj.GetValue("Display").ToString() == "Overlay";
            Settings.isLightSelected = Settings.settingsObj.GetValue("Display").ToString() == "Light";

            if (!Settings.settingsObj.TryGetValue("MainWindowLocation_X", out _))
                Settings.settingsObj["MainWindowLocation_X"] = 300;
            if (!Settings.settingsObj.TryGetValue("MainWindowLocation_Y", out _))
                Settings.settingsObj["MainWindowLocation_Y"] = 300;
            Settings.mainWindowLocation =
                new Point(Settings.settingsObj.GetValue("MainWindowLocation_X").ToObject<Int32>(),
                    Settings.settingsObj.GetValue("MainWindowLocation_Y").ToObject<Int32>());

            if (!Settings.settingsObj.TryGetValue("ActivationKey", out _))
                Settings.settingsObj["ActivationKey"] = "Snapshot";
            try
            {
                Settings.ActivationKey =
                    (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("ActivationKey").ToString());
            }
            catch
            {
                try
                {
                    Settings.ActivationMouseButton = (MouseButton)Enum.Parse(typeof(MouseButton),
                        Settings.settingsObj.GetValue("ActivationKey").ToString());
                }
                catch
                {
                    Main.AddLog("Couldn't Parse Activation Key -- Defaulting to PrintScreen");
                    Settings.settingsObj["ActivationKey"] = "Snapshot";
                    Settings.ActivationKey = Key.Snapshot;
                }
            }

            if (!Settings.settingsObj.TryGetValue("DebugModifierKey", out _))
                Settings.settingsObj["DebugModifierKey"] = Key.LeftShift.ToString();
            Settings.DebugModifierKey =
                (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("DebugModifierKey").ToString());

            if (!Settings.settingsObj.TryGetValue("SearchItModifierKey", out _))
                Settings.settingsObj["SearchItModifierKey"] = Key.OemTilde.ToString();
            Settings.SearchItModifierKey = (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("SearchItModifierKey").ToString());

            if (!Settings.settingsObj.TryGetValue("SnapitModifierKey", out _))
                Settings.settingsObj["SnapitModifierKey"] = Key.LeftCtrl.ToString();
            Settings.SnapitModifierKey = (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("SnapitModifierKey").ToString());

            if (!Settings.settingsObj.TryGetValue("MasterItModifierKey", out _))
                Settings.settingsObj["MasterItModifierKey"] = Key.RightCtrl.ToString();
            Settings.MasterItModifierKey = (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("MasterItModifierKey").ToString());

            if (!Settings.settingsObj.TryGetValue("Debug", out _))
                Settings.settingsObj["Debug"] = false;
            Settings.debug = (bool)Settings.settingsObj.GetValue("Debug");

            if (!Settings.settingsObj.TryGetValue("Locale", out _))
                Settings.settingsObj["Locale"] = "en";
            Settings.locale = Settings.settingsObj.GetValue("Locale").ToString();

            if (!Settings.settingsObj.TryGetValue("Clipboard", out _))
                Settings.settingsObj["Clipboard"] = false;
            Settings.clipboard = (bool)Settings.settingsObj.GetValue("Clipboard");

            if (!Settings.settingsObj.TryGetValue("AutoDelay", out _))
                Settings.settingsObj["AutoDelay"] = 250L;
            Settings.autoDelay = (long)Settings.settingsObj.GetValue("AutoDelay");

            if (!Settings.settingsObj.TryGetValue("Auto", out _))
                Settings.settingsObj["Auto"] = true;
            Settings.auto = (bool)Settings.settingsObj.GetValue("Auto");

            if (!Settings.settingsObj.TryGetValue("ImageRetentionTime", out _))
                Settings.settingsObj["ImageRetentionTime"] = 12;
            Settings.imageRetentionTime = Convert.ToInt32(Settings.settingsObj.GetValue("ImageRetentionTime"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("ClipboardTemplate", out _))
                Settings.settingsObj["ClipboardTemplate"] = "-- by WFInfo (smart OCR with pricecheck)";
            Settings.ClipboardTemplate = Convert.ToString(Settings.settingsObj.GetValue("ClipboardTemplate"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("SnapitExport", out _))
                Settings.settingsObj["SnapitExport"] = false;
            Settings.SnapitExport = Convert.ToBoolean(Settings.settingsObj.GetValue("SnapitExport"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("Delay", out _))
                Settings.settingsObj["Delay"] = 10000;
            Settings.delay = Convert.ToInt32(Settings.settingsObj.GetValue("Delay"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("HighlightRewards", out _))
                Settings.settingsObj["HighlightRewards"] = true;
            Settings.Highlight = Convert.ToBoolean(Settings.settingsObj.GetValue("HighlightRewards"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("ClipboardVaulted", out _))
                Settings.settingsObj["ClipboardVaulted"] = false;
            Settings.ClipboardVaulted = (bool)Settings.settingsObj.GetValue("ClipboardVaulted");

            if (!Settings.settingsObj.TryGetValue("Auto", out _)) { //Fixes issue with older versions using an int for auto rather than boolean.
                Settings.settingsObj["Auto"] = false;
            } else if (Settings.settingsObj.GetValue("Auto").Type != JTokenType.Boolean) {
                Settings.settingsObj["Auto"] = true;
            }
            Settings.auto = (bool)Settings.settingsObj.GetValue("Auto");

            if (!Settings.settingsObj.TryGetValue("HighContrast", out _))
                Settings.settingsObj["HighContrast"] = false;
            Settings.highContrast = (bool)Settings.settingsObj.GetValue("HighContrast");

            if (!Settings.settingsObj.TryGetValue("OverlayXOffsetValue", out _))
                Settings.settingsObj["OverlayXOffsetValue"] = 0;
            Settings.overlayXOffsetValue = Convert.ToInt32(Settings.settingsObj.GetValue("OverlayXOffsetValue"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("OverlayYOffsetValue", out _))
                Settings.settingsObj["OverlayYOffsetValue"] = 0;
            Settings.overlayYOffsetValue = Convert.ToInt32(Settings.settingsObj.GetValue("OverlayYOffsetValue"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("AutoList", out _))
                Settings.settingsObj["AutoList"] = false;
            Settings.automaticListing = (bool)Settings.settingsObj.GetValue("AutoList");

            if (!Settings.settingsObj.TryGetValue("DoDoubleCheck", out _))
                Settings.settingsObj["DoDoubleCheck"] = true;
            Settings.doDoubleCheck = (bool)Settings.settingsObj.GetValue("DoDoubleCheck");
            
            if (!Settings.settingsObj.TryGetValue("MaximumEfficiencyValue", out _))
                Settings.settingsObj["MaximumEfficiencyValue"] = 9.5;
            Settings.maximumEfficiencyValue = Convert.ToDouble(Settings.settingsObj.GetValue("MaximumEfficiencyValue"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("MinimumEfficiencyValue", out _))
                Settings.settingsObj["MinimumEfficiencyValue"] = 4.5;
            Settings.minimumEfficiencyValue = Convert.ToDouble(Settings.settingsObj.GetValue("MinimumEfficiencyValue"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("DoSnapItCount", out _))
                Settings.settingsObj["DoSnapItCount"] = true;
            Settings.doSnapItCount = (bool)Settings.settingsObj.GetValue("DoSnapItCount");

            if (!Settings.settingsObj.TryGetValue("SnapItCountThreshold", out _))
                Settings.settingsObj["SnapItCountThreshold"] = 0;
            Settings.snapItCountThreshold = Convert.ToInt32(Settings.settingsObj.GetValue("SnapItCountThreshold"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("SnapItEdgeWidth", out _))
                Settings.settingsObj["SnapItEdgeWidth"] = 1;
            Settings.snapItEdgeWidth = Convert.ToInt32(Settings.settingsObj.GetValue("SnapItEdgeWidth"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("SnapItEdgeRadius", out _))
                Settings.settingsObj["SnapItEdgeRadius"] = 1;
            Settings.snapItEdgeRadius = Convert.ToInt32(Settings.settingsObj.GetValue("SnapItEdgeRadius"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("SnapItHorizontalNameMargin", out _))
                Settings.settingsObj["SnapItHorizontalNameMargin"] = 0;
            Settings.snapItHorizontalNameMargin = Convert.ToDouble(Settings.settingsObj.GetValue("SnapItHorizontalNameMargin"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("DoCustomNumberBoxWidth", out _))
                Settings.settingsObj["DoCustomNumberBoxWidth"] = false;
            Settings.doCustomNumberBoxWidth = (bool)Settings.settingsObj.GetValue("DoCustomNumberBoxWidth");

            if (!Settings.settingsObj.TryGetValue("SnapItNumberBoxWidth", out _))
                Settings.settingsObj["SnapItNumberBoxWidth"] = 0.4;
            Settings.snapItNumberBoxWidth = Convert.ToDouble(Settings.settingsObj.GetValue("SnapItNumberBoxWidth"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("SnapMultiThreaded", out _))
                Settings.settingsObj["SnapMultiThreaded"] = true;
            Settings.snapMultiThreaded = (bool)Settings.settingsObj.GetValue("SnapMultiThreaded");

            if (!Settings.settingsObj.TryGetValue("SnapRowTextDensity", out _))
                Settings.settingsObj["SnapRowTextDensity"] = 0.015;
            Settings.snapRowTextDensity = Convert.ToDouble(Settings.settingsObj.GetValue("SnapRowTextDensity"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("SnapRowEmptyDensity", out _))
                Settings.settingsObj["SnapRowEmptyDensity"] = 0.01;
            Settings.snapRowEmptyDensity = Convert.ToDouble(Settings.settingsObj.GetValue("SnapRowEmptyDensity"), Main.culture);

            if (!Settings.settingsObj.TryGetValue("SnapColEmptyDensity", out _))
                Settings.settingsObj["SnapColEmptyDensity"] = 0.005;
            Settings.snapColEmptyDensity = Convert.ToDouble(Settings.settingsObj.GetValue("SnapColEmptyDensity"), Main.culture);

            Settings.Save();

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WFinfo");
            if (key.GetValue("JWT") != null) // if the key exists then update it, else ignore it.
            {
                Main.dataBase.JWT = (string)key.GetValue("JWT");
                key.Close();
            }

        }

        public void OnContentRendered(object sender, EventArgs e)
        {
            if (welcomeDialogue != null)
            {
                welcomeDialogue.Left = Left + Width + 30;
                welcomeDialogue.Top = Top + Height / 2 - welcomeDialogue.Height / 2;
                welcomeDialogue.Show();
            }
        }

        /// <summary>
        /// Sets the status
        /// </summary>
        /// <param name="status">The string to be displayed</param>
        /// <param name="severity">0 = normal, 1 = red, 2 = orange, 3 =yellow</param>
        public void ChangeStatus(string status, int severity)
        {
            Debug.WriteLine("Status message: " + status);
            Status.Text = status;
            switch (severity)
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

        public void Exit(object sender, RoutedEventArgs e)
        {
            NotifyIcon.Dispose();
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WFinfo");
            if (Main.dataBase.rememberMe) // if rememberme was checked then save it
            {
                key.SetValue("JWT", Main.dataBase.JWT);
                key.Close();
            }
            Application.Current.Shutdown();
        }

        private void Minimise(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
        }
        
        private void WebsiteClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/N8S5zfw");
        }

        private void RelicsClick(object sender, RoutedEventArgs e)
        {
            if (Main.dataBase.relicData == null) { ChangeStatus("Relic data not yet loaded in", 2); return; }

            _relicsWindow.Show();
            _relicsWindow.Focus();
        }

        private void EquipmentClick(object sender, RoutedEventArgs e)
        {
            if (Main.dataBase.equipmentData == null) { ChangeStatus("Equipment data not yet loaded in", 2); return; }
            Main.equipmentWindow.Show();
        }

        private void Settings_click(object sender, RoutedEventArgs e)
        {
            if (Main.settingsWindow == null) { ChangeStatus("Settings window not yet loaded in", 2); return; }
            Main.settingsWindow.populate();
            Main.settingsWindow.Left = Left;
            Main.settingsWindow.Top = Top + Height;
            Main.settingsWindow.Show();
        }

        private void ReloadMarketClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            MarketData.Content = "Loading...";
            Main.StatusUpdate("Forcing Market Update", 0);
            Task.Factory.StartNew(Main.dataBase.ForceMarketUpdate);
        }

        private void ReloadDropClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            DropData.Content = "Loading...";
            Main.StatusUpdate("Forcing Prime Update", 0);
            Task.Factory.StartNew(Main.dataBase.ForceEquipmentUpdate);
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                try
                {
                    DragMove();
                }
                catch (Exception)
                {
                    Main.AddLog("Error in Mouse down in mainwindow");
                }
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            if (Settings.settingsObj != null)
            {
                if (Settings.settingsObj.TryGetValue("MainWindowLocation_X", out _))
                {
                    Settings.mainWindowLocation = new Point(Left, Top);
                    Settings.settingsObj["MainWindowLocation_X"] = Left;
                    Settings.settingsObj["MainWindowLocation_Y"] = Top;
                    Settings.Save();
                }
                else
                {
                    Settings.mainWindowLocation = new Point(100, 100);
                    Settings.settingsObj["MainWindowLocation_X"] = 100;
                    Settings.settingsObj["MainWindowLocation_Y"] = 100;
                    Settings.Save();
                }
            }
        }

        public void ToForeground(object sender, RoutedEventArgs e)
        {
            INSTANCE.Visibility = Visibility.Visible;
            INSTANCE.Activate();
            INSTANCE.Topmost = true;  // important
            INSTANCE.Topmost = false; // important
            INSTANCE.Focus();         // important
        }

        public void LoggedIn()
        {
            Login.Visibility = Visibility.Collapsed;
            ComboBox.SelectedIndex = 1;
            ComboBox.Visibility = Visibility.Visible;
            PlusOneButton.Visibility = Visibility.Visible;
            CreateListing.Visibility = Visibility.Visible;
            SearchItButton.Visibility = Visibility.Visible;
            ChangeStatus("Logged in", 0);
        }

        /// <summary>
        /// Prompts user to log in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpawnLogin(object sender, RoutedEventArgs e)
        {
            Main.login.MoveLogin(Left + Width, Top);

        }

        public void SignOut()
        {
            Login.Visibility = Visibility.Visible;
            ComboBox.Visibility = Visibility.Collapsed;
            PlusOneButton.Visibility = Visibility.Collapsed;
            CreateListing.Visibility = Visibility.Collapsed;
            SearchItButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Changes the online selector. Used for websocket lisening to see if the status changed externally (i.e from the site)
        /// </summary>
        /// <param name="status">The status to change to</param>
        public void UpdateMarketStatus(string status)
        {
            updatesupression = true;
            switch (status)
            {
                case "online":
                    if (ComboBox.SelectedIndex == 1) break;
                    ComboBox.SelectedIndex = 1;
                    break;
                case "invisible":
                    if (ComboBox.SelectedIndex == 2) break;
                    ComboBox.SelectedIndex = 2;
                    break;
                case "ingame":
                    if (ComboBox.SelectedIndex == 0) break;
                    ComboBox.SelectedIndex = 0;
                    break;
            }
            updatesupression = false;
        }

        /// <summary>
        /// Allows the user to overwrite the current websocket status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ComboBox.IsLoaded || updatesupression) //Prevent firing off to early
                return;
            switch (ComboBox.SelectedIndex)
            {
                case 0: //Online in game
                    Task.Run(async () =>
                    {
                        await Main.dataBase.SetWebsocketStatus("in game");
                    });
                    break;
                case 1: //Online
                    Task.Run(async () =>
                    {
                        await Main.dataBase.SetWebsocketStatus("online");
                    });
                    break;
                case 2: //Invisible
                    Task.Run(async () =>
                    {
                        await Main.dataBase.SetWebsocketStatus("offline");
                    });
                    break;
                case 3: //Sign out
                    LoggOut(null, null);
                    break;
            }
        }

        internal void LoggOut(object sender, CancelEventArgs e)
        {
            Login.Visibility = Visibility.Visible;
            ComboBox.Visibility = Visibility.Hidden;
            PlusOneButton.Visibility = Visibility.Hidden;
            CreateListing.Visibility = Visibility.Hidden;
            Task.Factory.StartNew(() => { Main.dataBase.Disconnect(); });
        }

        internal void FinishedLoading()
        {
            Login.IsEnabled = true;
        }

        private void CreateListing_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (OCR.processingActive)
            {
                Main.StatusUpdate("Still Processing Reward Screen", 2);
                return;
            }

            if (Main.listingHelper.PrimeRewards == null || Main.listingHelper.PrimeRewards.Count == 0)
            {
                ChangeStatus("No recorded rewards found", 2);
                return;
            }

            var t = Task.Run(() =>
            {
                foreach (var rewardscreen in Main.listingHelper.PrimeRewards)
                {
                    var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(rewardscreen)).Result;
                    if (rewardCollection.PrimeNames.Count == 0)
                        continue;
                    Main.listingHelper.ScreensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
                }
            });
            t.Wait();
            if (Main.listingHelper.ScreensList.Count == 0)
            {
                ChangeStatus("No recorded rewards found", 2);
                return;

            }
            Main.listingHelper.SetScreen(0);
            Main.listingHelper.PrimeRewards.Clear();
            WindowState = WindowState.Normal;
            Main.listingHelper.Show();
        }

        private void PlusOne(object sender, MouseButtonEventArgs e)
        {
            Main.plusOne.Show();
            Main.plusOne.Left = Left + Width;
            Main.plusOne.Top = Top;
        }

        private void SearchItButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (OCR.processingActive)
            {
                Main.StatusUpdate("Still Processing Reward Screen", 2);
                return;
            }
            Main.AddLog("Starting search it");
            Main.StatusUpdate("Starting search it", 0);
            Main.searchBox.Start();
        }
    }
}

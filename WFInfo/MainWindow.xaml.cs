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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WFInfo.Settings;

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
        private SettingsViewModel _settingsViewModel = SettingsViewModel.Instance;

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

                System.Drawing.Rectangle winBounds = new System.Drawing.Rectangle(Convert.ToInt32(_settingsViewModel.MainWindowLocation.X), Convert.ToInt32(_settingsViewModel.MainWindowLocation.Y), Convert.ToInt32(Width), Convert.ToInt32(Height));
                foreach (System.Windows.Forms.Screen scr in System.Windows.Forms.Screen.AllScreens)
                {
                    if (scr.Bounds.Contains(winBounds))
                    {
                        Left = _settingsViewModel.MainWindowLocation.X;
                        Top = _settingsViewModel.MainWindowLocation.Y;
                        break;
                    }
                }

                _settingsViewModel.MainWindowLocation = new Point(Left, Top);

                SettingsWindow.Save();

                Closing += new CancelEventHandler(LoggOut);
            }
            catch (Exception e)
            {
                Main.AddLog("An error occured while loading the main window: " + e.Message);
            }
            
            Application.Current.MainWindow = this;
        }



        public void InitializeSettings()
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            jsonSettings.Converters.Add(new StringEnumConverter());
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json") && !ApplicationSettings.GlobalSettings.Initialized)
            {
                var jsonText = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json");
                JsonConvert.PopulateObject(jsonText, ApplicationSettings.GlobalSettings, jsonSettings);
                ApplicationSettings.GlobalSettings.Initialized = true;

            }
            else
            {
                ApplicationSettings.GlobalSettings.Initialized = true;
                welcomeDialogue = new WelcomeDialogue();
            }


            try
            {
                Enum.Parse(typeof(Key), _settingsViewModel.ActivationKey);
            }
            catch
            {
                try
                {
                    Enum.Parse(typeof(MouseButton), _settingsViewModel.ActivationKey);
                }
                catch
                {
                    Main.AddLog("Couldn't Parse Activation Key -- Defaulting to PrintScreen");
                    _settingsViewModel.ActivationKey = "Snapshot";
                }
            }

            SettingsWindow.Save();

            Main.dataBase.JWT = EncryptedDataService.LoadStoredJWT();

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
            if (Main.dataBase.rememberMe)
            { // if rememberme was checked then save it
                EncryptedDataService.PersistJWT(Main.dataBase.JWT);
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
            _relicsWindow?.Close();
            _relicsWindow = new RelicsWindow();
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
            Main.settingsWindow?.Close();
            Main.settingsWindow = new SettingsWindow();
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
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
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
            _settingsViewModel.MainWindowLocation = new Point(Left, Top);
            SettingsWindow.Save();
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

        private void OpenAppDataFolder(object sender, MouseButtonEventArgs e)
        {
            Process.Start(Main.AppPath);
        }
    }
}
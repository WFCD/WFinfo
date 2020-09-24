using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;
using AutoUpdaterDotNET;
using System.Windows;
using System.Windows.Forms;
using WebSocketSharp;
using WFInfo.Resources;

namespace WFInfo
{
    class Main
    {
        public static Main INSTANCE;
        public static string AppPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        public static string buildVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static Data dataBase = new Data();
        public static RewardWindow window = new RewardWindow();
        public static Overlay[] overlays = new Overlay[4] { new Overlay(), new Overlay(), new Overlay(), new Overlay() };
        public static RelicsWindow relicWindow = new RelicsWindow();
        public static EquipmentWindow equipmentWindow = new EquipmentWindow();
        public static Settings settingsWindow = new Settings();
        public static ErrorDialogue popup;
        public static UpdateDialogue update;
        public static SnapItOverlay snapItOverlayWindow = new SnapItOverlay();
        public static SearchIt searchBox = new SearchIt();
        public static Login login = new Login();
        public static ListingHelper listingHelper = new ListingHelper();
        public static DateTime latestActive;
        public static PlusOne plusOne = new PlusOne();
        public static System.Threading.Timer timer;
        public static System.Drawing.Point lastClick;
        private static int minutesTillAfk = 7;

        private static bool UserAway { get; set; }

        public Main()
        {
            INSTANCE = this;
            StartMessage();
            buildVersion = buildVersion.Substring(0, buildVersion.LastIndexOf("."));

            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.Start("https://github.com/WFCD/WFinfo/releases/latest/download/update.xml");

            Task.Factory.StartNew(ThreadedDataLoad);
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            update = new UpdateDialogue(args);
        }

        public static void ThreadedDataLoad()
        {
            try
            {
                StatusUpdate("Updating Databases...", 0);

                dataBase.Update();

                //RelicsWindow.LoadNodesOnThread();
                OCR.Init();

                if ((bool)Settings.settingsObj["Auto"])
                    dataBase.EnableLogCapture();
                if (dataBase.IsJWTvalid().Result)
                {
                    OCR.VerifyWarframe();
                    latestActive = DateTime.UtcNow.AddMinutes(1);
                    LoggedIn();

                    var startTimeSpan = TimeSpan.Zero;
                    var periodTimeSpan = TimeSpan.FromMinutes(1);

                    timer = new System.Threading.Timer((e) =>
                    {
                        TimeoutCheck();
                    }, null, startTimeSpan, periodTimeSpan);
                }
                StatusUpdate("WFInfo Initialization Complete", 0);
                AddLog("WFInfo has launched successfully");
                FinishedLoading();
            }
            catch (Exception ex)
            {
                AddLog("LOADING FAILED");
                AddLog(ex.ToString());
                StatusUpdate("Launch Failure - Please Restart", 0);
                RunOnUIThread(() =>
                {
                    _ = new ErrorDialogue(DateTime.Now, 0);
                });
            }
        }
        private static async void TimeoutCheck()
        {
            if (!await dataBase.IsJWTvalid())
                return;
            var now = DateTime.UtcNow;
            Debug.WriteLine($"Checking if the user has been inactive \nNow: {now}, Lastactive: {latestActive}");
            
            if (OCR.Warframe.HasExited)
            {//set user offline if Warframe has closed but no new game was found
                await Task.Run(async () =>
                {
                    if (!await dataBase.IsJWTvalid())
                        return;
                    await dataBase.SetWebsocketStatus("invisible");
                    StatusUpdate("Could not find warframe.exe setting user offline", 0);
                });
            }
            if (latestActive <= now)
            {//set users offline if afk for longer than set timer
                await Task.Run(async () =>
                {
                UserAway = true;
                await dataBase.SetWebsocketStatus("invisible");
                StatusUpdate($"User has been inactive for {minutesTillAfk} minutes", 0);
                });
            }
            if (UserAway)
            {
                await Task.Run(async () =>
                {
                UserAway = false;
                await dataBase.SetWebsocketStatus("online");
                var user = dataBase.inGameName.IsNullOrEmpty() ? "user" : dataBase.inGameName;
                StatusUpdate($"Welcome back {user}, we've put you online", 0);
                });
            }
        }


        public static void RunOnUIThread(Action act)
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(act);
        }

        public static void StartMessage()
        {
            Directory.CreateDirectory(AppPath);
            Directory.CreateDirectory(AppPath + @"\debug");
            using (StreamWriter sw = File.AppendText(AppPath + @"\debug.log"))
            {
                sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");
                sw.WriteLineAsync("   STARTING WFINFO " + buildVersion + " at " + DateTime.UtcNow);
                sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");
            }
        }

        public static void AddLog(string argm)
        { //write to the debug file, includes version and UTCtime
            Debug.WriteLine(argm);
            Directory.CreateDirectory(AppPath);
            try
            {
                using (StreamWriter sw = File.AppendText(AppPath + @"\debug.log"))
                    sw.WriteLineAsync("[" + DateTime.UtcNow + " " + buildVersion + "]   " + argm);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Sets the status on the main window
        /// </summary>
        /// <param name="message">The string to be displayed</param>
        /// <param name="severity">0 = normal, 1 = red, 2 = orange, 3 =yellow</param>
        public static void StatusUpdate(string message, int severity)
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.ChangeStatus(message, severity); });
        }

        public void OnMouseAction(MouseButton key)
        {
            latestActive = DateTime.UtcNow.AddMinutes(minutesTillAfk);

            if (Settings.ActivationMouseButton != MouseButton.Left && key == Settings.ActivationMouseButton)
            { //check if user pressed activation key
                if (Keyboard.IsKeyDown(Key.Delete))
                { //Close all overlays if hotkey + delete is held down
                    foreach (Window overlay in App.Current.Windows)
                    {
                        if (overlay.GetType().ToString() == "WFInfo.Overlay")
                        {
                            overlay.Hide();
                        }
                    }
                    return;
                }

                if (searchBox.IsInUse)
                { //if key is pressed and searchbox is active then rederect keystokes to it.
                    if (Keyboard.IsKeyDown(Key.Escape))
                    { // close it if esc is used.
                        searchBox.Finish();
                        return;
                    }
                    searchBox.searchField.Focus();
                    return;
                }

                if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey) && Keyboard.IsKeyDown(Settings.SnapitModifierKey))
                { //snapit debug
                    AddLog("Loading screenshot from file for snapit");
                    StatusUpdate("Offline testing with screenshot for snapit", 0);
                    LoadScreenshotSnap();
                }
                else if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey))
                {//normal debug
                    AddLog("Loading screenshot from file");
                    StatusUpdate("Offline testing with screenshot", 0);
                    LoadScreenshot();
                }
                else if (Keyboard.IsKeyDown(Settings.SnapitModifierKey))
                {//snapit
                    AddLog("Starting snap it");
                    StatusUpdate("Starting snap it", 0);
                    OCR.SnapScreenshot();
                }
                else if (Keyboard.IsKeyDown(Settings.SearchItModifierKey))
                { //Searchit  
                    AddLog("Starting search it");
                    StatusUpdate("Starting search it", 0);
                    searchBox.Start();
                }
                else if (Settings.debug || OCR.VerifyWarframe())
                {
                    Task.Factory.StartNew(() => OCR.ProcessRewardScreen());
                }
            }
            else if (key == MouseButton.Left && OCR.Warframe != null && !OCR.Warframe.HasExited && Overlay.rewardsDisplaying)
            {
                Task.Run((() =>
                {
                    lastClick = System.Windows.Forms.Cursor.Position;
                    var index = OCR.GetSelectedReward(lastClick);
                    Debug.WriteLine(index);
                    if (index < 0) return;
                    listingHelper.SelectedRewardIndex = (short)index;
                }));
            }
        }

        public void OnKeyAction(Key key)
        {
            latestActive = DateTime.UtcNow.AddMinutes(minutesTillAfk);

            // close the snapit overlay when *any* key is pressed down
            if (snapItOverlayWindow.isEnabled && KeyInterop.KeyFromVirtualKey((int)key) != Key.None)
            {
                snapItOverlayWindow.closeOverlay();
                StatusUpdate("Closed snapit", 0);
                return;
            }
            if (searchBox.IsInUse)
            { //if key is pressed and searchbox is active then rederect keystokes to it.
                if (key == Key.Escape)
                { // close it if esc is used.
                    searchBox.Finish();
                    return;
                }
                searchBox.searchField.Focus();
                return;
            }

            
            if (key == Settings.ActivationKey)
            { //check if user pressed activation key
                Main.AddLog($"User is activating with pressing key: {key} and is holding down:\n" +
                                $"Delete:{Keyboard.IsKeyDown(Key.Delete)}\n" +
                                $"Snapit, {Settings.SnapitModifierKey}:{Keyboard.IsKeyDown(Settings.SnapitModifierKey)}\n" +
                                $"Searchit, {Settings.SearchItModifierKey}:{Keyboard.IsKeyDown(Settings.SearchItModifierKey)}\n" +
                                $"debug, {Settings.DebugModifierKey}:{Keyboard.IsKeyDown(Settings.DebugModifierKey)}");
                if (Keyboard.IsKeyDown(Key.Delete))
                { //Close all overlays if hotkey + delete is held down
                    foreach (Window overlay in App.Current.Windows)
                    {
                        if (overlay.GetType().ToString() == "WFInfo.Overlay")
                        {
                            overlay.Hide();
                        }
                    }
                    StatusUpdate("Overlays dismissed", 1);
                    return;
                }
                if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey) && Keyboard.IsKeyDown(Settings.SnapitModifierKey))
                { //snapit debug
                    AddLog("Loading screenshot from file for snapit");
                    StatusUpdate("Offline testing with screenshot for snapit", 0);
                    LoadScreenshotSnap();
                }
                else if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey))
                {//normal debug
                    AddLog("Loading screenshot from file");
                    StatusUpdate("Offline testing with screenshot", 0);
                    LoadScreenshot();
                }
                else if (Keyboard.IsKeyDown(Settings.SnapitModifierKey))
                {//snapit
                    AddLog("Starting snap it");
                    StatusUpdate("Starting snap it", 0);
                    OCR.SnapScreenshot();
                }
                else if (Keyboard.IsKeyDown(Settings.SearchItModifierKey))
                { //Searchit  
                    AddLog("Starting search it");
                    StatusUpdate("Starting search it", 0);
                    searchBox.Start();
                }
                else if (Settings.debug || OCR.VerifyWarframe())
                {
                    Task.Factory.StartNew(() => OCR.ProcessRewardScreen());
                }
            }
        }

        // timestamp is the time to look for, and gap is the threshold of seconds different
        public static void SpawnErrorPopup(DateTime timeStamp, int gap = 30)
        {
            popup = new ErrorDialogue(timeStamp, gap);
        }

        private void LoadScreenshot()
        {
            // Using WinForms for the openFileDialog because it's simpler and much easier
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            foreach (string file in openFileDialog.FileNames)
                            {
                                AddLog("Testing file: " + file);

                                //Get the path of specified file
                                Bitmap image = new Bitmap(file);
                                OCR.UpdateWindow(image);
                                OCR.ProcessRewardScreen(image);
                            }

                        }
                        catch (Exception e)
                        {
                            AddLog(e.Message);
                            StatusUpdate("Failed to load image", 1);
                        }
                    });
                }
                else
                {
                    StatusUpdate("Failed to load image", 1);
                    OCR.processingActive = false;
                }
            }
        }

        private void LoadScreenshotSnap()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            foreach (string file in openFileDialog.FileNames)
                            {
                                AddLog("Testing snapit on file: " + file);

                                Bitmap image = new Bitmap(file);
                                OCR.ProcessSnapIt(image, image, new System.Drawing.Point(0, 0));
                            }

                        }
                        catch (Exception e)
                        {
                            AddLog(e.Message);
                            StatusUpdate("Failed to load image", 1);
                        }
                    });
                }
                else
                {
                    StatusUpdate("Failed to load image", 1);
                }
            }
        }

        public static void LoggedIn()
        { //this is bullshit, but I couldn't call it in login.xaml.cs because it doesn't properly get to the main window
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.LoggedIn(); });
        }


        public static void FinishedLoading()
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.FinishedLoading(); });
        }
        public static void UpdateMarketStatus(string msg)
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.UpdateMarketStatus(msg); });
        }

        public static string BuildVersion { get => buildVersion; }

        public static int VersionToInteger(string vers)
        {
            int ret = 0;
            string[] versParts = Regex.Replace(vers, "[^0-9.]+", "").Split('.');
            if (versParts.Length == 3)
                for (int i = 0; i < versParts.Length; i++)
                {
                    if (versParts[i].Length == 0)
                        return -1;
                    ret += Convert.ToInt32(int.Parse(versParts[i], Main.culture) * Math.Pow(100, 2 - i));
                }

            return ret;
        }

        // Glob
        public static System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en");

        public static void SignOut()
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.SignOut(); });
        }
    }

    public class Status
    {
        public string Message { get; set; }
        public int Severity { get; set; }

        public Status(string msg, int ser)
        {
            Message = msg;
            Severity = ser;
        }
    }

}

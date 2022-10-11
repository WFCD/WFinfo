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
using System.Threading;
using System.Globalization;
using WebSocketSharp;
using WFInfo.Resources;
using WFInfo.Settings;

namespace WFInfo
{
    class Main
    {
        public static Main INSTANCE;
        public static string AppPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        public static string buildVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static Data dataBase = new Data(ApplicationSettings.GlobalReadonlySettings);
        public static RewardWindow window = new RewardWindow();
        public static Overlay[] overlays = new Overlay[4] { new Overlay(), new Overlay(), new Overlay(), new Overlay() };
        public static EquipmentWindow equipmentWindow = new EquipmentWindow();
        public static SettingsWindow settingsWindow = new SettingsWindow();
        public static VerifyCount verifyCount = new VerifyCount();
        public static ErrorDialogue popup;
        public static FullscreenReminder fullscreenpopup;
        public static UpdateDialogue update;
        public static SnapItOverlay snapItOverlayWindow = new SnapItOverlay();
        public static SearchIt searchBox = new SearchIt();
        public static Login login = new Login();
        public static ListingHelper listingHelper = new ListingHelper();
        public static DateTime latestActive;
        public static PlusOne plusOne = new PlusOne();
        public static System.Threading.Timer timer;
        public static System.Drawing.Point lastClick;
        private const int minutesTillAfk = 7;

        private static bool UserAway { get; set; }
        private static string LastMarketStatus { get; set; } = "invisible";
        private static string LastMarketStatusB4AFK { get; set; } = "invisible";
        private readonly IReadOnlyApplicationSettings _settings = ApplicationSettings.GlobalReadonlySettings;

        public Main()
        {
            INSTANCE = this;
            StartMessage();
            buildVersion = buildVersion.Substring(0, buildVersion.LastIndexOf("."));

            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.Start("https://github.com/WFCD/WFinfo/releases/latest/download/update.xml");

            Task.Factory.StartNew(ThreadedDataLoad);

            //string str = Properties.strings.Primary; To localize all hard coded text

        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            update = new UpdateDialogue(args);
        }

        public static void ThreadedDataLoad()
        {
            try
            {
                StatusUpdate(Properties.strings.Update_databases, 0);

                dataBase.Update();

                //RelicsWindow.LoadNodesOnThread();
                OCR.Init(new TesseractService(), new SoundPlayer(), ApplicationSettings.GlobalReadonlySettings);

                if (ApplicationSettings.GlobalReadonlySettings.Auto)
                    dataBase.EnableLogCapture();
                if (dataBase.IsJWTvalid().Result)
                {
                    OCR.VerifyWarframe();
                    LoggedIn();
                }
                StatusUpdate(Properties.strings.Init_complete, 0);
                AddLog("WFInfo has launched successfully");
                FinishedLoading();

                if (dataBase.JWT != null)// if token is loaded in, connect to websocket
                {
                    bool result = dataBase.OpenWebSocket().Result;
                    Debug.WriteLine("Logging into websocket success: " + result);
                }
            }
            catch (Exception ex)
            {
                AddLog("LOADING FAILED");
                AddLog(ex.ToString());
                StatusUpdate(ex.ToString().Contains("invalid_grant") ? "System time out of sync with server\nResync system clock in windows settings": "Launch Failure - Please Restart", 0);
                RunOnUIThread(() =>
                {
                    _ = new ErrorDialogue(DateTime.Now, 0);
                });
            }
        }
        private static async void TimeoutCheck()
        {
            if (!await dataBase.IsJWTvalid().ConfigureAwait(true))
                return;
            DateTime now = DateTime.UtcNow;
            Debug.WriteLine($"Checking if the user has been inactive \nNow: {now}, Lastactive: {latestActive}");

            if (OCR.Warframe != null && OCR.Warframe.HasExited && LastMarketStatus != "invisible")
            {//set user offline if Warframe has closed but no new game was found
                Debug.WriteLine($"Warframe was detected as closed");

                await Task.Run(async () =>
                {
                    if (!await dataBase.IsJWTvalid().ConfigureAwait(true))
                        return;
                    //IDE0058 - computed value is never used.  Ever. Consider changing the return signature of SetWebsocketStatus to void instead
                    await dataBase.SetWebsocketStatus("invisible").ConfigureAwait(false);
                    StatusUpdate("WFM status set offline, Warframe was closed", 0);
                }).ConfigureAwait(false);
            }
            else if (UserAway && latestActive > now)
            {
                Debug.WriteLine($"User has returned. Last Status was: {LastMarketStatusB4AFK}");
 
                UserAway = false;
                if (LastMarketStatusB4AFK != "invisible")
                {
                    await Task.Run(async () =>
                    {
                        await dataBase.SetWebsocketStatus(LastMarketStatusB4AFK).ConfigureAwait(false);
                        string user = dataBase.inGameName.IsNullOrEmpty() ? "user" : dataBase.inGameName;
                        StatusUpdate($"Welcome back {user}, restored as {LastMarketStatusB4AFK}", 0);
                    }).ConfigureAwait(false);
                }
                else
                {
                    StatusUpdate($"Welcome back user", 0);
                }
            }
            else if (!UserAway && latestActive <= now)
            {//set users offline if afk for longer than set timer
                LastMarketStatusB4AFK = LastMarketStatus;
                Debug.WriteLine($"User is now away - Storing last known user status as: {LastMarketStatusB4AFK}");
                
                UserAway = true;
                if (LastMarketStatus != "invisible")
                {
                    await Task.Run(async () =>
                    {
                        await dataBase.SetWebsocketStatus("invisible").ConfigureAwait(false);
                        StatusUpdate($"User has been inactive for {minutesTillAfk} minutes", 0);
                    }).ConfigureAwait(false);
                }
            }
            else
            {
                if (UserAway)
                {
                    Debug.WriteLine($"User is away - no status change needed.  Last known status was: {LastMarketStatusB4AFK}");
                }
                else
                {
                    Debug.WriteLine($"User is active - no status change needed");
                }
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

        public void ActivationKeyPressed(Object key)
        {
            //Log activation. Can't set activation key to left or right mouse button via UI so not differentiating between MouseButton and Key should be fine
            Main.AddLog($"User is activating with pressing key: {key} and is holding down:\n" +
                $"Delete:{Keyboard.IsKeyDown(Key.Delete)}\n" +
                $"Snapit, {_settings.SnapitModifierKey}:{Keyboard.IsKeyDown(_settings.SnapitModifierKey)}\n" +
                $"Searchit, {_settings.SearchItModifierKey}:{Keyboard.IsKeyDown(_settings.SearchItModifierKey)}\n" +
                $"Masterit, {_settings.MasterItModifierKey}:{Keyboard.IsKeyDown(_settings.MasterItModifierKey)}\n" +
                $"debug, {_settings.DebugModifierKey}:{Keyboard.IsKeyDown(_settings.DebugModifierKey)}");

            if (Keyboard.IsKeyDown(Key.Delete))
            { 
                //Close all overlays if hotkey + delete is held down
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

            if (_settings.Debug && Keyboard.IsKeyDown(_settings.DebugModifierKey) && Keyboard.IsKeyDown(_settings.SnapitModifierKey))
            { //snapit debug
                AddLog("Loading screenshot from file for snapit");
                StatusUpdate("Offline testing with screenshot for snapit", 0);
                LoadScreenshot(ScreenshotType.SNAPIT);
            } 
            else if (_settings.Debug && Keyboard.IsKeyDown(_settings.DebugModifierKey) && Keyboard.IsKeyDown(_settings.MasterItModifierKey))
            { //master debug
                AddLog("Loading screenshot from file for masterit");
                StatusUpdate("Offline testing with screenshot for masterit", 0);
                LoadScreenshot(ScreenshotType.MASTERIT);
            }
            else if (_settings.Debug && Keyboard.IsKeyDown(_settings.DebugModifierKey))
            {//normal debug
                AddLog("Loading screenshot from file");
                StatusUpdate("Offline testing with screenshot", 0);
                LoadScreenshot(ScreenshotType.NORMAL);
            }
            else if (Keyboard.IsKeyDown(_settings.SnapitModifierKey))
            {//snapit
                AddLog("Starting snap it");
                StatusUpdate("Starting snap it", 0);
                OCR.SnapScreenshot();
            }
            else if (Keyboard.IsKeyDown(_settings.SearchItModifierKey))
            { //Searchit  
                AddLog("Starting search it");
                StatusUpdate("Starting search it", 0);
                searchBox.Start();
            }
            else if (Keyboard.IsKeyDown(_settings.MasterItModifierKey))
            {//masterit
                AddLog("Starting master it");
                StatusUpdate("Starting master it", 0);
                Task.Factory.StartNew(() => {
                    Bitmap bigScreenshot = OCR.CaptureScreenshot();
                    OCR.ProcessProfileScreen(bigScreenshot);
                    bigScreenshot.Dispose();
                });
            }
            else if (_settings.Debug || OCR.VerifyWarframe())
            {
                Task.Factory.StartNew(() => OCR.ProcessRewardScreen());
            }
        }

        public void OnMouseAction(MouseButton key)
        {
            latestActive = DateTime.UtcNow.AddMinutes(minutesTillAfk);

            if (_settings.ActivationMouseButton != null && key == _settings.ActivationMouseButton)
            { //check if user pressed activation key


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

                ActivationKeyPressed(key);


            }
            else if (key == MouseButton.Left && OCR.Warframe != null && !OCR.Warframe.HasExited && Overlay.rewardsDisplaying)
            {
                Task.Run((() =>
                {
                    lastClick = System.Windows.Forms.Cursor.Position;
                    int index = OCR.GetSelectedReward(lastClick);
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

            
            if (key == _settings.ActivationKeyKey)
            { //check if user pressed activation key

                ActivationKeyPressed(key);

            }
        }

        // timestamp is the time to look for, and gap is the threshold of seconds different
        public static void SpawnErrorPopup(DateTime timeStamp, int gap = 30)
        {
            popup = new ErrorDialogue(timeStamp, gap);
        }
        
        public static void SpawnFullscreenReminder()
        {
            fullscreenpopup = new FullscreenReminder();
        }


        public enum ScreenshotType 
        {
            NORMAL,
            SNAPIT,
            MASTERIT
        }
        private void LoadScreenshot(ScreenshotType type)
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
                    Task.Factory.StartNew(
                        () =>
                    {
                        try
                        {
                            foreach (string file in openFileDialog.FileNames)
                            {
                                if (type == ScreenshotType.NORMAL)
                                {
                                    AddLog("Testing file: " + file);

                                    //Get the path of specified file
                                    Bitmap image = new Bitmap(file);
                                    OCR.UpdateWindow(image);
                                    OCR.ProcessRewardScreen(image);
                                } else if (type == ScreenshotType.SNAPIT)
                                {
                                    AddLog("Testing snapit on file: " + file);

                                    Bitmap image = new Bitmap(file);
                                    OCR.UpdateWindow(image);
                                    OCR.ProcessSnapIt(image, image, new System.Drawing.Point(0, 0));
                                } else if (type == ScreenshotType.MASTERIT)
                                {
                                    AddLog("Testing masterit on file: " + file);

                                    Bitmap image = new Bitmap(file);
                                    OCR.UpdateWindow(image);
                                    OCR.ProcessProfileScreen(image);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            AddLog(e.Message);
                            AddLog(e.StackTrace);
                            StatusUpdate("Failed to load image", 1);
                        }
                    });
                }
                else
                {
                    StatusUpdate("Failed to load image", 1);
                    if (type == ScreenshotType.NORMAL)
                    {
                        OCR.processingActive = false;
                    }
                }
            }
        }

        // Switch to logged in mode for warfrane.market systems
        public static void LoggedIn()
        { //this is bullshit, but I couldn't call it in login.xaml.cs because it doesn't properly get to the main window
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.LoggedIn(); });

            // start the AFK timer
            latestActive = DateTime.UtcNow.AddMinutes(1);
            TimeSpan startTimeSpan = TimeSpan.Zero;
            TimeSpan periodTimeSpan = TimeSpan.FromMinutes(1);
            
            timer = new System.Threading.Timer((e) =>
            {
                TimeoutCheck();
            }, null, startTimeSpan, periodTimeSpan);
        }


        public static void FinishedLoading()
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.FinishedLoading(); });
        }
        public static void UpdateMarketStatus(string msg)
        {
            Debug.WriteLine($"New market status received: {msg}");
            if (!UserAway)
            {
                // AFK system only cares about a status that the user set
                LastMarketStatus = msg;
                Debug.WriteLine($"User is not away. last known market status will be: {LastMarketStatus}");
            }
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
        public static System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en", false);

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

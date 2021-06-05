using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using Tesseract;
using Brushes = System.Drawing.Brushes;
using Clipboard = System.Windows.Forms.Clipboard;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using Pen = System.Drawing.Pen;
using Point = System.Drawing.Point;
using Rect = Tesseract.Rect;
using Size = System.Drawing.Size;

namespace WFInfo
{
    class OCR
    {
        private static readonly string applicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        #region variabels and sizzle
        public enum WFtheme : int
        {
            VITRUVIAN,
            STALKER,
            BARUUK,
            CORPUS,
            FORTUNA,
            GRINEER,
            LOTUS,
            NIDUS,
            OROKIN,
            TENNO,
            HIGH_CONTRAST,
            LEGACY,
            EQUINOX,
            DARK_LOTUS,
            ZEPHYR,
            UNKNOWN = -1
        }

        // Colors for the top left "profile bar"
        public static Color[] ThemePrimary = new Color[] {  Color.FromArgb(190, 169, 102),		//VITRUVIAN		
															Color.FromArgb(153,  31,  35), 	    //STALKER		
															Color.FromArgb(238, 193, 105),  	//BARUUK		
															Color.FromArgb( 35, 201, 245),  	//CORPUS		
															Color.FromArgb( 57, 105, 192),  	//FORTUNA		
															Color.FromArgb(255, 189, 102),  	//GRINEER		
															Color.FromArgb( 36, 184, 242),  	//LOTUS			
															Color.FromArgb(140,  38,  92),  	//NIDUS			
															Color.FromArgb( 20,  41,  29),  	//OROKIN		
															Color.FromArgb(  9,  78, 106),  	//TENNO			
															Color.FromArgb(  2, 127, 217),  	//HIGH_CONTRAST	
															Color.FromArgb(255, 255, 255),  	//LEGACY		
															Color.FromArgb(158, 159, 167),  	//EQUINOX		
															Color.FromArgb(140, 119, 147),      //DARK_LOTUS
                                                            Color.FromArgb(253, 132,   2), };   //ZEPHER

    //highlight colors from selected items
    public static Color[] ThemeSecondary = new Color[] {    Color.FromArgb(245, 227, 173),		//VITRUVIAN		
															Color.FromArgb(255,  61,  51), 	//STALKER		
															Color.FromArgb(236, 211, 162),  	//BARUUK		
															Color.FromArgb(111, 229, 253),  	//CORPUS		
															Color.FromArgb(255, 115, 230),  	//FORTUNA		
															Color.FromArgb(255, 224, 153),  	//GRINEER		
															Color.FromArgb(255, 241, 191),  	//LOTUS			
															Color.FromArgb(245,  73,  93),  	//NIDUS			
															Color.FromArgb(178, 125,   5),  	//OROKIN		
															Color.FromArgb(  6, 106,  74),  	//TENNO			
															Color.FromArgb(255, 255,   0),  	//HIGH_CONTRAST	
															Color.FromArgb(232, 213,  93),  	//LEGACY		
															Color.FromArgb(232, 227, 227),  	//EQUINOX		
															Color.FromArgb(189, 169, 237),      //DARK_LOTUS	
                                                            Color.FromArgb(255,  53,   0) };    //ZEPHER	


    public static Assembly assembly = Assembly.GetExecutingAssembly();
        public static Stream audioStream = assembly.GetManifestResourceStream("WFInfo.Resources.achievment_03.wav");
        public static System.Media.SoundPlayer player = new System.Media.SoundPlayer(audioStream);

        private static int numberOfRewardsDisplayed;

        public static WindowStyle currentStyle;
        public enum WindowStyle
        {
            FULLSCREEN,
            BORDERLESS,
            WINDOWED
        }
        public static HandleRef HandleRef { get; private set; }
        public static Process Warframe = null;
        public static Point center;
        public static Rectangle window;

        private const NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;
        private static readonly IFormatProvider provider = CultureInfo.CreateSpecificCulture("en-GB");

        //public static float dpi;
        //private static double ScreenScaling; // Additional to settings.scaling this is used to calculate any widescreen or 4:3 aspect content.
        //private static double TotalScaling;

        // DPI - Only used to display on screen or to get the "actual" screen bounds
        public static double dpiScaling;
        // UI - Scaling used in Warframe
        public static double uiScaling;
        // Screen / Resolution Scaling - Used to adjust pixel values to each person's monitor
        public static double screenScaling;

        public static TesseractEngine firstEngine;
        public static TesseractEngine secondEngine;
        public static TesseractEngine[] engines = new TesseractEngine[4];
        public static Regex RE = new Regex("[^a-z가-힣]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Pixel measurements for reward screen @ 1920 x 1080 with 100% scale https://docs.google.com/drawings/d/1Qgs7FU2w1qzezMK-G1u9gMTsQZnDKYTEU36UPakNRJQ/edit
        public const int pixleRewardWidth = 968;
        public const int pixleRewardHeight = 235;
        public const int pixleRewardYDisplay = 316;
        public const int pixelRewardLineHeight = 48;

        public const int SCALING_LIMIT = 100;
        public static bool processingActive = false;

        private static Bitmap bigScreenshot;
        private static Bitmap partialScreenshot;
        public static Bitmap RewarIndexScreenshot;
        private static Bitmap partialScreenshotExpanded;

        private static WFtheme activeTheme;
        private static string[] firstChecks;
        private static List<string> secondChecks;
#pragma warning disable IDE0044 // Add readonly modifier
        private static int[] firstProximity = { -1, -1, -1, -1 };
        private static int[] secondProximity = { -1, -1, -1, -1 }; //TODO is never being written to, essentialy dissabling slow processing
#pragma warning restore IDE0044 // Add readonly modifier
        private static string timestamp;

        private static string clipboard;
        #endregion

        static void getLocaleTessdata()
        {
            string traineddata_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/libs/tessdata/";
            JObject traineddata_checksums = new JObject
            {
                {"en", "7af2ad02d11702c7092a5f8dd044d52f"},
                {"ko", "c776744205668b7e76b190cc648765da"}
            };

            // get trainned data
            string traineddata_hotlink = traineddata_hotlink_prefix + Settings.locale + ".traineddata";
            string app_data_traineddata_path = CustomEntrypoint.appdata_tessdata_folder + @"\" + Settings.locale + ".traineddata";

            WebClient webClient = new WebClient();

            if (!File.Exists(app_data_traineddata_path) || CustomEntrypoint.GetMD5hash(app_data_traineddata_path) != traineddata_checksums.GetValue(Settings.locale).ToObject<string>())
            {
                try
                {
                    webClient.DownloadFile(traineddata_hotlink, app_data_traineddata_path);
                }
                catch (Exception) { }
            }
        }
        static OCR()
        {
            getLocaleTessdata();
            firstEngine = new TesseractEngine(applicationDirectory + @"\tessdata", Settings.locale)
            {
                DefaultPageSegMode = PageSegMode.SingleBlock
            };

            secondEngine = new TesseractEngine(applicationDirectory + @"\tessdata", Settings.locale)
            {
                DefaultPageSegMode = PageSegMode.SingleBlock
            };


        }

        public static void Init()
        {
            Directory.CreateDirectory(Main.AppPath + @"\Debug");

            for (int i = 0; i < 4; i++)
            {
                if(engines[i] != null)
                {
                    engines[i].Dispose();
                }
                engines[i] = new TesseractEngine(applicationDirectory + @"\tessdata", Settings.locale)
                {
                    DefaultPageSegMode = PageSegMode.SingleBlock
                };
            }
        }

        internal static void ProcessRewardScreen(Bitmap file = null)
        {
            #region initializers
            if (processingActive)
            {
                Main.StatusUpdate("Still Processing Reward Screen", 2);
                return;
            }

            var primeRewards = new List<string>();

            processingActive = true;
            Main.StatusUpdate("Processing...", 0);
            Main.AddLog("----  Triggered Reward Screen Processing  ------------------------------------------------------------------");

            DateTime time = DateTime.UtcNow;
            timestamp = time.ToString("yyyy-MM-dd HH-mm-ssff", Main.culture);
            var watch = new Stopwatch();
            watch.Start();
            long start = watch.ElapsedMilliseconds;

            List<Bitmap> parts;

            bigScreenshot = file ?? CaptureScreenshot();
            try
            {
				    parts = ExtractPartBoxAutomatically(out uiScaling, out activeTheme, bigScreenshot);
            }
            catch (Exception e)
            {
                processingActive = false;
                Debug.WriteLine(e);
                return;
            }


            firstChecks = new string[parts.Count];
            Task[] tasks = new Task[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                int tempI = i;
                tasks[i] = Task.Factory.StartNew(() => { firstChecks[tempI] = OCR.GetTextFromImage(parts[tempI], engines[tempI]);});
            }
            Task.WaitAll(tasks);

            // Remove any empty items from the array
            firstChecks = firstChecks.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            if (firstChecks == null || firstChecks.Length == 0 || CheckIfError())
            {
                processingActive = false;
                Main.AddLog(("----  Partial Processing Time, couldn't find rewards " + (watch.ElapsedMilliseconds - start) + " ms  ------------------------------------------------------------------------------------------").Substring(0, 108));
                Main.StatusUpdate("Couldn't find any rewards to display", 2);
                if (firstChecks == null)
                {
                    Main.RunOnUIThread(() =>
                    {
                        Main.SpawnErrorPopup(time);
                    });
                }
            }
            double bestPlat = 0;
            int bestDucat = 0;
            int bestPlatItem = 0;
            int bestDucatItem = 0;
            List<int> unownedItems = new List<int>();

            #endregion

            #region processing data
            if (firstChecks.Length > 0)
            {
                clipboard = string.Empty;
                int width = (int)(pixleRewardWidth * screenScaling * uiScaling) + 10;
                int startX = center.X - width / 2 + (int)(width * 0.004);
                if (firstChecks.Length == 3 && firstChecks[0].Length > 0) { startX += width / 8; }
                if (firstChecks.Length == 4 && firstChecks[0].Replace(" ", "").Length < 6) { startX += 2 * (width / 8); }
                int overWid = (int)(width / (4.1 * dpiScaling));
                int startY = (int)(center.Y / dpiScaling - 20 * screenScaling * uiScaling);
                int partNumber = 0;
                bool hideRewardInfo = false;
                for (int i = 0; i < firstChecks.Length; i++)
                {
                    string part = firstChecks[i];
                    if (part.Replace(" ", "").Length > 6)
                    {
                        #region found a part
                        string correctName = Main.dataBase.GetPartName(part, out firstProximity[i]);
                        JObject job = Main.dataBase.marketData.GetValue(correctName).ToObject<JObject>();
                        string ducats = job["ducats"].ToObject<string>();
                        if (int.Parse(ducats, Main.culture) == 0)
                        {
                            hideRewardInfo = true;
                        }
                        //else if (correctName != "Kuva" || correctName != "Exilus Weapon Adapter Blueprint" || correctName != "Riven Sliver" || correctName != "Ayatan Amber Star")
                        primeRewards.Add(correctName);
                        string plat = job["plat"].ToObject<string>();
                        double platinum = double.Parse(plat, styles, provider);
                        string volume = job["volume"].ToObject<string>();
                        bool vaulted = Main.dataBase.IsPartVaulted(correctName);
                        string partsOwned = Main.dataBase.PartsOwned(correctName);
                        string partsCount = Main.dataBase.PartsCount(correctName);
                        int duc = int.Parse(ducats, Main.culture);
                        numberOfRewardsDisplayed++;
                        #endregion

                        #region hilighting
                        if (platinum >= bestPlat)
                        {
                            bestPlat = platinum; bestPlatItem = i;
                            if (duc >= bestDucat)
                            {
                                bestDucat = duc; bestDucatItem = i;
                            }
                        }
                        if (duc > bestDucat)
                        {
                            bestDucat = duc; bestDucatItem = i;
                        }
                        if (duc > 0)
                        {
                            if (int.Parse(partsOwned, Main.culture) < int.Parse(partsCount, Main.culture))
                            {
                                unownedItems.Add(i);
                            }
                        }
                        #endregion

                        #region clipboard
                        if (platinum > 0)
                        {
                            if (!string.IsNullOrEmpty(clipboard)) { clipboard += "-  "; }

                            clipboard += "[" + correctName.Replace(" Blueprint", "") + "]: " + plat + ":platinum: ";

                            if (Settings.ClipboardVaulted)
                            {
                                clipboard += ducats + ":ducats:";
                                if (vaulted)
                                    clipboard += "(V)";
                            }
                        }

                        if ((partNumber == firstChecks.Length - 1) && (!string.IsNullOrEmpty(clipboard)))
                        {
                            clipboard += Settings.ClipboardTemplate;
                        }
                        #endregion

                        #region display part
                        Main.RunOnUIThread(() =>
                        {
                            Overlay.rewardsDisplaying = true;

                            if (Settings.isOverlaySelected)
                            {
                                Main.overlays[partNumber].LoadTextData(correctName, plat, ducats, volume, vaulted, $"{partsOwned} / {partsCount}", "", hideRewardInfo);
                                Main.overlays[partNumber].Resize(overWid);
                                Main.overlays[partNumber].Display((int)((startX + width / 4 * partNumber + Settings.overlayXOffsetValue) / dpiScaling), startY + (int)(Settings.overlayYOffsetValue / dpiScaling), Settings.delay);
                            }
                            else if (!Settings.isLightSelected)
                            {
                                Main.window.loadTextData(correctName, plat, ducats, volume, vaulted, $"{partsOwned} / {partsCount}", partNumber, true, hideRewardInfo);
                            }
                            //else
                                //Main.window.loadTextData(correctName, plat, ducats, volume, vaulted, $"{partsOwned} / {partsCount}", partNumber, false, hideRewardInfo);

                            if (Settings.clipboard && !string.IsNullOrEmpty(clipboard))
                                Clipboard.SetText(clipboard);

                        });
                        partNumber++;
                        hideRewardInfo = false;
                        #endregion
                    }
                }
                var end = watch.ElapsedMilliseconds;
                Main.StatusUpdate("Completed processing (" + (end - start) + "ms)", 0);

                if (Main.listingHelper.PrimeRewards.Count == 0 || Main.listingHelper.PrimeRewards.Last().Except(primeRewards).ToList().Count != 0)
                {
                    Main.listingHelper.PrimeRewards.Add(primeRewards);
                }

                if (Settings.Highlight)
                {
                    Main.RunOnUIThread(() =>
                    {
                        foreach (int item in unownedItems)
                        {
                            Main.overlays[item].BestOwnedChoice();
                        }
                        Main.overlays[bestDucatItem].BestDucatChoice();
                        Main.overlays[bestPlatItem].BestPlatChoice();
                    });
                }

                if (partialScreenshot.Height < 70 && Settings.doDoubleCheck)
                {
                    // SlowSecondProcess(); secondProximity is never being written to, thus this will always result in that there is no change in the first scan. I've commented this out to increase preformance. @Dapal
                    end = watch.ElapsedMilliseconds;
                    Main.StatusUpdate("Completed second pass(" + (end - start) + "ms)", 0);
                }
                Main.AddLog(("----  Total Processing Time " + (end - start) + " ms  ------------------------------------------------------------------------------------------").Substring(0, 108));
                watch.Stop();
            }
            #endregion 

            if (Settings.isLightSelected && clipboard.Length > 3) //light mode doesn't have any visual confirmation that the ocr has finished, thus we use a sound to indicate this.
            {
                player.Play();
            }


            (new DirectoryInfo(Main.AppPath + @"\Debug\")).GetFiles()
                .Where(f => f.CreationTime < DateTime.Now.AddHours(-1 * Settings.imageRetentionTime))
                .ToList().ForEach(f => f.Delete());

            if (partialScreenshot != null)
            {
                partialScreenshot.Save(Main.AppPath + @"\Debug\PartBox " + timestamp + ".png");
                partialScreenshot.Dispose();
                partialScreenshot = null;
            }

            processingActive = false;

        }

        internal static int GetSelectedReward(Point lastClick)
        {
            Debug.WriteLine(lastClick.ToString());
            var primeRewardIndex = 0;
            lastClick.Offset(-window.X, -window.Y);
            var width = window.Width * (int)dpiScaling;
            var height = window.Height * (int)dpiScaling;
            var mostWidth = (int)(pixleRewardWidth * screenScaling * uiScaling);
            var mostLeft = (width / 2) - (mostWidth / 2);
            var bottom = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight) * screenScaling * 0.5 * uiScaling);
            var top = height / 2 - (int)((pixleRewardYDisplay) * screenScaling * uiScaling);
            var selectionRectangle = new Rectangle(mostLeft, top, mostWidth, bottom / 2);
            if (numberOfRewardsDisplayed == 3)
            {
                var offset = selectionRectangle.Width / 8;
                selectionRectangle = new Rectangle(selectionRectangle.X + offset, selectionRectangle.Y, selectionRectangle.Width - offset * 2, selectionRectangle.Height);
            }

            if (!selectionRectangle.Contains(lastClick))
                return -1;
            var middelHeight = top + bottom / 4;
            var length = mostWidth / 8;


            var RewardPoints4 = new List<Point>() {
            new Point(mostLeft + length, middelHeight),
            new Point(mostLeft + 3 * length, middelHeight),
            new Point(mostLeft + 5 * length, middelHeight),
            new Point(mostLeft + 7 * length, middelHeight)};

            var RewardPoints3 = new List<Point>() {
            new Point(mostLeft + 2 * length, middelHeight),
            new Point(mostLeft + 4 * length, middelHeight),
            new Point(mostLeft + 6 * length, middelHeight)};

            var lowestDistance = int.MaxValue;
            var lowestDistancePoint = new Point();
            if (numberOfRewardsDisplayed != 3)
            {
                foreach (var pnt in RewardPoints4)
                {
                    var distanceToLastClick = ((lastClick.X - pnt.X) * (lastClick.X - pnt.X) + (lastClick.Y - pnt.Y) * (lastClick.Y - pnt.Y));
                    Debug.WriteLine($"current point: {pnt}, with distance: {distanceToLastClick}");

                    if (distanceToLastClick >= lowestDistance) continue;
                    lowestDistance = distanceToLastClick;
                    lowestDistancePoint = pnt;
                    primeRewardIndex = RewardPoints4.IndexOf(pnt);
                }

                if (numberOfRewardsDisplayed == 2)
                {
                    if (primeRewardIndex == 1)
                        primeRewardIndex = 0;
                    if (primeRewardIndex >= 2)
                        primeRewardIndex = 1;
                }
            }
            else
            {
                foreach (var pnt in RewardPoints3)
                {
                    var distanceToLastClick = ((lastClick.X - pnt.X) * (lastClick.X - pnt.X) + (lastClick.Y - pnt.Y) * (lastClick.Y - pnt.Y));
                    Debug.WriteLine($"current point: {pnt}, with distance: {distanceToLastClick}");

                    if (distanceToLastClick >= lowestDistance) continue;
                    lowestDistance = distanceToLastClick;
                    lowestDistancePoint = pnt;
                    primeRewardIndex = RewardPoints3.IndexOf(pnt);
                }
            }

            #region  debuging image
            /*Debug.WriteLine($"Closest point: {lowestDistancePoint}, with distance: {lowestDistance}");

            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff", Main.culture);
            var img = CaptureScreenshot();
            var pinkP = new Pen(Brushes.Pink);
            var blackP = new Pen(Brushes.Black);
            using (Graphics g = Graphics.FromImage(img))
            {
                g.DrawRectangle(blackP, selectionRectangle);
                if (numberOfRewardsDisplayed != 3)
                {
                    foreach (var pnt in RewardPoints4)
                    {
                        pnt.Offset(-5, -5);
                        g.DrawEllipse(blackP, new Rectangle(pnt, new Size(10, 10)));
                    }
                }
                else
                {
                    foreach (var pnt in RewardPoints3)
                    {
                        pnt.Offset(-5, -5);
                        g.DrawEllipse(blackP, new Rectangle(pnt, new Size(10, 10)));
                    }
                }

                g.DrawString($"User selected reward nr{primeRewardIndex}", new Font(FontFamily.GenericMonospace, 16), Brushes.Chartreuse, lastClick);
                g.DrawLine(pinkP, lastClick, lowestDistancePoint);
                lastClick.Offset(-5, -5);

                g.DrawEllipse(pinkP, new Rectangle(lastClick, new Size(10, 10)));
            }
            img.Save(Main.AppPath + @"\Debug\GetSelectedReward " + timestamp + ".png");
            pinkP.Dispose();
            blackP.Dispose();
            img.Dispose();*/
            #endregion
            return primeRewardIndex;
        }

        private const double ERROR_DETECTION_THRESH = 0.25;
        private static bool CheckIfError()
        {
            if (firstChecks == null || firstProximity == null || secondChecks == null || secondProximity == null)
                return false;

            int max = Math.Min(Math.Min(Math.Min(firstChecks.Length, firstProximity.Length), secondChecks.Count), secondProximity.Length);
            for (int i = 0; i < max; i++)
                if (firstProximity[i] > ERROR_DETECTION_THRESH * firstChecks[i].Length &&
                  (secondProximity[i] == -1 || secondProximity[i] > ERROR_DETECTION_THRESH * secondChecks[i].Length))
                    return true;

            return false;

        }

        public static void SlowSecondProcess()
        {
            #region initilizers
            var tempclipboard = "";
            Bitmap newFilter = ScaleUpAndFilter(partialScreenshot, activeTheme);
            partialScreenshotExpanded.Save(Main.AppPath + @"\Debug\PartShotUpscaled " + timestamp + ".png");
            newFilter.Save(Main.AppPath + @"\Debug\PartShotUpscaledFiltered " + timestamp + ".png");
            Main.AddLog(("----  SECOND OCR CHECK  ------------------------------------------------------------------------------------------").Substring(0, 108));
            secondChecks = SeparatePlayers(newFilter, secondEngine);
            var primeRewards = new List<string>();

            var tempAmountOfRewardsDisplayed = 0;
            double bestPlat = 0;
            var bestDucat = 0;
            var bestPlatItem = 0;
            var bestDucatItem = 0;
            List<int> unownedItems = new List<int>();
            bool hideRewardInfo = false;
            int partNumber = 0;
            #endregion
            try
            {
                for (int i = 0; i < firstChecks.Length; i++)
                {
                    Debug.WriteLine(firstChecks[i]);
                    string first = firstChecks[i];
                    if (secondProximity[i] == -1) {
                        Main.AddLog("Second proximity was not set");
                        continue;
                    } else {
                        Main.AddLog($"First proximity {firstProximity[i]}, Second proximity {secondProximity[i]} Is the newer closer?: {secondProximity[i] > firstProximity[i]}");
                    }
                    if (first.Replace(" ", "").Length > 6)
                    {
                        Debug.WriteLine(secondChecks[i]);
                        string second = secondChecks[i];
                        string secondName = Main.dataBase.GetPartName(second, out secondProximity[i]);
                        //if (secondProximity[i] < firstProximity[i])
                        //{
                        JObject job = Main.dataBase.marketData.GetValue(secondName).ToObject<JObject>();
                        string ducats = job["ducats"].ToObject<string>();
                        string plat = job["plat"].ToObject<string>();
                        string volume = job["volume"].ToObject<string>();
                        bool vaulted = Main.dataBase.IsPartVaulted(secondName);
                        string partsOwned = Main.dataBase.PartsOwned(secondName);
                        string partsCount = Main.dataBase.PartsCount(secondName);
                        double platinum = double.Parse(plat, styles, provider);
                        int duc = int.Parse(ducats, Main.culture);
                        tempAmountOfRewardsDisplayed++;

                        if (duc == 0)
                        {
                            hideRewardInfo = true;
                        }
                        //else if (secondName != "Kuva" || secondName != "Exilus Weapon Adapter Blueprint" || secondName != "Riven Sliver" || secondName != "Ayatan Amber Star")
                        //{
                        Debug.WriteLine($"Adding : {secondName}");
                        primeRewards.Add(secondName);
                        //}
                        #region clipboard
                        if (platinum > 0)
                        {
                            if (!string.IsNullOrEmpty(tempclipboard)) { tempclipboard += "-  "; }

                            tempclipboard += "[" + secondName.Replace(" Blueprint", "") + "]: " + platinum + ":platinum: ";

                            if (Settings.ClipboardVaulted)
                            {
                                tempclipboard += ducats + ":ducats:";
                                if (vaulted)
                                    tempclipboard += "(V)";
                            }
                        }
                        if ((partNumber == firstChecks.Length - 1) && (!string.IsNullOrEmpty(tempclipboard)))
                        {
                            tempclipboard += Settings.ClipboardTemplate;
                        }

                        #endregion

                        #region highlight
                        if (platinum >= bestPlat)
                        {
                            bestPlat = platinum; bestPlatItem = i;
                            if (duc >= bestDucat)
                            {
                                bestDucat = duc; bestDucatItem = i;
                            }
                        }
                        if (duc > bestDucat)
                        {
                            bestDucat = duc; bestDucatItem = i;
                        }
                        if (duc > 0)
                        {
                            if (int.Parse(partsOwned, Main.culture) < int.Parse(partsCount, Main.culture))
                            {
                                unownedItems.Add(i);
                            }
                        }
                        #endregion

                        #region display

                        Main.RunOnUIThread(() =>
                        {
                            if (Settings.isOverlaySelected)
                            {
                                Main.overlays[partNumber].LoadTextData(secondName, plat, ducats, volume, vaulted, $"{partsOwned} / {partsCount}", "", hideRewardInfo);
                            }
                            else if (!Settings.isLightSelected)
                            {
                                Main.overlays[partNumber].LoadTextData(secondName, plat, ducats, volume, vaulted, $"{partsOwned} / {partsCount}", "", hideRewardInfo);
                            }
                            else
                                Main.window.loadTextData(secondName, plat, ducats, volume, vaulted, $"{partsOwned} / {partsCount}", partNumber, false, hideRewardInfo);

                            if (Settings.clipboard && !string.IsNullOrEmpty(tempclipboard))
                                Clipboard.SetText(tempclipboard);
                        });
                        #endregion


                        //}
                        hideRewardInfo = false;
                        partNumber++;
                    }
                }

                numberOfRewardsDisplayed = tempAmountOfRewardsDisplayed;
                Main.listingHelper.PrimeRewards.RemoveAt(Main.listingHelper.PrimeRewards.Count - 1);
                var msg = primeRewards.Aggregate("", (current, item) => current + $"{item}, ");

                Main.AddLog($"Replacing the last entry as slow processing found another rewards: {msg} to list");
                Main.listingHelper.PrimeRewards.Add(primeRewards);

                if (Settings.Highlight)
                {
                    Main.RunOnUIThread(() =>
                    {
                        foreach (var overlay in Main.overlays)
                        {
                            overlay.Clear();
                        }
                        foreach (int item in unownedItems)
                        {
                            Main.overlays[item].BestOwnedChoice();
                            Debug.WriteLine($"nr: {item} is unowned");
                        }
                        Main.overlays[bestDucatItem].BestDucatChoice();
                        Main.overlays[bestPlatItem].BestPlatChoice();
                        Debug.WriteLine($"Best ducat: {bestDucatItem}, Best plat: {bestPlatItem}");
                    });
                }
                newFilter.Dispose();
            }
            catch (Exception ex)
            {
                DateTime time = DateTime.UtcNow;
                timestamp = time.ToString("yyyy-MM-dd HH-mm-ssff", Main.culture);
                processingActive = false;
                Main.AddLog($"Couldn't process second check {ex.ToString()}");
                Main.StatusUpdate("Couldn't process second check", 1);
                Main.RunOnUIThread(() =>
                {
                    Main.SpawnErrorPopup(time);
                });
                throw;
            }
        }

        /// <summary>
        /// Processes the theme, parse image to detect the theme in the image. Parse null to detect the theme from the screen.
        /// closeestThresh is used for getting the most "Accuracte" result, anything over 100 is sure to be correct.
        /// </summary>
        /// <param name="closestThresh"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static WFtheme GetThemeWeighted(out double closestThresh, Bitmap image = null)
        {
            int lineHeight = (int)(pixelRewardLineHeight / 2 * screenScaling);
            // int width = image == null ? window.Width * (int)dpiScaling : image.Width;
            // int height = image == null ? window.Height * (int)dpiScaling : image.Height;
            int mostWidth = (int)(pixleRewardWidth * screenScaling);
            // int mostLeft = (width / 2) - (mostWidth / 2);
            // int mostTop = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight) * screenScaling);
            // int mostBot = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight) * screenScaling * 0.5);

            if (image == null)
            {
                // using (image = new Bitmap(mostWidth, mostBot - mostTop))
                //     using (Graphics graphics = Graphics.FromImage(image))
                //         graphics.CopyFromScreen(window.Left + mostLeft, window.Top + mostTop, 0, 0, new Size(image.Width, image.Height));
                image = CaptureScreenshot();
            }



                    double[] weights = new double[15] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
            int minWidth = mostWidth / 4;

            if (image == null || image.Height == 0)
            {
                throw new Exception("Image height was 0");
            }

            for (int y = lineHeight; y < image.Height; y++)
            {
                double perc = (y - lineHeight) / (image.Height - lineHeight);
                int totWidth = (int)(minWidth * perc + minWidth);
                for (int x = 0; x < totWidth; x++)
                {
                    int match = (int)GetClosestTheme(image.GetPixel(x + (mostWidth - totWidth) / 2, y), out int thresh);
                
                    weights[match] += 1 / Math.Pow(thresh + 1, 4);
                }
            }

            double max = 0;
            WFtheme active = WFtheme.UNKNOWN;
            for (int i = 0; i < weights.Length; i++)
            {
                Debug.Write(weights[i].ToString("F2", Main.culture) + " ");
                if (weights[i] > max)
                {
                    max = weights[i];
                    active = (WFtheme)i;
                }
            }
            Main.AddLog("CLOSEST THEME(" + max.ToString("F2", Main.culture) + "): " + active.ToString());
            closestThresh = max;
            return active;
        }
#pragma warning disable IDE0044 // Add readonly modifier
        private static short[,,] GetThemeCache = new short[256, 256, 256];
        private static short[,,] GetThresholdCache = new short[256, 256, 256];
#pragma warning disable IDE0044 // Add readonly modifier

        private static WFtheme GetClosestTheme(Color clr, out int threshold)
        {
            threshold = 999;
            WFtheme minTheme = WFtheme.CORPUS;
            if (GetThemeCache[clr.R, clr.G, clr.B] > 0)
            {
                threshold = GetThresholdCache[clr.R, clr.G, clr.B];
                return (WFtheme)(GetThemeCache[clr.R, clr.G, clr.B] - 1);
            }

            foreach (WFtheme theme in (WFtheme[])Enum.GetValues(typeof(WFtheme)))
            {
                if (theme != WFtheme.UNKNOWN)
                {
                    Color themeColor = ThemePrimary[(int)theme];
                    int tempThresh = ColorDifference(clr, themeColor);
                    if (tempThresh < threshold)
                    {
                        threshold = tempThresh;
                        minTheme = theme;
                    }
                }
            }
            GetThemeCache[clr.R, clr.G, clr.B] = (byte)(minTheme + 1);
            GetThresholdCache[clr.R, clr.G, clr.B] = (byte)threshold;
            return minTheme;
        }

        /// <summary>
        /// Checks if partName is close enough to valid to actually process
        /// </summary>
        /// <param name="partName">Scanned part name</param>
        /// <returns>If part name is close enough to valid to actually process</returns>
        internal static bool PartNameValid (string partName)
        {
            if ((partName.Length < 13 && Settings.locale == "en") || (partName.Replace(" ", "").Length < 6 && Settings.locale == "ko")) // if part name is smaller than "Bo prime handle" skip current part 
                //TODO: Add a min character for other locale here.
                return false;
            return true;
        }

        /// <summary>
        /// Processes the image the user cropped in the selection
        /// </summary>
        /// <param name="snapItImage"></param>
        internal static void ProcessSnapIt(Bitmap snapItImage, Bitmap fullShot, Point snapItOrigin)
        {
            ProcessProfileScreen(snapItImage, fullShot, snapItOrigin);
            return;
            var watch = new Stopwatch();
            watch.Start();
            long start = watch.ElapsedMilliseconds;

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff", Main.culture);
            WFtheme theme = GetThemeWeighted(out _, fullShot);
            snapItImage.Save(Main.AppPath + @"\Debug\SnapItImage " + timestamp + ".png");
            Bitmap snapItImageFiltered = ScaleUpAndFilter(snapItImage, theme);
            snapItImageFiltered.Save(Main.AppPath + @"\Debug\SnapItImageFiltered " + timestamp + ".png");
            long end = watch.ElapsedMilliseconds;
            Main.StatusUpdate("Completed snapit Processing(" + (end - start) + "ms)", 0);
            List<InventoryItem> foundParts = FindAllParts(snapItImageFiltered);
            string csv = string.Empty;
            snapItImageFiltered.Dispose();
            if (!File.Exists(applicationDirectory + @"\export " + DateTime.UtcNow.ToString("yyyy-MM-dd", Main.culture) + ".csv") && Settings.SnapitExport)
                csv += "ItemName,Plat,Ducats,Volume,Vaulted,Owned,partsDetected" + DateTime.UtcNow.ToString("yyyy-MM-dd", Main.culture) + Environment.NewLine;
            for (int i = 0; i < foundParts.Count; i++)
            {
                var part = foundParts[i];
                if (!PartNameValid(part.Name))
                    continue;
                Debug.WriteLine($"Part  {foundParts.IndexOf(part)} out of {foundParts.Count}");
                string name = Main.dataBase.GetPartName(part.Name, out firstProximity[0]);
                part.Name = name;
                foundParts[i] = part;
                JObject job = Main.dataBase.marketData.GetValue(name).ToObject<JObject>();
                string plat = job["plat"].ToObject<string>();
                string ducats = job["ducats"].ToObject<string>();
                string volume = job["volume"].ToObject<string>();
                bool vaulted = Main.dataBase.IsPartVaulted(name);
                string partsOwned = Main.dataBase.PartsOwned(name);
                string partsDetected = ""+part.Count;

                if (Settings.SnapitExport)
                {
                    var owned = string.IsNullOrEmpty(partsOwned) ? "0" : partsOwned;
                    csv += name + "," + plat + "," + ducats + "," + volume + "," + vaulted.ToString(Main.culture) + "," + owned + "," + partsDetected + ", \"\"" + Environment.NewLine;
                }

                int width = (int)(part.Bounding.Width * screenScaling);
                if (width < 120)
                {
                    if (width < 50)
                        continue;
                    width = 120;
                }
                else if (width > 160)
                {
                    width = 160;
                }


                Main.RunOnUIThread(() =>
                {
                    Overlay itemOverlay = new Overlay();
                    itemOverlay.LoadTextData(name, plat, ducats, volume, vaulted, partsOwned, partsDetected, false);
                    itemOverlay.toSnapit();
                    itemOverlay.Resize(width);
                    itemOverlay.Display((int)(window.X + snapItOrigin.X + (part.Bounding.X - width / 8) / dpiScaling), (int)((window.Y + snapItOrigin.Y + part.Bounding.Y - itemOverlay.Height) / dpiScaling), Settings.delay);
                });
            }

            if (Settings.doSnapItCount)
                Main.RunOnUIThread(() =>
                {
                    VerifyCount.ShowVerifyCount(foundParts);
                 });

            if (Main.snapItOverlayWindow.tempImage != null)
                Main.snapItOverlayWindow.tempImage.Dispose();
            end = watch.ElapsedMilliseconds;
            Main.StatusUpdate("Completed snapit Displaying(" + (end - start) + "ms)", 0);
            watch.Stop();
            if (Settings.SnapitExport)
            {
                File.AppendAllText(applicationDirectory + @"\export " + DateTime.UtcNow.ToString("yyyy-MM-dd", Main.culture) + ".csv", csv);
            }
        }

        /// <summary>
        /// Filters out any group of words and addes them all into a single InventoryItem, containing the found words as well as the bounds within they reside.
        /// </summary>
        /// <param name="filteredImage"></param>
        /// <returns>List of found items</returns>
        private static List<InventoryItem> FindAllParts(Bitmap filteredImage)
        {
            Bitmap filteredImageClean = new Bitmap(filteredImage);
            DateTime time = DateTime.UtcNow;
            string timestamp = time.ToString("yyyy-MM-dd HH-mm-ssff", Main.culture);
            List<InventoryItem> foundItems = new List<InventoryItem>();
            int numberTooLarge = 0;
            int numberTooFewCharacters = 0;
            int numberTooLargeButEnoughCharacters = 0;
            var orange = new Pen(Brushes.Orange);
            var red = new SolidBrush(Color.FromArgb(100, 139, 0, 0));
            var green = new SolidBrush(Color.FromArgb(100, 255, 165, 0));
            var greenp = new Pen(green);
            var pinkP = new Pen(Brushes.Pink);
            var font = new Font("Arial", 16);
            using (var page = firstEngine.Process(filteredImageClean, PageSegMode.SparseText))
            {
                using (var iterator = page.GetIterator())
                {

                    iterator.Begin();
                    do
                    {
                        string currentWord = iterator.GetText(PageIteratorLevel.TextLine);
                        iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect tempbounds);
                        Rectangle bounds = new Rectangle(tempbounds.X1, tempbounds.Y1, tempbounds.Width, tempbounds.Height);
                        if (currentWord != null)
                        {
                            currentWord = RE.Replace(currentWord, "").Trim();
                            if (currentWord.Length > 0)
                            { //word is valid start comparing to others
                                int VerticalPad = bounds.Height/2;
                                int HorizontalPad = bounds.Height;
                                var paddedBounds = new Rectangle(bounds.X - HorizontalPad, bounds.Y - VerticalPad, bounds.Width + HorizontalPad * 2, bounds.Height + VerticalPad * 2);
                                //var paddedBounds = new Rectangle(bounds.X - bounds.Height / 3, bounds.Y - bounds.Height / 3, bounds.Width + bounds.Height, bounds.Height + bounds.Height / 2);

                                using (Graphics g = Graphics.FromImage(filteredImage))
                                {
                                    if (paddedBounds.Height > 50 * screenScaling || paddedBounds.Width > 84 * screenScaling)
                                    { //Determen weither or not the box is too large, false positives in OCR can scan items (such as neuroptics, chassis or systems) as a character(s).
                                        if (currentWord.Length > 3)
                                        { // more than 3 characters in a box too large is likely going to be good, pass it but mark as potentially bad
                                            g.DrawRectangle(orange, paddedBounds);
                                            numberTooLargeButEnoughCharacters++;
                                        }
                                        else
                                        {
                                            g.FillRectangle(red, paddedBounds);
                                            numberTooLarge++;
                                            continue;
                                        }
                                    }
                                    else if (currentWord.Length < 2 && Settings.locale == "en")
                                    {
                                        g.FillRectangle(green, paddedBounds);
                                        numberTooFewCharacters++;
                                        continue;
                                    }
                                    else
                                    {
                                        g.DrawRectangle(pinkP, paddedBounds);
                                    }
                                    g.DrawRectangle(greenp, bounds);
                                    g.DrawString(currentWord, font, Brushes.Pink, new Point(paddedBounds.X, paddedBounds.Y));

                                }
                                int i = foundItems.Count - 1;

                                for (; i >= 0; i--)
                                    if (foundItems[i].Bounding.IntersectsWith(paddedBounds))
                                        break;

                                if (i == -1)
                                {
                                    foundItems.Add(new InventoryItem(currentWord, paddedBounds));
                                }
                                else
                                {
                                    int left = Math.Min(foundItems[i].Bounding.Left, paddedBounds.Left);
                                    int top = Math.Min(foundItems[i].Bounding.Top, paddedBounds.Top);
                                    int right = Math.Max(foundItems[i].Bounding.Right, paddedBounds.Right);
                                    int bot = Math.Max(foundItems[i].Bounding.Bottom, paddedBounds.Bottom);

                                    Rectangle intersectingBounds = new Rectangle(left, top, right - left, bot - top);

                                    InventoryItem newItem = new InventoryItem(foundItems[i].Name + " " + currentWord, intersectingBounds);
                                    foundItems.RemoveAt(i);
                                    foundItems.Add(newItem);
                                }

                            }
                        }
                    }
                    while (iterator.Next(PageIteratorLevel.TextLine));
                }
            }

            if ( Settings.doSnapItCount)
            {
                GetItemCounts(filteredImage, filteredImageClean, foundItems, font, Settings.snapItCountThreshold);
            }

            filteredImageClean.Dispose();
            red.Dispose();
            green.Dispose();
            orange.Dispose();
            pinkP.Dispose();
            greenp.Dispose();
            font.Dispose();
            if (numberTooLarge > .3 * foundItems.Count || numberTooFewCharacters > .4 * foundItems.Count)
            {
                Main.AddLog("numberTooLarge: " + numberTooLarge + ", numberTooFewCharacters: " + numberTooFewCharacters + ", numberTooLargeButEnoughCharacters: " + numberTooLargeButEnoughCharacters + ", foundItems.Count: " + foundItems.Count);
                //If there's a too large % of any error make a pop-up. These precentages are arbritary at the moment, a rough index.
                Main.RunOnUIThread(() =>
                {
                    Main.SpawnErrorPopup(time);
                });
            }

            filteredImage.Save(Main.AppPath + @"\Debug\SnapItImageBounds " + timestamp + ".png");
            return foundItems;
        }

        /// <summary>
        /// Gets the item count of an item by estimating what regions should contain item counts. Filters out noise in the image, aggressiveness based on <c>threshold</c>
        /// </summary>
        /// <param name="filteredImage">Image to draw debug markings on</param>
        /// <param name="filteredImageClean">Image to use for scan</param>
        /// <param name="foundItems">Item list, used to deduce grid system. Modified to add Count for items</param>
        /// <param name="threshold">If the amount of adjacent black pixels (including itself) is below this number, it will be converted to white before the number scanning</param>
        /// <param name="font">Font used for debug marking</param>
        /// <returns>Nothing, but if successful <c>foundItems</c> will be modified</returns>
        private static void GetItemCounts(Bitmap filteredImage, Bitmap filteredImageClean, List<InventoryItem> foundItems, Font font, int threshold)
        {
            Pen darkCyan = new Pen(Brushes.DarkCyan);
            Pen red = new Pen(Brushes.Red);
            Pen cyan = new Pen(Brushes.Cyan);
            using (Graphics g = Graphics.FromImage(filteredImage))
            {

                //features of grid system
                List<Rectangle> Columns = new List<Rectangle>();
                List<Rectangle> Rows = new List<Rectangle>();

                //sort for easier processing in loop below
                List<InventoryItem> foundItemsBottom = foundItems.OrderBy(o => o.Bounding.Bottom).ToList();
                //filter out bad parts for more accurate grid
                bool itemRemoved = false;
                for (int i = 0; i < foundItemsBottom.Count; i+=(itemRemoved ? 0 : 1))
                {
                    itemRemoved = false;
                    if (!PartNameValid(foundItemsBottom[i].Name))
                    {
                        foundItemsBottom.RemoveAt(i);
                        itemRemoved = true;
                    }
                }
                List<InventoryItem> foundItemsLeft = foundItemsBottom.OrderBy(o => o.Bounding.Left).ToList();


                for (int i = 0; i < foundItemsBottom.Count; i++)
                {
                    Rectangle currRow = new Rectangle(0, foundItemsBottom[i].Bounding.Y, 10000, foundItemsBottom[i].Bounding.Height);
                    Rectangle currColumn = new Rectangle(foundItemsLeft[i].Bounding.X, 0, foundItemsLeft[i].Bounding.Width, 10000);

                    //find or improve latest ColumnsRight
                    if (Rows.Count == 0 || !currRow.IntersectsWith(Rows.Last()))
                    {
                        Rows.Add(currRow);
                    }
                    else
                    {
                        if (currRow.Bottom < Rows.Last().Bottom)
                        {
                            Rows[Rows.Count - 1] = new Rectangle(0, Rows.Last().Y, 10000, currRow.Bottom - Rows.Last().Top);
                        }
                        if (Rows.Count != 1 && currColumn.Top > Columns.Last().Top)
                        {
                            Rows[Rows.Count - 1] = new Rectangle(0, currRow.Y, 10000, Rows.Last().Bottom - currRow.Top);
                        }
                    }

                    //find or improve latest ColumnsRight
                    if (Columns.Count == 0 || !currColumn.IntersectsWith(Columns.Last()))
                    {
                        Columns.Add(currColumn);
                    }
                    else
                    {
                        if (currColumn.Right < Columns.Last().Right)
                        {
                            Columns[Columns.Count - 1] = new Rectangle(Columns.Last().X, 0, currColumn.Right - Columns.Last().X, 10000);
                        }
                        if (Columns.Count != 1 && currColumn.Left > Columns.Last().Left)
                        {
                            Columns[Columns.Count - 1] = new Rectangle(currColumn.X, 0, Columns.Last().Right - currColumn.X, 10000);
                        }
                    }

                }

                //draw debug markings for grid system
                for (int i = 0; i < Columns.Count; i++)
                {
                    g.DrawLine(darkCyan, Columns[i].Right, 0, Columns[i].Right, 10000);
                    g.DrawLine(darkCyan, Columns[i].X, 0, Columns[i].X, 10000);
                }
                for (int i = 0; i < Rows.Count; i++)
                {
                    g.DrawLine(darkCyan, 0, Rows[i].Bottom, 10000, Rows[i].Bottom);
                }



                //set OCR to numbers only
                firstEngine.SetVariable("tessedit_char_whitelist", "0123456789");

                
                //Process grid system
                for (int i = 0; i < Rows.Count; i++)
                {
                    for (int j = 0; j < Columns.Count; j++)
                    {
                        //edges of current area to scan
                        int Left = (j == 0 ? 0 : (Columns[j - 1].Right + Columns[j].X) / 2);
                        int Top = (i == 0 ? 0 : Rows[i - 1].Bottom);
                        int Width = Math.Min((Columns[j].Right - Left) / 3, filteredImage.Size.Width - Left);
                        int Height = Math.Min((Rows[i].Bottom - Top) / 3, filteredImage.Size.Height - Top);

                        Rectangle cloneRect = new Rectangle(Left, Top, Width, Height);
                        Bitmap cloneBitmap = filteredImageClean.Clone(cloneRect, filteredImageClean.PixelFormat);

                        //filter out parts of item icon
                        Stack<Point> toFilter = new Stack<Point>();
                        //mark edges for checking
                        for (int k = cloneRect.X; k < cloneRect.Right; k++)
                        {
                            for (int l = 0; l <= Settings.snapItEdgeWidth; l++)
                            {
                                toFilter.Push(new Point(k, cloneRect.Bottom - l));
                            }
                        }
                        for (int k = cloneRect.Y; k < cloneRect.Bottom; k++)
                        {
                            for (int l = 0; l <= Settings.snapItEdgeWidth; l++)
                            {
                                toFilter.Push(new Point(cloneRect.Right-l, k));
                            }
                        }
                        int checkRadius = Settings.snapItEdgeRadius;
                        while (toFilter.Count > 0)
                        {
                            Point curr = toFilter.Pop();
                            if (filteredImageClean.GetPixel(curr.X, curr.Y).R < 200)
                            {
                                g.DrawRectangle(red, curr.X, curr.Y, 1, 1);
                                filteredImageClean.SetPixel(curr.X, curr.Y, Color.White);
                                //check neighbours
                                for (int k = Math.Max(curr.X - checkRadius, cloneRect.X); k < Math.Min(curr.X + checkRadius, cloneRect.Right); k++)
                                {
                                    for (int l = Math.Max(curr.Y - checkRadius, cloneRect.Y); l < Math.Min(curr.Y + checkRadius, cloneRect.Bottom); l++)
                                    {
                                        toFilter.Push(new Point(k, l));
                                    }
                                }
                            }
                        }

                        //filter out noise
                        for (int k = 1; k < cloneRect.Width - 1; k++)
                        {
                            for (int l = 1; l < cloneRect.Height - 1; l++)
                            {
                                if (cloneBitmap.GetPixel(k, l).R < 200)
                                {
                                    int BlackCount = 0;
                                    for (int m = -1; m <= 1; m++)
                                    {
                                        for (int n = -1; n <= 1; n++)
                                        {
                                            if (cloneBitmap.GetPixel(k + m, l + n).R < 200)
                                            {
                                                BlackCount++;
                                            }
                                        }
                                    }
                                    if (BlackCount < threshold)
                                    {
                                        g.DrawRectangle(red, k + cloneRect.X, l + cloneRect.Y, 1, 1);
                                        filteredImageClean.SetPixel(k + cloneRect.X, l + cloneRect.Y, Color.White);
                                    }
                                }
                            }
                        }

                        cloneBitmap = filteredImageClean.Clone(cloneRect, filteredImageClean.PixelFormat);
                        g.DrawRectangle(cyan, cloneRect);

                        //do OCR
                        using (var page = firstEngine.Process(cloneBitmap, PageSegMode.SingleLine))
                        {
                            using (var iterator = page.GetIterator())
                            {
                                iterator.Begin();
                                string rawText = iterator.GetText(PageIteratorLevel.TextLine);
                                if (rawText != null) 
                                    rawText = rawText.Replace(" ", "");
                                //if no number found, 1 of item
                                if (!Int32.TryParse(rawText, out int itemCount))
                                {
                                    itemCount = 1;
                                }
                                g.DrawString(rawText, font, Brushes.Cyan, new Point(cloneRect.X, cloneRect.Y));

                                //find what item the item belongs to
                                Rectangle itemLabel = new Rectangle( Columns[j].X, Rows[i].Top, Columns[j].Width , Rows[i].Height);
                                g.DrawRectangle(cyan, itemLabel);
                                for (int k = 0; k < foundItems.Count; k++)
                                {
                                    var item = foundItems[k];
                                    if (item.Bounding.IntersectsWith(itemLabel))
                                    {
                                        item.Count = itemCount;
                                        foundItems[k] = item;
                                    }
                                }
                            }

                        }
                        cloneBitmap.Dispose();
                    }
                }
                
                //return OCR to any symbols
                firstEngine.SetVariable("tessedit_char_whitelist", "");
            }
            darkCyan.Dispose();
            red.Dispose();
            cyan.Dispose();
        }

        /// <summary>
        /// Processes the image the user cropped in the selection
        /// </summary>
        /// <param name="snapItImage"></param>
        internal static void ProcessProfileScreen(Bitmap snapItImage, Bitmap fullShot, Point snapItOrigin)
        {
            var watch = new Stopwatch();
            watch.Start();
            long start = watch.ElapsedMilliseconds;

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff", Main.culture);
            snapItImage.Save(Main.AppPath + @"\Debug\ProfileImage " + timestamp + ".png");
            List<InventoryItem> foundParts = FindOwnedItems(snapItImage, timestamp);
            for (int i = 0; i < foundParts.Count; i++)
            {
                var part = foundParts[i];
                if (!PartNameValid(part.Name + " Blueprint"))
                    continue;
                string name = Main.dataBase.GetPartName(part.Name+" Blueprint", out int proximity);
                part.Name = name;
                foundParts[i] = part;

                //Decide if item is an actual prime, if so mark as mastered
                if (proximity < 3 && name.Contains("Prime"))
                {
                    //mark as mastered
                    string[] nameParts = part.Name.Split(new string[] { "Prime" }, 2, StringSplitOptions.None);
                    string primeName = nameParts[0] + "Prime";

                    if (Main.dataBase.equipmentData[primeName].ToObject<JObject>().TryGetValue("mastered", out _))
                    {
                        Main.dataBase.equipmentData[primeName]["mastered"] = true;

                        Main.AddLog("Marked \"" + primeName + "\" as mastered");
                    } else
                    {
                        Main.AddLog("Failed to mark \"" + primeName + "\" as mastered");
                    }
                }
            }
            Main.dataBase.SaveAllJSONs();

            long end = watch.ElapsedMilliseconds;
            Main.StatusUpdate("Completed Profile Scanning(" + (end - start) + "ms)", 0);
            watch.Stop();

        }

        private static bool probeProfilePixel(Color pixel)
        {
            return pixel.A > 240 && pixel.R > 200 && pixel.G > 200 && pixel.B > 200;
        }

        private static List<InventoryItem> FindOwnedItems(Bitmap ProfileImage, string timestamp)
        {
            //find edges of owned item label Colour: A > 250, R > 230, G > 230, B > 230
            //check that there are 2 rows of text
            //OCR the first row
            Pen orange = new Pen(Brushes.Orange);
            Pen red = new Pen(Brushes.Red);
            Pen cyan = new Pen(Brushes.Cyan);
            Pen pink = new Pen(Brushes.Pink);
            Pen darkCyan = new Pen(Brushes.DarkCyan);
            var font = new Font("Arial", 16);
            List<InventoryItem> foundItems = new List<InventoryItem>();
            Bitmap ProfileImageClean = new Bitmap(ProfileImage);
            int probe_interval = 10;
            using (Graphics g = Graphics.FromImage(ProfileImage))
            {
                int nextY = 0;
                int nextYCounter = -1;
                for (int y = 0; y < ProfileImageClean.Height; y = (nextYCounter-- == 0 ? nextY : y+1 ))
                {
                    for (int x = 0; x < ProfileImageClean.Width; x+= probe_interval) //probe every few pixels for performance
                    {
                        Color pixel = ProfileImageClean.GetPixel(x, y);
                        if (probeProfilePixel(pixel) )
                        {
                            //find left edge and check that the coloured area is at least as big as probe_interval
                            int leftEdge = -1;
                            int hits = 0;
                            int areaWidth = 0;
                            double hitRatio = 0;
                            for (int tempX = Math.Max(x - probe_interval, 0); tempX < Math.Min(x + probe_interval, ProfileImageClean.Width ) ; tempX++)
                            {
                                areaWidth++;
                                if ( probeProfilePixel( ProfileImageClean.GetPixel(tempX, y)))
                                {
                                    hits++;
                                    leftEdge = (leftEdge == -1 ? tempX : leftEdge);
                                }
                            }
                            hitRatio = (double)(hits) / areaWidth;
                            if ( hitRatio < 0.5) //skip if too low hit ratio
                            {
                                g.DrawLine(orange, x - probe_interval, y, x + probe_interval, y);
                                continue;
                            }

                            //find where the line ends
                            int rightEdge = leftEdge;
                            while (rightEdge+2 < ProfileImageClean.Width && ( probeProfilePixel(ProfileImageClean.GetPixel(rightEdge+1, y)) || probeProfilePixel(ProfileImageClean.GetPixel(rightEdge + 2, y))))
                            {
                                rightEdge++;
                            }

                            
                            //check hit ratio for line above and skip if too high
                            hits = 0;
                            for (int i = leftEdge; i <= rightEdge; i++)
                            {
                                if ( probeProfilePixel(ProfileImageClean.GetPixel(i, Math.Max(y - 1, 0))))
                                {
                                    hits++;
                                }
                            }
                            hitRatio = hits / (double)(rightEdge - leftEdge);
                            if ( (rightEdge - leftEdge) < 100 || hitRatio > 0.9)
                            {
                                g.DrawLine(darkCyan, x - probe_interval, y, x + probe_interval, y);
                                g.DrawLine(darkCyan, leftEdge, y, rightEdge, y);
                                continue;
                            }
                            

                            //find bottom edge and hit ratio of all rows
                            int topEdge = y;
                            int bottomEdge = y;
                            List<double> hitRatios = new List<double>();
                            hitRatios.Add(1);
                            do
                            {
                                hits = 0;
                                bottomEdge++;
                                for (int i = leftEdge; i < rightEdge; i++)
                                {
                                    if (probeProfilePixel(ProfileImageClean.GetPixel(i, bottomEdge)))
                                    {
                                        hits++;
                                    }
                                }
                                hitRatio = hits / (double)(rightEdge - leftEdge );
                                hitRatios.Add(hitRatio);
                            } while (bottomEdge+2 < ProfileImageClean.Height && hitRatios.Last() > 0.5);
                            hitRatios.RemoveAt(hitRatios.Count - 1);
                            //find if/where it transitions from text (some misses) to no text (basically no misses) then back to text (some misses). This is proof it's an owned item and marks the bottom edge of the text
                            int ratioChanges = 0;
                            bool prevMostlyHits = true;
                            int lineBreak = -1;
                            for (int i = 0; i < hitRatios.Count; i++)
                            {
                                if ( (hitRatios[i] > 0.99) != prevMostlyHits)
                                {
                                    if (ratioChanges == 1)
                                    {
                                        lineBreak = i+1;
                                        g.DrawLine(cyan, rightEdge, topEdge+lineBreak, leftEdge, topEdge + lineBreak);
                                    }
                                    prevMostlyHits = !prevMostlyHits;
                                    ratioChanges++;
                                }
                            }

                            int width = rightEdge - leftEdge;
                            int height = bottomEdge - topEdge;

                            if (ratioChanges != 4 || width < 4 * height || width > 6 * height)
                            {
                                g.DrawRectangle(pink, leftEdge, topEdge, width, height);
                                continue;
                            }

                            g.DrawRectangle(red, leftEdge, topEdge, width, height);
                            x = rightEdge;
                            nextY = bottomEdge + 1;
                            nextYCounter = 3;

                            height = lineBreak;

                            Rectangle cloneRect = new Rectangle(leftEdge, topEdge, width, height);
                            Bitmap cloneBitmap = ProfileImageClean.Clone(cloneRect, ProfileImageClean.PixelFormat);
                            for (int i = 0; i < cloneBitmap.Width; i++)
                            {
                                for (int j = 0; j < cloneBitmap.Height; j++)
                                {
                                    if (probeProfilePixel(cloneBitmap.GetPixel(i, j)))
                                    {
                                        cloneBitmap.SetPixel(i, j, Color.White);
                                    } else
                                    {
                                        cloneBitmap.SetPixel(i, j, Color.Black);
                                        ProfileImage.SetPixel(cloneRect.X + i , cloneRect.Y + j , Color.Red);
                                    }
                                }
                            }
                            //do OCR
                            firstEngine.SetVariable("tessedit_char_whitelist", " ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                            using (var page = firstEngine.Process(cloneBitmap, PageSegMode.SingleLine))
                            {
                                using (var iterator = page.GetIterator())
                                {
                                    iterator.Begin();
                                    string rawText = iterator.GetText(PageIteratorLevel.TextLine);

                                    foundItems.Add(new InventoryItem(rawText, cloneRect));

                                    g.DrawString(rawText, font, Brushes.DarkBlue, new Point(cloneRect.X, cloneRect.Y));


                                }
                            }
                            firstEngine.SetVariable("tessedit_char_whitelist", "");
                        }
                    }
                }
            }

            ProfileImageClean.Dispose();
            ProfileImage.Save(Main.AppPath + @"\Debug\ProfileImageBounds " + timestamp + ".png");
            darkCyan.Dispose();
            pink.Dispose();
            cyan.Dispose();
            red.Dispose();
            orange.Dispose();
            return foundItems;
        }

            private static int ColorDifference(Color test, Color thresh)
        {
            return Math.Abs(test.R - thresh.R) + Math.Abs(test.G - thresh.G) + Math.Abs(test.B - thresh.B);
        }

        public static bool ThemeThresholdFilter(Color test, WFtheme theme)
        {
            Color primary = ThemePrimary[(int)theme];
            Color secondary = ThemeSecondary[(int)theme];

            switch (theme)
            {
                case WFtheme.VITRUVIAN:     // TO CHECK
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 4 && test.GetSaturation() >= 0.25 && test.GetBrightness() >= 0.42;
                case WFtheme.LOTUS:
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 5 && test.GetSaturation() >= 0.65 && Math.Abs(test.GetBrightness() - primary.GetBrightness()) <= 0.1
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 15 && test.GetBrightness() >= 0.65);
                case WFtheme.OROKIN:        // TO CHECK
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 5 && test.GetBrightness() <= 0.42 && test.GetSaturation() >= 0.1)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 5 && test.GetBrightness() <= 0.5 && test.GetBrightness() >= 0.25 && test.GetSaturation() >= 0.25);
                case WFtheme.STALKER:
                    return ((Math.Abs(test.GetHue() - primary.GetHue()) < 4 && test.GetSaturation() >= 0.55)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetSaturation() >= 0.66)) && test.GetBrightness() >= 0.25;
                case WFtheme.CORPUS:
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 3 && test.GetBrightness() >= 0.42 && test.GetSaturation() >= 0.35;
                case WFtheme.EQUINOX:
                    return test.GetSaturation() <= 0.2 && test.GetBrightness() >= 0.55;
                case WFtheme.DARK_LOTUS:
                    return (Math.Abs(test.GetHue() - secondary.GetHue()) < 20 && test.GetBrightness() >= 0.35 && test.GetBrightness() <= 0.55 && test.GetSaturation() <= 0.25 && test.GetSaturation() >= 0.05)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetBrightness() >= 0.50 && test.GetSaturation() >= 0.20);
                case WFtheme.FORTUNA:
                    return ((Math.Abs(test.GetHue() - primary.GetHue()) < 3 && test.GetBrightness() >= 0.35) || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetBrightness() >= 0.15)) && test.GetSaturation() >= 0.20;
                case WFtheme.HIGH_CONTRAST:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 3 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2) && test.GetSaturation() >= 0.75 && test.GetBrightness() >= 0.35; // || Math.Abs(test.GetHue() - secondary.GetHue()) < 2;
                case WFtheme.LEGACY:    // TO CHECK
                    return (test.GetBrightness() >= 0.65)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 6 && test.GetBrightness() >= 0.5 && test.GetSaturation() >= 0.5);
                case WFtheme.NIDUS:
                    return (Math.Abs(test.GetHue() - (primary.GetHue() + 6)) < 8 && test.GetSaturation() >= 0.30)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 15 && test.GetSaturation() >= 0.55);
                case WFtheme.TENNO:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 3 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2) && test.GetSaturation() >= 0.38 && test.GetBrightness() <= 0.55;
                case WFtheme.BARUUK:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 2) && test.GetSaturation() > 0.25 && test.GetBrightness() > 0.5;
                case WFtheme.GRINEER:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 5 && test.GetBrightness() > 0.5)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 6 && test.GetBrightness() > 0.55);
                case WFtheme.ZEPHYR:
                return ((Math.Abs(test.GetHue() - primary.GetHue()) < 4 && test.GetSaturation() >= 0.55)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetSaturation() >= 0.66)) && test.GetBrightness() >= 0.25;
                default:
                    // This shouldn't be ran
                    //   Only for initial testing
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 2 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2;
            }
        }

        private static Bitmap ScaleUpAndFilter(Bitmap image, WFtheme active)
        {
            Bitmap filtered;
            if (image.Height <= SCALING_LIMIT)
            {
                partialScreenshotExpanded = new Bitmap(image.Width * SCALING_LIMIT / image.Height, SCALING_LIMIT);
                partialScreenshotExpanded.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (Graphics graphics = Graphics.FromImage(partialScreenshotExpanded))
                {
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    graphics.DrawImage(image, 0, 0, partialScreenshotExpanded.Width, partialScreenshotExpanded.Height);
                }

                filtered = new Bitmap(partialScreenshotExpanded.Width, partialScreenshotExpanded.Height);
            }
            else
            {
                partialScreenshotExpanded = image;
                filtered = image;
            }
            Color clr;
            for (int x = 0; x < filtered.Width; x++)
            {
                for (int y = 0; y < filtered.Height; y++)
                {
                    clr = partialScreenshotExpanded.GetPixel(x, y);
                    if (ThemeThresholdFilter(clr, active))
                        filtered.SetPixel(x, y, Color.Black);
                    else
                        filtered.SetPixel(x, y, Color.White);
                }
            }
            return filtered;
        }

        // The parts of text
        // The top bit (upper case and dots/strings, bdfhijklt) > the juicy bit (lower case, acemnorsuvwxz) > the tails (gjpqy)
        // we ignore the "tippy top" because it has a lot of variance, so we just look at the "bottom half of the top"
        private static readonly int[] TextSegments = new int[] { 2, 4, 16, 21 };
        private static List<Bitmap> ExtractPartBoxAutomatically(out double scaling, out WFtheme active, Bitmap fullScreen)
        {
            var watch = new Stopwatch();
            watch.Start();
            long start = watch.ElapsedMilliseconds;
            long beginning = start;

            int lineHeight = (int)(pixelRewardLineHeight / 2 * screenScaling);

            Color clr;
            int width = window.Width;
            int height = window.Height;
            int mostWidth = (int)(pixleRewardWidth * screenScaling);
            int mostLeft = (width / 2) - (mostWidth / 2 );
            // Most Top = pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight
            //                   (316          -        235        +       44)    *    1.1    =    137
            int mostTop = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight) * screenScaling);
            int mostBot = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight) * screenScaling * 0.5);
            //Bitmap postFilter = new Bitmap(mostWidth, mostBot - mostTop);
            var rectangle = new Rectangle((int)(mostLeft), (int)(mostTop), mostWidth, mostBot - mostTop);
            Bitmap preFilter;

            try
            {
                Main.AddLog($"Fullscreen is {fullScreen.Size}:, trying to clone: {rectangle.Size} at {rectangle.Location}");
                preFilter = fullScreen.Clone(new Rectangle(mostLeft, mostTop, mostWidth, mostBot - mostTop), fullScreen.PixelFormat);
            }
            catch (Exception ex)
            {
                Main.AddLog("Something went wrong with getting the starting image: " + ex.ToString());
                throw;
            }


            long end = watch.ElapsedMilliseconds;
            Main.AddLog("Grabbed images " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;
            
            active = GetThemeWeighted(out var closest, fullScreen);
            Main.AddLog("CLOSEST THEME(" + closest.ToString("F2", Main.culture) + "): " + active);

            end = watch.ElapsedMilliseconds;
            Main.AddLog("Got theme " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;

            int[] rows = new int[preFilter.Height];
            // 0 => 50   27 => 77   50 => 100


            //Main.AddLog("ROWS: 0 to " + preFilter.Height);
            //var postFilter = preFilter;
            for (int y = 0; y < preFilter.Height; y++)
            {
                rows[y] = 0;
                for (int x = 0; x < preFilter.Width; x++)
                {
                    clr = preFilter.GetPixel(x, y);
                    if (ThemeThresholdFilter(clr, active))
                    //{
                        rows[y]++;
                        //postFilter.SetPixel(x, y, Color.Black);
                    //} else
                        //postFilter.SetPixel(x, y, Color.White);
                }
                //Debug.Write(rows[y] + " ");
            }

            //postFilter.Save(Main.AppPath + @"\Debug\PostFilter" + timestamp + ".png");

            end = watch.ElapsedMilliseconds;
            Main.AddLog("Filtered Image " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;

            double[] percWeights = new double[51];
            double[] topWeights = new double[51];
            double[] midWeights = new double[51];
            double[] botWeights = new double[51];

            int topLine_100 = preFilter.Height - lineHeight;
            int topLine_50 = lineHeight / 2;

            scaling = -1;
            double lowestWeight = 0;
            Rectangle uidebug = new Rectangle((topLine_100 - topLine_50) / 50 + topLine_50, (int)(preFilter.Height/screenScaling), preFilter.Width, 50);
            for (int i = 0; i <= 50; i++)
            {
                int yFromTop = preFilter.Height - (i * (topLine_100 - topLine_50) / 50 + topLine_50);

                int scale = (50 + i);
                int scaleWidth = preFilter.Width * scale / 100;

                int textTop = (int)(screenScaling * TextSegments[0] * scale / 100);
                int textTopBot = (int)(screenScaling * TextSegments[1] * scale / 100);
                int textBothBot = (int)(screenScaling * TextSegments[2] * scale / 100);
                int textTailBot = (int)(screenScaling * TextSegments[3] * scale / 100);

                int loc = textTop;
                for (; loc <= textTopBot; loc++)
                    topWeights[i] += Math.Abs(scaleWidth * 0.06 - rows[yFromTop + loc]);

                loc++;
                for (; loc < textBothBot; loc++)
                {
                    if (rows[yFromTop + loc] < scaleWidth / 15)
                        midWeights[i] += (scaleWidth * 0.26 - rows[yFromTop + loc]) * 5;
                    else
                        midWeights[i] += Math.Abs(scaleWidth * 0.24 - rows[yFromTop + loc]);
                }

                loc++;
                for (; loc < textTailBot; loc++)
                    botWeights[i] += 10 * Math.Abs(scaleWidth * 0.007 - rows[yFromTop + loc]);

                topWeights[i] /= textTopBot - textTop + 1;
                midWeights[i] /= textBothBot - textTopBot - 2;
                botWeights[i] /= textTailBot - textBothBot - 1;
                percWeights[i] = topWeights[i] + midWeights[i] + botWeights[i];

                if (scaling == -1 || lowestWeight > percWeights[i])
                {
                    scaling = scale;
                    lowestWeight = percWeights[i];
                }
            }

            end = watch.ElapsedMilliseconds;

            Main.AddLog("Got scaling " + (end - start) + "ms");

            int[] topFive = new int[] { -1, -1, -1, -1, -1 };

            for (int i = 0; i <= 50; i++)
            {
                int match = 4;
                while (match != -1 && topFive[match] != -1 && percWeights[i] > percWeights[topFive[match]])
                    match--;

                if (match != -1)
                {
                    for (int move = 0; move < match; move++)
                        topFive[move] = topFive[move + 1];
                    topFive[match] = i;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                Main.AddLog("RANK " + (5 - i) + " SCALE: " + (topFive[i] + 50) + "%\t\t" + percWeights[topFive[i]].ToString("F2", Main.culture) + " -- " + topWeights[topFive[i]].ToString("F2", Main.culture) + ", " + midWeights[topFive[i]].ToString("F2", Main.culture) + ", " + botWeights[topFive[i]].ToString("F2", Main.culture));
            }

            using (Graphics g = Graphics.FromImage(fullScreen))
            {
                g.DrawRectangle(Pens.Red, rectangle);
                g.DrawRectangle(Pens.Chartreuse, uidebug);
            }
            fullScreen.Save(Main.AppPath + @"\Debug\BorderScreenshot " + timestamp + ".png");


            //postFilter.Save(Main.appPath + @"\Debug\DebugBox1 " + timestamp + ".png");
            preFilter.Save(Main.AppPath + @"\Debug\FullPartArea " + timestamp + ".png");
            scaling = topFive[4] + 50; //scaling was sometimes going to 50 despite being set to 100, so taking the value from above that seems to be accurate.

            scaling /= 100;
            double highScaling = scaling < 1.0 ? scaling + 0.01 : scaling;
            double lowScaling = scaling > 0.5 ? scaling - 0.01 : scaling;

            int cropWidth = (int)(pixleRewardWidth * screenScaling * highScaling);
            int cropLeft = (preFilter.Width / 2) - (cropWidth / 2);
            int cropTop = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight) * screenScaling * highScaling);
            int cropBot = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight) * screenScaling * lowScaling);
            int cropHei = cropBot - cropTop;
            cropTop -= mostTop;
            try
            {
                Rectangle rect = new Rectangle(cropLeft, cropTop, cropWidth, cropHei);
                partialScreenshot = preFilter.Clone(rect, System.Drawing.Imaging.PixelFormat.DontCare);
                if (partialScreenshot.Height == 0 || partialScreenshot.Width == 0)
                    throw new ArithmeticException("New image was null");
            }
            catch (Exception ex)
            {
                Main.AddLog("Something went wrong while trying to copy the right part of the screen into partial screenshot: " + ex.ToString());
                throw;
            }

            preFilter.Dispose();

            end = watch.ElapsedMilliseconds;
            Main.AddLog("Finished function " + (end - beginning) + "ms");
            partialScreenshot.Save(Main.AppPath + @"\Debug\PartialScreenshot" + timestamp + ".png");
            return FilterAndSeparatePartsFromPartBox(partialScreenshot, active);
        }

        private static List<Bitmap> FilterAndSeparatePartsFromPartBox(Bitmap partBox, WFtheme active)
        {
            Color clr;
            double weight = 0;
            double totalEven = 0;
            double totalOdd = 0;

            Bitmap filtered = new Bitmap(partBox.Width, partBox.Height);
            for (int x = 0; x < filtered.Width; x++)
            {
                int count = 0;
                for (int y = 0; y < filtered.Height; y++)
                {
                    clr = partBox.GetPixel(x, y);
                    if (ThemeThresholdFilter(clr, active))
                    {
                        filtered.SetPixel(x, y, Color.Black);
                        count++;
                    }
                    else
                        filtered.SetPixel(x, y, Color.White);
                }

                count = Math.Min(count, partBox.Height / 3);
                double sinVal = Math.Cos(8 * x * Math.PI / partBox.Width);
                sinVal = sinVal * sinVal * sinVal;
                weight += sinVal * count;

                if (sinVal < 0)
                    totalEven -= sinVal * count;
                else if (sinVal > 0)
                    totalOdd += sinVal * count;
            }

            if (totalEven == 0 || totalOdd == 0)
            {
                Main.RunOnUIThread(() =>
                {
                    Main.StatusUpdate("Filter and separate failed, report to dev", 1);
                });
                processingActive = false;
                throw new Exception("Unable to find any parts");
            }

            double total = totalEven + totalOdd;
            Main.AddLog("EVEN DISTRIBUTION: " + (totalEven / total * 100).ToString("F2", Main.culture) + "%");
            Main.AddLog("ODD DISTRIBUTION: " + (totalOdd / total * 100).ToString("F2", Main.culture) + "%");

            int boxWidth = partBox.Width / 4;
            int boxHeight = filtered.Height;
            Rectangle destRegion = new Rectangle(0, 0, boxWidth, boxHeight);

            int currLeft = 0;
            int playerCount = 4;

            if (totalOdd > totalEven)
            {
                currLeft = boxWidth / 2;
                playerCount = 3;
            }

            List<Bitmap> ret = new List<Bitmap>(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                Rectangle srcRegion = new Rectangle(currLeft + i * boxWidth, 0, boxWidth, boxHeight);
                Bitmap newBox = new Bitmap(boxWidth, boxHeight);
                using (Graphics grD = Graphics.FromImage(newBox))
                    grD.DrawImage(filtered, destRegion, srcRegion, GraphicsUnit.Pixel);
                ret.Add(newBox);
                newBox.Save(Main.AppPath + @"\Debug\PartBox(" + i + ") " + timestamp + ".png");
            }
            filtered.Dispose();
            return ret;
        }

        //private static List<Bitmap> FilterAndSeparateParts(Bitmap image, WFtheme active)
        //{
        //    int width = (int)(pixleRewardWidth * screenScaling * uiScaling);
        //    int lineHeight = (int)(pixelRewardLineHeight * screenScaling * uiScaling);
        //    int left = (image.Width / 2) - (width / 2);
        //    int top = (image.Height / 2) - (int)(pixleRewardYDisplay * screenScaling * uiScaling) + (int)(pixleRewardHeight * screenScaling * uiScaling) - lineHeight;

        //    partialScreenshot = new Bitmap(width, lineHeight);

        //    Color clr;
        //    for (int x = 0; x < width; x++)
        //    {
        //        for (int y = 0; y < partialScreenshot.Height; y++)
        //        {
        //            clr = image.GetPixel(left + x, top + y);
        //            partialScreenshot.SetPixel(x, y, clr);
        //        }
        //    }
        //    return FilterAndSeparatePartsFromPartBox(partialScreenshot, active);
        //}

        public static string GetTextFromImage(Bitmap image, TesseractEngine engine)
        {
            string ret = "";
            using (Page page = engine.Process(image))
                ret = page.GetText().Trim();
            return RE.Replace(ret, "").Trim();
        }

        internal static List<string> SeparatePlayers(Bitmap image, TesseractEngine engine)
        {

            // Values to determine whether there's an even or odd number of players
            int wid = image.Width / 4;
            int subwid = wid / 2;
            int subsubwid = subwid / 4;

            // 3 player values
            //  left  mid  right
            int mid = image.Width / 2;
            int left = mid - wid;
            int right = mid + wid;

            // list of centers for each potential set of text
            //   alternates between even locations and odd locations
            int[] allLocs = new int[] { left - subwid, left, left + subwid, mid, right - subwid, right, right + subwid };

            // Point system to determine if the player count is even or odd
            //    At the end of the calc, whichever has more wins
            int oddCount = 0;
            int evenCount = 0;

            // 2d array - words with bounds (1 dimensional)
            /*   [
             *     [start, end, word_ind, word_ind, ...] -- horizontal start and end position of this part and the list of words with it
             *     ...
             *   ]
            */
            List<List<int>> arr2D = new List<List<int>>();
            List<string> words = new List<string>();
            using (Page page = engine.Process(image))
            {
                using (var iter = page.GetIterator())
                {
                    iter.Begin();
                    do
                    {
                        iter.TryGetBoundingBox(PageIteratorLevel.Word, out Rect outRect);
                        string word = iter.GetText(PageIteratorLevel.Word);
                        if (word != null)
                        {
                            word = RE.Replace(word, "").Trim();
                            if (word.Length > 0)
                            {
                                int topOrBot = outRect.Y1 > (outRect.Height * 3 / 4) ? 0 : 1;
                                for (int i = 0; i < allLocs.Length; i++)
                                {
                                    int bot = allLocs[i] - subsubwid;
                                    int top = allLocs[i] + subsubwid;
                                    if (bot <= outRect.X2 && top >= outRect.X1)
                                    {
                                        if ((i & 1) == 0)
                                            evenCount++;
                                        else
                                            oddCount++;
                                        break;
                                    }
                                }
                                List<int> temp = new List<int> {
                                    outRect.X1,
                                    outRect.X2,
                                    words.Count,
                                    topOrBot
                                };
                                arr2D.Add(temp);
                                words.Add(word);
                            }
                        }

                        // Giant blob of shit
                        // Translates to:
                        //   keep going while there's words left in the line
                        //           or while there's lines left in the para
                        //           or while there's paras left in the block
                        //           or while there's blocks left 
                    } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word) || iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine) || iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para) || iter.Next(PageIteratorLevel.Block));
                }
            }
            arr2D.Sort(new Arr2D_Compare());

            List<string> ret = new List<string>();

            // time to group text together
            // get left side of part text area
            //    and width of text area
            //    group all text within
            //    go to the next one

            // position the left correctly based on part boxes
            if (oddCount > evenCount)
                left -= subwid;
            else
                left -= wid;

            string currPartTop = "";
            string currPartBot = "";

            int ind = 0;
            for (; ind < arr2D.Count && arr2D[ind][0] < left; ind++) ;


            for (; ind < arr2D.Count; ind++)
            {
                if (arr2D[ind][0] >= left + wid)
                {
                    left += wid;
                    ret.Add((currPartTop.Trim() + " " + currPartBot.Trim()).Trim());
                    currPartTop = "";
                    currPartBot = "";
                }
                if (arr2D[ind][3] == 1)
                    currPartTop += words[arr2D[ind][2]] + " ";
                else
                    currPartBot += words[arr2D[ind][2]] + " ";
            }
            ret.Add((currPartTop.Trim() + " " + currPartBot.Trim()).Trim());


            Main.AddLog((ret[0].Length == 0 ? ret.Count - 1 : ret.Count) + " Players Found -- Odds (" + oddCount + ") vs Evens (" + evenCount + ")");

            if ((oddCount == 0 && evenCount == 0) || oddCount == evenCount)
            { //Detected 0 rewards
                return null;
            }

            // Remove any empty items from the array
            return ret.Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        private class Arr2D_Compare : IComparer<List<int>>
        {
            public int Compare(List<int> x, List<int> y)
            {
                return x[0].CompareTo(y[0]);
            }
        }

        internal static Bitmap CaptureScreenshot()
        {
            UpdateWindow();

            int width = window.Width;
            int height = window.Height;

            if (window == null || window.Width == 0 || window.Height == 0)
            {
                window = Screen.PrimaryScreen.Bounds;
                center = new Point(window.X + window.Width / 2, window.Y + window.Height / 2);

                width *= (int)dpiScaling;
                height *= (int)dpiScaling;
            }


            Bitmap image = new Bitmap(width, height);
            Size FullscreenSize = new Size(image.Width, image.Height);
            using (Graphics graphics = Graphics.FromImage(image))
                graphics.CopyFromScreen(window.Left, window.Top, 0, 0, FullscreenSize, CopyPixelOperation.SourceCopy);
            image.Save(Main.AppPath + @"\Debug\FullScreenShot " + timestamp + ".png");
            return image;
        }

        internal static void SnapScreenshot()
        {
            Main.snapItOverlayWindow.Populate(CaptureScreenshot());
            Main.snapItOverlayWindow.Show();
            Main.snapItOverlayWindow.Left = window.Left;
            Main.snapItOverlayWindow.Top = window.Top;
            Main.snapItOverlayWindow.Width = window.Width;
            Main.snapItOverlayWindow.Height = window.Height;
            Main.snapItOverlayWindow.Topmost = true;
            Main.snapItOverlayWindow.Focusable = true;
            Main.snapItOverlayWindow.Focus();
        }

        public static async Task updateEngineAsync()
        {
            getLocaleTessdata();
            Init();
            firstEngine.Dispose();
            firstEngine = new TesseractEngine(applicationDirectory + @"\tessdata", Settings.locale)
            {
                DefaultPageSegMode = PageSegMode.SingleBlock
            };
            secondEngine.Dispose();
            secondEngine = new TesseractEngine(applicationDirectory + @"\tessdata", Settings.locale)
            {
                DefaultPageSegMode = PageSegMode.SingleBlock
            };
        }

        public static bool VerifyWarframe()
        {
    
            if (Warframe != null && !Warframe.HasExited)
            { // don't update status
                return true;
            }
            Task.Run(() =>
            {
            foreach (Process process in Process.GetProcesses())
                if (process.ProcessName == "Warframe.x64")
                {
                    if (process.MainWindowTitle == "Warframe")
                    {
                        HandleRef = new HandleRef(process, process.MainWindowHandle);
                        Warframe = process;
                        if (Main.dataBase.GetSocketAliveStatus())
                            Debug.WriteLine("Socket was open in verify warframe");
                        Task.Run(async () =>
                        {
                            await Main.dataBase.SetWebsocketStatus("in game");
                        });
                        Main.AddLog("Found Warframe Process: ID - " + process.Id + ", MainTitle - " + process.MainWindowTitle + ", Process Name - " + process.ProcessName);
                        return true;
                    }
                }
                if (!Settings.debug)
                {
                    Main.AddLog("Did Not Detect Warframe Process");
                    Main.StatusUpdate("Unable to Detect Warframe Process", 1);
                }
                return false;
            });
            return false;
        }

        private static void RefreshDPIScaling()
        {
            try
            {
                var mon = Win32.MonitorFromPoint(new Point(Screen.PrimaryScreen.Bounds.Left+1, Screen.PrimaryScreen.Bounds.Top+1), 2);
                Win32.GetDpiForMonitor(mon, Win32.DpiType.Effective, out var dpiXEffective, out _);
                //Win32.GetDpiForMonitor(mon, Win32.DpiType.Angular, out var dpiXAngular, out _);
                //Win32.GetDpiForMonitor(mon, Win32.DpiType.Raw, out var dpiXRaw, out _);

                Main.AddLog($"Effective dpi, X:{dpiXEffective}\n Which is %: {dpiXEffective / 96.0}");
                //Main.AddLog($"Raw dpi, X:{dpiXRaw}\n Which is %: {dpiXRaw / 96.0}");
                //Main.AddLog($"Angular dpi, X:{dpiXAngular}\n Which is %: {dpiXAngular / 96.0}");
                dpiScaling = dpiXEffective / 96.0; // assuming that y and x axis dpi scaling will be uniform. So only need to check one value
            }
            catch (Exception e)
            {
                Main.AddLog($"Was unable to set a new dpi scaling, defaulting to 100% zoom, exception: {e}");
                dpiScaling = 1;
            }
        }

        private static void RefreshScaling()
        {
            if (window.Width * 9 > window.Height * 16)  // image is less than 16:9 aspect
                screenScaling = window.Height / 1080.0;
            else
                screenScaling = window.Width / 1920.0; //image is higher than 16:9 aspect

            Main.AddLog("SCALING VALUES UPDATED: Screen_Scaling = " + (screenScaling * 100).ToString("F2", Main.culture) + "%, DPI_Scaling = " + (dpiScaling * 100).ToString("F2", Main.culture) + "%, UI_Scaling = " + (uiScaling * 100).ToString("F0", Main.culture) + "%");
        }

        public static void UpdateWindow(Bitmap image = null)
        {
            RefreshDPIScaling();
            if (image != null || !VerifyWarframe())
            {
                int width = image?.Width ?? Screen.PrimaryScreen.Bounds.Width;
                int height = image?.Height ?? Screen.PrimaryScreen.Bounds.Height;
                window = new Rectangle(0, 0, width, height);
                center = new Point(window.X + window.Width / 2, window.Y + window.Height / 2);
                if (image != null)
                    Main.AddLog("DETECTED LOADED IMAGE BOUNDS: " + window.ToString());
                else
                    Main.AddLog("Couldn't Detect Warframe Process. Using Primary Screen Bounds: " + window.ToString() + " Named: " + Screen.PrimaryScreen.DeviceName);

                RefreshScaling();
                return;
            }

            if (!Win32.GetWindowRect(HandleRef, out Win32.R osRect))
            { // get window size of warframe
                if (Settings.debug)
                { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
                    int width = Screen.PrimaryScreen.Bounds.Width * (int)dpiScaling;
                    int height = Screen.PrimaryScreen.Bounds.Height * (int)dpiScaling;
                    window = new Rectangle(0, 0, width, height);
                    center = new Point(window.X + window.Width / 2, window.Y + window.Height / 2);
                    Main.AddLog("Couldn't Detect Warframe Process. Using Primary Screen Bounds: " + window.ToString() + " Named: " + Screen.PrimaryScreen.DeviceName);
                    RefreshScaling();
                    return;
                }
                else
                {
                    Main.AddLog("Failed to get window bounds");
                    Main.StatusUpdate("Failed to get window bounds", 1);
                    return;
                }
            }

            if (osRect.Left < -20000 || osRect.Top < -20000)
            { // if the window is in the VOID delete current process and re-set window to nothing
                Warframe = null;
                window = Rectangle.Empty;
            }
            else if (window == null || window.Left != osRect.Left || window.Right != osRect.Right || window.Top != osRect.Top || window.Bottom != osRect.Bottom)
            { // checks if old window size is the right size if not change it
                window = new Rectangle(osRect.Left, osRect.Top, osRect.Right - osRect.Left, osRect.Bottom - osRect.Top); // get Rectangle out of rect
                                                                                                                         // Rectangle is (x, y, width, height) RECT is (x, y, x+width, y+height) 
                int GWL_style = -16;
                uint WS_BORDER = 0x00800000;
                uint WS_POPUP = 0x80000000;


                uint styles = Win32.GetWindowLongPtr(HandleRef, GWL_style);
                if ((styles & WS_POPUP) != 0)
                {
                    // Borderless, don't do anything
                    currentStyle = WindowStyle.BORDERLESS;
                    Main.AddLog($"Borderless detected (0x{styles.ToString("X8", Main.culture)}, {window.ToString()}");
                }
                else if ((styles & WS_BORDER) != 0)
                {
                    // Windowed, adjust for thicc border
                    window = new Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38);
                    Main.AddLog($"Windowed detected (0x{styles.ToString("X8", Main.culture)}, adjusting window to: {window.ToString()}");
                    currentStyle = WindowStyle.WINDOWED;
                }
                else
                {
                    // Assume Fullscreen, don't do anything
                    Main.AddLog($"Fullscreen detected (0x{styles.ToString("X8", Main.culture)}, {window.ToString()}");
                    currentStyle = WindowStyle.FULLSCREEN;
                    //Show the Fullscreen prompt
                    if (Settings.isOverlaySelected)
                    {
                        Main.AddLog($"Showing the Fullscreen Reminder");
                        Main.RunOnUIThread(() =>
                        {
                            Main.SpawnFullscreenReminder();
                        });
                    }
                }
                    
                center = new Point(window.X + window.Width / 2, window.Y + window.Height / 2);
                RefreshScaling();
            }
        }
    }

    public struct InventoryItem
    {
        public InventoryItem(string itemName, Rectangle boundingbox)
        {
            Name = itemName;
            Bounding = boundingbox;
            Count = 0;
        }

        static public T DeepCopy<T>(T obje)
        {
            BinaryFormatter s = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                s.Serialize(ms, obje);
                ms.Position = 0;
                T t = (T)s.Deserialize(ms);

                return t;
            }
        }

        public string Name { get; set; }
        public Rectangle Bounding { get; set; }
        public int Count { get; set; }
    }
}

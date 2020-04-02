using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace WFInfo
{
    class OCR
    {
        private static string applicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
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
            UNKNOWN = -1
        }

        //TODO: DARK_LOTUS needs colors, and well, the rest need confirmations
        // Colors for the top left "profile bar"
        public static Color[] ThemePrimary = new Color[] {  Color.FromArgb(190, 169, 102),		//VITRUVIAN		
															Color.FromArgb(153,  31,  35), 		//STALKER		
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
															Color.FromArgb(140, 119, 147) };    //DARK_LOTUS

        public static Color[] ThemeSecondary = new Color[] {Color.FromArgb(245, 227, 173),		//VITRUVIAN		
															Color.FromArgb(255,  61,  51), 		//STALKER		
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
															Color.FromArgb(200, 169, 237) };    //DARK_LOTUS	

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

        //public static float dpi;
        //private static double ScreenScaling; // Additional to settings.scaling this is used to calculate any widescreen or 4:3 aspect content.
        //private static double TotalScaling;

        // DPI - Only used to display on screen or to get the "actual" screen bounds
        public static double dpiScaling;
        // UI - Scaling used in Warframe
        public static double uiScaling;
        // Screen / Resolution Scaling - Used to adjust pixel values to each person's monitor
        public static double screenScaling;



        public static TesseractEngine firstEngine = new TesseractEngine(applicationDirectory + @"\tessdata", "engbest")
        {
            DefaultPageSegMode = PageSegMode.SingleBlock
        };
        public static TesseractEngine secondEngine = new TesseractEngine(applicationDirectory + @"\tessdata", "engbest")
        {
            DefaultPageSegMode = PageSegMode.SingleBlock
        };

        public static TesseractEngine[] engines = new TesseractEngine[4];
        public static Regex RE = new Regex("[^a-z&// ]", RegexOptions.IgnoreCase | RegexOptions.Compiled);



        // Pixel measurements for reward screen @ 1920 x 1080 with 100% scale https://docs.google.com/drawings/d/1Qgs7FU2w1qzezMK-G1u9gMTsQZnDKYTEU36UPakNRJQ/edit
        public const int pixleRewardWidth = 968;
        public const int pixleRewardHeight = 235;
        public const int pixleRewardYDisplay = 316;
        public const int pixelRewardLineHeight = 44;
        public const int pixleRewardLineWidth = 240;

        // Pixel measurement for player bars for player count
        //   Width is same as pixRwrdWid
        // public static int pixRareWid = pixRwrdWid;
        //   Height is always 1px
        // public static int pixRareHei = 1;
        //   Box is centered horizontally
        // public static int pixRareXDisp = ???;
        public const int pixleRareYDisplay = 58;
        public const int pixleOverlayPossition = 30;

        // Pixel measurement for profile bars ( for theme detection )
        public const int pixelProfileXDisplay = 97;
        public const int pixelProfileYDisplay = 86;
        public const int pixelProfileWidth = 184;
        public const int pixelProfileHeight = 1;

        // Pixel measurements for the "VOID FISSURE / REWARDS"
        public const int pixelFissureWidth = 377;
        public const int pixelFissureHeight = 37;
        public const int pixelFissureXDisplay = 238; // Removed 50 pixels to assist with 2 player theme detection

        public const int pixelFissureYDisplay = 47;

        public const int SCALING_LIMIT = 100;
        private static bool processingActive = false;

        private static Bitmap bigScreenshot;
        private static Bitmap partialScreenshot;
        //private static Bitmap[] partScreenshots;
        private static Bitmap partialScreenshotExpanded;

        private static WFtheme activeTheme;
        private static string[] firstChecks;
        private static List<string> secondChecks;
        private static int[] firstProximity = { -1, -1, -1, -1 };
        private static int[] secondProximity = { -1, -1, -1, -1 };
        private static string timestamp;

        private static string clipboard;
        #endregion
        public static void init()
        {
            Directory.CreateDirectory(Main.appPath + @"\Debug");

            for (int i = 0; i < 4; i++)
            {
                engines[i] = new TesseractEngine(applicationDirectory + @"\tessdata", "engbest")
                {
                    DefaultPageSegMode = PageSegMode.SingleBlock
                };
            }
        }

        internal static void ProcessRewardScreen(Bitmap file = null)
        {
            if (processingActive)
            {
                Main.StatusUpdate("Already Processing Reward Screen", 2);
                return;
            }
            processingActive = true;
            Main.StatusUpdate("Processing...", 0);
            Main.AddLog("----  Triggered Reward Screen Processing  ------------------------------------------------------------------");
            try
            {
                DateTime time = DateTime.UtcNow;
                timestamp = time.ToString("yyyy-MM-dd HH-mm-ssff");

                var watch = Stopwatch.StartNew();
                long start = watch.ElapsedMilliseconds;

                double uiScalingVal = uiScaling;
                List<Bitmap> parts;

                if (Settings.autoScaling)
                {
                    parts = ExtractPartBoxAutomatically(out uiScalingVal, out activeTheme, file);
                    bigScreenshot = file ?? CaptureScreenshot();

                } else
                {
                    // Get that theme
                    activeTheme = GetThemeWeighted(out _, file);

                    bigScreenshot = file ?? CaptureScreenshot();

                    // Get the part box and filter it
                    parts = FilterAndSeparateParts(bigScreenshot, activeTheme);
                }


                firstChecks = new string[parts.Count];
                Task[] tasks = new Task[parts.Count];
                for (int i = 0; i < parts.Count; i++)
                {
                    int tempI = i;
                    tasks[i] = Task.Factory.StartNew(() => { firstChecks[tempI] = OCR.GetTextFromImage(parts[tempI], engines[tempI]); });
                }
                Task.WaitAll(tasks);

                double bestPlat = 0;
                int bestDucat = 0;
                int bestPlatItem = 0;
                int bestDucatItem = 0;
                List<int> unownedItems = new List<int>();

                NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;
                IFormatProvider provider = CultureInfo.CreateSpecificCulture("en-GB");

                if (firstChecks.Length > 0)
                {
                    clipboard = string.Empty;
                    int width = (int)(pixleRewardWidth * screenScaling * uiScalingVal) + 10;
                    int startX = center.X - width / 2 + (int)(width * 0.004);
                    if (firstChecks.Length == 3 && firstChecks[0].Length > 0) { startX += width / 8; }
                    int overWid = (int)(width / (4.1 * dpiScaling));
                    int startY = (int)(center.Y / dpiScaling - 20 * screenScaling * uiScalingVal);
                    int partNumber = 0;
                    bool hideRewardInfo = false;
                    for (int i = 0; i < firstChecks.Length; i++) {
                        string part = firstChecks[i];
                        if (part.Replace(" ", "").Length > 6) {


                            string correctName = Main.dataBase.GetPartName(part, out firstProximity[i]);
                            JObject job = Main.dataBase.marketData.GetValue(correctName).ToObject<JObject>();
                            string ducats = job["ducats"].ToObject<string>();
                            if (int.Parse(ducats) == 0) {
                                hideRewardInfo = true;
                            }
                            string plat = job["plat"].ToObject<string>();
                            double platinum = double.Parse(plat, styles, provider);
                            string volume = job["volume"].ToObject<string>();
                            bool vaulted = Main.dataBase.IsPartVaulted(correctName);
                            string partsOwned = Main.dataBase.PartsOwned(correctName);
                            int duc = int.Parse(ducats);

                            if (platinum >= bestPlat) {
                                bestPlat = platinum; bestPlatItem = i;
                                if (duc >= bestDucat) {
                                    bestDucat = duc; bestDucatItem = i;
                                }
                            }
                            if (duc > bestDucat) {
                                bestDucat = duc; bestDucatItem = i;
                            }

                            if (duc > 0) {
                                bool _ = int.TryParse(partsOwned, out int owned);
                                if (owned < int.Parse(Main.dataBase.equipmentData[Main.dataBase.GetSetName(correctName)]["parts"][correctName]["count"].ToString())) {
                                    unownedItems.Add(i);
                                }
                            }

                            if (platinum > 0) {
                                clipboard += "[" + correctName.Replace(" Blueprint", "") + "]: " + plat + ":platinum: ";
                                if (i == firstChecks.Length - 1) {
                                    clipboard += Settings.ClipboardTemplate;
                                } else {
                                    clipboard += "-  ";
                                }
                            }

                            Main.RunOnUIThread(() => {
                                if (Settings.isOverlaySelected) {
                                    Main.overlays[partNumber].LoadTextData(correctName, plat, ducats, volume, vaulted, partsOwned, hideRewardInfo);
                                    Main.overlays[partNumber].Resize(overWid);
                                    Main.overlays[partNumber].Display((int)((startX + width / 4 * partNumber) / dpiScaling), startY);

                                } else {
                                    Main.window.loadTextData(correctName, plat, ducats, volume, vaulted, partsOwned, partNumber, true, hideRewardInfo);
                                }
                                if (Settings.clipboard && clipboard != string.Empty)
                                    Clipboard.SetText(clipboard);

                            });
                            partNumber++;
                            hideRewardInfo = false;
                        }
                    }
                    var end = watch.ElapsedMilliseconds;
                    Main.StatusUpdate("Completed Processing (" + (end - start) + "ms)", 0);

                    if (Settings.Highlight) {
                        Main.RunOnUIThread(() => {
                            Main.overlays[bestPlatItem].bestPlatChoice();
                            Main.overlays[bestDucatItem].bestDucatChoice();
                            foreach (int item in unownedItems) {
                                Main.overlays[item].bestOwnedChoice();
                            }
                        });
                    }

                    if (partialScreenshot.Height < 70) {
                        SlowSecondProcess();
                        end = watch.ElapsedMilliseconds;
                    }

                    if (Settings.Highlight) {
                        Main.RunOnUIThread(() => {
                            Main.overlays[bestPlatItem].bestPlatChoice();
                            Main.overlays[bestDucatItem].bestDucatChoice();
                            foreach (int item in unownedItems) {
                                Main.overlays[item].bestOwnedChoice();
                            }
                        });
                    }
                    Main.AddLog(("----  Total Processing Time " + (end - start) + " ms  ------------------------------------------------------------------------------------------").Substring(0, 108));
                }

                if (firstChecks == null || CheckIfError())
                {
                    if (firstChecks == null)
                    {
                        Main.AddLog(("----  Partial Processing Time, couldn't find rewards " + (watch.ElapsedMilliseconds - start) + " ms  ------------------------------------------------------------------------------------------").Substring(0, 108));
                        Main.StatusUpdate("Couldn't find any rewards to display", 2);
                    }
                    Main.RunOnUIThread(() =>
                    {
                        Main.SpawnErrorPopup(time);
                    });

                }
                watch.Stop();

                (new DirectoryInfo(Main.appPath + @"\Debug\")).GetFiles()
                    .Where(f => f.CreationTime < DateTime.Now.AddHours(-1 * Settings.imageRetentionTime))
                    .ToList().ForEach(f => f.Delete());
            }
            catch (Exception ex)
            {
                Main.AddLog(ex.ToString());
                Main.StatusUpdate("Genneric error occured during processing", 1);
            }

            if (bigScreenshot != null)
            {
                bigScreenshot.Save(Main.appPath + @"\Debug\FullScreenShot " + timestamp + ".png");
                bigScreenshot.Dispose();
                bigScreenshot = null;
            }
            if (partialScreenshot != null)
            {
                partialScreenshot.Save(Main.appPath + @"\Debug\PartBox " + timestamp + ".png");
                partialScreenshot.Dispose();
                partialScreenshot = null;
            }

            processingActive = false;

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

            Bitmap newFilter = ScaleUpAndFilter(partialScreenshot, activeTheme);
            partialScreenshotExpanded.Save(Main.appPath + @"\Debug\PartShotUpscaled " + timestamp + ".png");
            newFilter.Save(Main.appPath + @"\Debug\PartShotUpscaledFiltered " + timestamp + ".png");
            Main.AddLog(("----  SECOND OCR CHECK  ------------------------------------------------------------------------------------------").Substring(0, 108));
            secondChecks = SeparatePlayers(newFilter, secondEngine);
            List<int> comparisions = new List<int>();

            if (secondChecks != null && firstChecks.Length != secondChecks.Count)
            {
                //Whelp, we fucked bois
                Main.AddLog("Second check didn't find the same amount of part names");
                Main.StatusUpdate("Verification of items failed", 2);
                return;
            }
            bool hideRewardInfo = false;
            int partNumber = 0;
            for (int i = 0; i < firstChecks.Length; i++)
            {
                string first = firstChecks[i];
                if (first.Replace(" ", "").Length > 6)
                {
                    string second = secondChecks[i];
                    string secondName = Main.dataBase.GetPartName(second, out secondProximity[i]);
                    if (secondProximity[i] < firstProximity[i])
                    {
                        JObject job = Main.dataBase.marketData.GetValue(secondName).ToObject<JObject>();
                        string ducats = job["ducats"].ToObject<string>();
                        if (int.Parse(ducats) == 0)
                        {
                            hideRewardInfo = true;
                        }
                        string plat = job["plat"].ToObject<string>();
                        string volume = job["volume"].ToObject<string>();
                        bool vaulted = Main.dataBase.IsPartVaulted(secondName);
                        string partsOwned = Main.dataBase.PartsOwned(secondName);

                        Main.RunOnUIThread(() =>
                        {
                            if (Settings.isOverlaySelected)
                            {
                                Main.overlays[partNumber].LoadTextData(secondName, plat, ducats, volume, vaulted, partsOwned, hideRewardInfo);
                            }
                            else
                            {
                                Main.window.loadTextData(secondName, plat, ducats, volume, vaulted, partsOwned, partNumber, false, hideRewardInfo);
                            }
                        });

                    }
                    hideRewardInfo = false;
                    partNumber++;
                }
            }
        }

        /// <summary>
        /// Processes the theme, parse image to detect the theme in the image. Parse null to detect the theme from the screen.
        /// closeestThresh is used for ???
        /// </summary>
        /// <param name="closestThresh"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static WFtheme GetThemeWeighted(out double closestThresh, Bitmap image = null)
        {
            int profileX = (int)(pixelProfileXDisplay * screenScaling * uiScaling);
            int profileY = (int)(pixelProfileYDisplay * screenScaling * uiScaling);
            int profileWid = (int)(pixelProfileWidth * screenScaling * uiScaling);

            int fissureX = (int)(pixelFissureXDisplay * screenScaling * uiScaling);
            int fissureY = (int)(pixelFissureYDisplay * screenScaling * uiScaling);
            int fissureWid = (int)(pixelFissureWidth * screenScaling * uiScaling);
            int fissureHei = (int)(pixelFissureHeight * screenScaling * uiScaling);


            if (image == null)
            {
                image = new Bitmap(fissureX + fissureWid + 1, profileY + 1);

                Size profileSize = new Size(profileWid, 1);
                Size fissureSize = new Size(fissureWid, fissureHei);
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.CopyFromScreen(window.X + profileX, window.Y + profileY, profileX, profileY, profileSize, CopyPixelOperation.SourceCopy);
                    graphics.CopyFromScreen(window.X + fissureX, window.Y + fissureY, fissureX, fissureY, fissureSize, CopyPixelOperation.SourceCopy);
                }
            }

            double[] weights = new double[14] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int closest = 0;

            int coorX = fissureX;
            int coorY = fissureY + (fissureHei / 2);
            int endX = fissureX + fissureWid;

            for (; coorX < endX; coorX++)
            {
                closest = (int)GetClosestTheme(image.GetPixel(coorX, coorY), out int thresh);
                weights[closest] += 1.0 / (1 + thresh);
            }

            coorX = profileX;
            coorY = profileY;
            endX = profileX + profileWid;

            for (; coorX < endX; coorX++)
            {
                closest = (int)GetClosestTheme(image.GetPixel(coorX, coorY), out int thresh);
                weights[closest] += 1.0 / (1 + thresh);
            }

            closest = 0;
            for (int i = 1; i < weights.Length; i++)
            {
                if (weights[closest] < weights[i])
                    closest = i;
            }

            WFtheme ret = ((WFtheme)closest);
            Main.AddLog("HIGHEST WEIGHTED THEME(" + weights[closest].ToString("F2") + "): " + ret.ToString());
            closestThresh = weights[closest];
            return ret;
        }

        public static int GetThemeThreshold(Bitmap image = null)
        {
            int fissureX = (int)(pixelFissureXDisplay * screenScaling * uiScaling);
            int fissureY = (int)(pixelFissureYDisplay * screenScaling * uiScaling);
            int fissureWid = (int)(pixelFissureWidth * screenScaling * uiScaling);
            int fissureHei = (int)(pixelFissureHeight * screenScaling * uiScaling);


            if (image == null)
            {
                image = new Bitmap(fissureX + fissureWid, fissureY + fissureHei);

                Size fissureSize = new Size(fissureWid, fissureHei);
                using (Graphics graphics = Graphics.FromImage(image))
                    graphics.CopyFromScreen(window.X + fissureX, window.Y + fissureY, fissureX, fissureY, fissureSize, CopyPixelOperation.SourceCopy);
                //image.Save(Main.appPath + @"\Debug\TESTSHOT " + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff") + ".png");
            }

            int closest = 999;
            WFtheme theme = WFtheme.UNKNOWN;

            int coorY = fissureY + (fissureHei / 2);
            int endX = fissureX + fissureWid;

            for (int coorX = fissureX; coorX < endX; coorX++)
            {
                WFtheme temp = GetClosestTheme(image.GetPixel(coorX, coorY), out int thresh);
                if (thresh < closest)
                {
                    theme = temp;
                    closest = thresh;
                }
            }

            Main.AddLog("CLOSEST THEME(" + closest + "): " + theme.ToString());
            return closest;
        }

        private static int[,,] GetThemeCache = new int[256, 256, 256];
        private static int[,,] GetThresholdCache = new int[256, 256, 256];
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
            GetThemeCache[clr.R, clr.G, clr.B] = (int)minTheme + 1;
            GetThresholdCache[clr.R, clr.G, clr.B] = threshold;
            return minTheme;
        }

        /// <summary>
        /// Processes the image the user cropped in the selection
        /// </summary>
        /// <param name="snapItImage"></param>
        internal static void ProcessSnapIt(Bitmap snapItImage, Bitmap fullShot, Point snapItOrigin)
        {
            var watch = Stopwatch.StartNew();
            long start = watch.ElapsedMilliseconds;

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff");
            WFtheme theme = GetThemeWeighted(out _, fullShot);
            snapItImage.Save(Main.appPath + @"\Debug\SnapItImage " + timestamp + ".png");
            Bitmap snapItImageFiltered = ScaleUpAndFilter(snapItImage, theme, true);
            snapItImageFiltered.Save(Main.appPath + @"\Debug\SnapItImageFiltered " + timestamp + ".png");
            long end = watch.ElapsedMilliseconds;
            Main.StatusUpdate("Completed snapit Processing(" + (end - start) + "ms)", 0);
            List<InventoryItem> foundParts = FindAllParts(snapItImageFiltered);
            string csv = string.Empty;

            if (!File.Exists(applicationDirectory + @"\export " + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".csv") && Settings.SnapitExport)
                csv += "ItemName,Plat,Ducats,Volume,Vaulted,Owned," + DateTime.UtcNow.ToString("yyyy-MM-dd") + Environment.NewLine;

            foreach (var part in foundParts)
            {
                if (part.name.Length < 13) // if part name is smaller than "Bo prime handle" skip current part
                    continue;


                string name = Main.dataBase.GetPartName(part.name, out firstProximity[0]);
                JObject job = Main.dataBase.marketData.GetValue(name).ToObject<JObject>();
                string plat = job["plat"].ToObject<string>();
                string ducats = job["ducats"].ToObject<string>();
                string volume = job["volume"].ToObject<string>();
                bool vaulted = Main.dataBase.IsPartVaulted(name);
                string partsOwned = Main.dataBase.PartsOwned(name);

                if (Settings.SnapitExport)
                {
                    var owned = partsOwned == string.Empty ? "0" : partsOwned;
                    csv += name + "," + plat + "," + ducats + "," + volume + "," + vaulted.ToString() + "," + owned + ", \"\"" + Environment.NewLine;
                }

                int width = (int)(part.bounding.Width * screenScaling);
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
                    itemOverlay.LoadTextData(name, plat, ducats, volume, vaulted, partsOwned, false);
                    itemOverlay.Resize(width);
                    itemOverlay.Display(snapItOrigin.X + (part.bounding.X - width / 8), (int)(snapItOrigin.Y + part.bounding.Y - itemOverlay.Height));
                });
            }
            Main.snapItOverlayWindow.tempImage.Dispose();
            end = watch.ElapsedMilliseconds;
            Main.StatusUpdate("Completed snapit Displaying(" + (end - start) + "ms)", 0);
            watch.Stop();
            if (Settings.SnapitExport)
            {
                File.AppendAllText(applicationDirectory + @"\export " + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".csv", csv);
            }
        }
        /// <summary>
        /// Filters out any group of words and addes them all into a single InventoryItem, containing the found words as well as the bounds within they reside.
        /// </summary>
        /// <param name="filteredImage"></param>
        /// <returns>List of found items</returns>
        private static List<InventoryItem> FindAllParts(Bitmap filteredImage)
        {
            DateTime time = DateTime.UtcNow;
            string timestamp = time.ToString("yyyy-MM-dd HH-mm-ssff");
            List<InventoryItem> foundItems = new List<InventoryItem>();
            int numberTooLarge = 0;
            int numberTooFewCharacters = 0;
            int numberTooLargeButEnoughCharacters = 0;
            using (var page = firstEngine.Process(filteredImage, PageSegMode.SparseText))
            {
                using (var iterator = page.GetIterator())
                {

                    iterator.Begin();
                    do
                    {
                        string currentWord = iterator.GetText(PageIteratorLevel.Word);
                        iterator.TryGetBoundingBox(PageIteratorLevel.Word, out Rect tempbounds);
                        Rectangle bounds = new Rectangle(tempbounds.X1, tempbounds.Y1, tempbounds.Width, tempbounds.Height);
                        if (currentWord != null)
                        {
                            currentWord = RE.Replace(currentWord, "").Trim();
                            if (currentWord.Length > 0)
                            { //word is valid start comparing to others
                                var paddedBounds = new Rectangle(bounds.X - bounds.Height / 3, bounds.Y - bounds.Height / 3, bounds.Width + bounds.Height, bounds.Height + bounds.Height / 2);



                                using (Graphics g = Graphics.FromImage(filteredImage))
                                {
                                    if (paddedBounds.Height > 30 * screenScaling || paddedBounds.Width > 60 * screenScaling)
                                    { //box is invalid, fill it out
                                        if (currentWord.Length > 3)
                                        { // more than 3 characters in a box too large is likely going to be good, pass it but mark as potentially bad
                                            g.DrawRectangle(new Pen(Brushes.Orange), paddedBounds);
                                            numberTooLargeButEnoughCharacters++;
                                        }
                                        else
                                        {
                                            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 139, 0, 0)), paddedBounds);
                                            numberTooLarge++;
                                            continue;
                                        }
                                    }
                                    else if (currentWord.Length < 2)
                                    {
                                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 255, 165, 0)), paddedBounds);
                                        numberTooFewCharacters++;
                                        continue;
                                    }
                                    else
                                    {
                                        g.DrawRectangle(new Pen(Brushes.Pink), paddedBounds);
                                    }
                                    g.DrawRectangle(new Pen(Brushes.Green), bounds);
                                    g.DrawString(currentWord, new Font("Arial", 16), new SolidBrush(Color.Pink), new Point(paddedBounds.X, paddedBounds.Y));

                                }
                                int i = foundItems.Count - 1;

                                for (; i >= 0; i--)
                                    if (foundItems[i].bounding.IntersectsWith(paddedBounds))
                                        break;

                                if (i == -1)
                                {
                                    foundItems.Add(new InventoryItem(currentWord, paddedBounds));
                                }
                                else
                                {
                                    int left = Math.Min(foundItems[i].bounding.Left, paddedBounds.Left);
                                    int top = Math.Min(foundItems[i].bounding.Top, paddedBounds.Top);
                                    int right = Math.Max(foundItems[i].bounding.Right, paddedBounds.Right);
                                    int bot = Math.Max(foundItems[i].bounding.Bottom, paddedBounds.Bottom);

                                    Rectangle intersectingBounds = new Rectangle(left, top, right - left, bot - top);

                                    InventoryItem newItem = new InventoryItem(foundItems[i].name + " " + currentWord, intersectingBounds);
                                    foundItems.RemoveAt(i);
                                    foundItems.Add(newItem);
                                }

                            }
                        }
                    }
                    while (iterator.Next(PageIteratorLevel.Word));
                }
            }

            if (numberTooLarge > .3 * foundItems.Count || numberTooFewCharacters > .4 * foundItems.Count)
            {
                Main.AddLog("numberTooLarge: " + numberTooLarge + ", numberTooFewCharacters: " + numberTooFewCharacters + ", numberTooLargeButEnoughCharacters: " + numberTooLargeButEnoughCharacters + ", foundItems.Count: " + foundItems.Count);
                //If there's a too large % of any error make a pop-up. These precentages are arbritary at the moment, a rough index.
                Main.RunOnUIThread(() =>
                {
                    Main.SpawnErrorPopup(time);
                });
            }

            filteredImage.Save(Main.appPath + @"\Debug\SnapItImageBounds " + timestamp + ".png");
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
                case WFtheme.LEGACY:
                    return (test.GetBrightness() >= 0.75 && test.GetSaturation() <= 0.2)
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
                default:
                    // This shouldn't be ran
                    //   Only for initial testing
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 2 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2;
            }
        }

        private static Bitmap ScaleUpAndFilter(Bitmap image, WFtheme active, bool fromSnapit = false)
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
        private static List<Bitmap> ExtractPartBoxAutomatically(out double scaling, out WFtheme active, Bitmap fullScreen = null)
        {
            Stopwatch watch = Stopwatch.StartNew();
            long start = watch.ElapsedMilliseconds;
            long beginning = start;

            int lineHeight = (int)(pixelRewardLineHeight / 2 * screenScaling);

            Color clr;
            int width = fullScreen == null ? window.Width * (int)dpiScaling : fullScreen.Width;
            int height = fullScreen == null ? window.Height * (int)dpiScaling : fullScreen.Height;
            int mostWidth = (int)(pixleRewardWidth * screenScaling);
            int mostLeft = (width / 2) - (mostWidth / 2);
            // Most Top = pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight
            //                   (316          -        235        +       44)    *    1.1    =    137
            int mostTop = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight) * screenScaling);
            int mostBot = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight) * screenScaling * 0.5);
            //Bitmap postFilter = new Bitmap(mostWidth, mostBot - mostTop);
            Bitmap preFilter;

            if (fullScreen != null)
                preFilter = fullScreen.Clone(new Rectangle(mostLeft, mostTop, mostWidth, mostBot - mostTop), fullScreen.PixelFormat);
            else
            {
                preFilter = new Bitmap(mostWidth, mostBot - mostTop);
                using (Graphics graphics = Graphics.FromImage(preFilter))
                    graphics.CopyFromScreen(window.Left + mostLeft, window.Top + mostTop, 0, 0, new Size(preFilter.Width, preFilter.Height), CopyPixelOperation.SourceCopy);
            }

            long end = watch.ElapsedMilliseconds;
            Console.WriteLine("Grabbed images " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;

            double[] weights = new double[14] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int minWidth = mostWidth / 4;

            for (int y = lineHeight; y < preFilter.Height; y++)
            {
                double perc = (y - lineHeight) / (preFilter.Height - lineHeight);
                int totWidth = (int)(minWidth * perc + minWidth);
                for (int x = 0; x < totWidth; x++)
                {
                    int match = (int)GetClosestTheme(preFilter.GetPixel(x + (mostWidth - totWidth) / 2, y), out int thresh);
                    weights[match] += 1 / Math.Pow(thresh + 1, 4);
                }
            }

            double max = 0;
            active = WFtheme.UNKNOWN;
            for (int i = 0; i < weights.Length; i++)
            {
                Console.Write(weights[i].ToString("F2") + " ");
                if (weights[i] > max)
                {
                    max = weights[i];
                    active = (WFtheme)i;
                }
            }
            Console.WriteLine();

            Main.AddLog("CLOSEST THEME(" + max.ToString("F2") + "): " + active.ToString());

            end = watch.ElapsedMilliseconds;
            Console.WriteLine("Got theme " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;

            int[] rows = new int[preFilter.Height];
            // 0 => 50   27 => 77   50 => 100


            //Console.WriteLine("ROWS: 0 to " + preFilter.Height);
            for (int y = 0; y < preFilter.Height; y++)
            {
                rows[y] = 0;
                for (int x = 0; x < preFilter.Width; x++)
                {
                    clr = preFilter.GetPixel(x, y);
                    if (ThemeThresholdFilter(clr, active))
                    {
                        rows[y]++;
                        //postFilter.SetPixel(x, y, Color.Black);
                    } //else
                      //  postFilter.SetPixel(x, y, Color.White);
                }
                //Console.Write(rows[y] + " ");
            }
            //Console.WriteLine();


            end = watch.ElapsedMilliseconds;
            Console.WriteLine("Filtered Image " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;

            double[] percWeights = new double[51];
            double[] topWeights = new double[51];
            double[] midWeights = new double[51];
            double[] botWeights = new double[51];

            int topLine_100 = preFilter.Height - lineHeight;
            int topLine_50 = lineHeight / 2;

            scaling = -1;
            double lowestWeight = 0;

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
            Console.WriteLine("Got scaling " + (end - start) + "ms");

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
                //int yFromTop = preFilter.Height - (topFive[i] * (topLine_100 - topLine_50) / 50 + topLine_50);

                //int scale = (50 + topFive[i]);

                //int textTop = (int)(screenScaling * TextSegments[0] * scale / 100);
                //int textTopBot = (int)(screenScaling * TextSegments[1] * scale / 100);
                //int textBothBot = (int)(screenScaling * TextSegments[2] * scale / 100);
                //int textTailBot = (int)(screenScaling * TextSegments[3] * scale / 100);

                Main.AddLog("RANK " + (5 - i) + " SCALE: " + (topFive[i] + 50) + "%\t\t" + percWeights[topFive[i]].ToString("F2") + " -- " + topWeights[topFive[i]].ToString("F2") + ", " + midWeights[topFive[i]].ToString("F2") + ", " + botWeights[topFive[i]].ToString("F2"));
                //Console.WriteLine("\t" + yFromTop + " - " + textTop + " - " + textTopBot + " - " + textBothBot + " - " + textTailBot);
            }


            //postFilter.Save(Main.appPath + @"\Debug\DebugBox1 " + timestamp + ".png");
            preFilter.Save(Main.appPath + @"\Debug\FullPartArea " + timestamp + ".png");

            scaling /= 100;
            double highScaling = scaling < 1.0 ? scaling + 0.01 : scaling;
            double lowScaling = scaling > 0.5 ? scaling - 0.01 : scaling;

            int cropWidth = (int)(pixleRewardWidth * screenScaling * highScaling);
            int cropLeft = (preFilter.Width / 2) - (cropWidth / 2);
            int cropTop = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight) * screenScaling * highScaling);
            int cropBot = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight) * screenScaling * lowScaling);
            int cropHei = cropBot - cropTop;
            cropTop = cropTop - mostTop;

            Rectangle rect = new Rectangle(cropLeft, cropTop, cropWidth, cropHei);
            partialScreenshot = preFilter.Clone(rect, System.Drawing.Imaging.PixelFormat.DontCare);

            end = watch.ElapsedMilliseconds;
            Console.WriteLine("Finished function " + (end - beginning) + "ms");

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

            double total = totalEven + totalOdd;
            Main.AddLog("EVEN DISTRIBUTION: " + (totalEven / total * 100).ToString("F2") + "%");
            Main.AddLog("ODD DISTRIBUTION: " + (totalOdd / total * 100).ToString("F2") + "%");

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
                newBox.Save(Main.appPath + @"\Debug\PartBox(" + i + ") " + timestamp + ".png");
            }
            return ret;
        }

        private static List<Bitmap> FilterAndSeparateParts(Bitmap image, WFtheme active)
        {
            int width = (int)(pixleRewardWidth * screenScaling * uiScaling);
            int lineHeight = (int)(pixelRewardLineHeight * screenScaling * uiScaling);
            int left = (image.Width / 2) - (width / 2);
            int top = (image.Height / 2) - (int)(pixleRewardYDisplay * screenScaling * uiScaling) + (int)(pixleRewardHeight * screenScaling * uiScaling) - lineHeight;

            partialScreenshot = new Bitmap(width, lineHeight);

            Color clr;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < partialScreenshot.Height; y++)
                {
                    clr = image.GetPixel(left + x, top + y);
                    partialScreenshot.SetPixel(x, y, clr);
                }
            }
            return FilterAndSeparatePartsFromPartBox(partialScreenshot, active);
        }

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
                    Rect outRect;
                    iter.Begin();
                    do
                    {
                        iter.TryGetBoundingBox(PageIteratorLevel.Word, out outRect);
                        string word = iter.GetText(PageIteratorLevel.Word);
                        //Console.WriteLine(outRect.ToString());
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
            arr2D.Sort(new arr2D_Compare());

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


            Console.WriteLine((ret[0].Length == 0 ? ret.Count - 1 : ret.Count) + " Players Found -- Odds (" + oddCount + ") vs Evens (" + evenCount + ")");

            if ((oddCount == 0 && evenCount == 0) || oddCount == evenCount)
            { //Detected 0 rewards
                return null;
            }

            return ret;
        }

        private class arr2D_Compare : IComparer<List<int>>
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
                center = new Point(window.Width / 2, window.Height / 2);

                width *= (int)dpiScaling;
                height *= (int)dpiScaling;
            }


            Bitmap image = new Bitmap(width, height);
            Size FullscreenSize = new Size(image.Width, image.Height);
            using (Graphics graphics = Graphics.FromImage(image))
                graphics.CopyFromScreen(window.Left, window.Top, 0, 0, FullscreenSize, CopyPixelOperation.SourceCopy);

            return image;
        }

        internal static void SnapScreenshot()
        {
            Main.snapItOverlayWindow.Populate(CaptureScreenshot());
            Main.snapItOverlayWindow.Show();
            Main.snapItOverlayWindow.Top = 0;
            Main.snapItOverlayWindow.WindowState = System.Windows.WindowState.Maximized;
            Main.snapItOverlayWindow.Topmost = true;
            Main.snapItOverlayWindow.Focusable = true;
            Main.snapItOverlayWindow.Focus();
        }

        public static bool VerifyWarframe()
        {
            if (Warframe != null && !Warframe.HasExited) { return true; }
            foreach (Process process in Process.GetProcesses())
            {
                if (process.MainWindowTitle == "Warframe")
                {
                    HandleRef = new HandleRef(process, process.MainWindowHandle);
                    Warframe = process;
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

        }

        private static void RefreshDPIScaling()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                dpiScaling = graphics.DpiX / 96; //assuming that y and x axis dpi scaling will be uniform. So only need to check one value

            uiScaling = Settings.scaling / 100.0;
        }

        private static void RefreshScaling()
        {
            if (window.Width * 9 > window.Height * 16)  // image is less than 16:9 aspect
                screenScaling = window.Height / 1080.0;
            else
                screenScaling = window.Width / 1920.0; //image is higher than 16:9 aspect

            Main.AddLog("SCALING VALUES UPDATED: Screen_Scaling = " + (screenScaling * 100).ToString("F2") + "%, DPI_Scaling = " + (dpiScaling * 100).ToString("F2") + "%, UI_Scaling = " + (uiScaling * 100).ToString("F0") + "%");
        }

        public static void UpdateWindow(Bitmap image = null)
        {
            RefreshDPIScaling();
            if (image != null || !VerifyWarframe())
            {
                int width = image?.Width ?? Screen.PrimaryScreen.Bounds.Width * (int)dpiScaling;
                int height = image?.Height ?? Screen.PrimaryScreen.Bounds.Height * (int)dpiScaling;
                window = new Rectangle(0, 0, width, height);
                center = new Point(window.Width / 2, window.Height / 2);
                if (image != null)
                    Main.AddLog("DETECTED LOADED IMAGE BOUNDS: " + window.ToString());
                else
                    Main.AddLog("Couldn't Detect Warframe Process. Using Primary Screen Bounds: " + window.ToString() + " Named: " + Screen.PrimaryScreen.DeviceName);

                RefreshScaling();
                return;
            }

            if (!Win32.GetWindowRect(HandleRef, out Win32.r osRect))
            { // get window size of warframe
                if (Settings.debug)
                { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
                    int width = Screen.PrimaryScreen.Bounds.Width * (int)dpiScaling;
                    int height = Screen.PrimaryScreen.Bounds.Height * (int)dpiScaling;
                    window = new Rectangle(0, 0, width, height);
                    center = new Point(window.Width / 2, window.Height / 2);
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
                Main.AddLog("Detected Warframe Process - Window Bounds: " + window.ToString());
                int GWL_style = -16;
                uint WS_BORDER = 0x00800000;
                uint WS_POPUP = 0x80000000;


                uint styles = Win32.GetWindowLongPtr(HandleRef, GWL_style);
                if ((styles & WS_POPUP) != 0)
                {
                    // Borderless, don't do anything
                    currentStyle = WindowStyle.BORDERLESS;
                    Main.AddLog("Borderless detected (0x" + styles.ToString("X8") + ")");
                }
                else if ((styles & WS_BORDER) != 0)
                {
                    // Windowed, adjust for thicc border
                    window = new Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38);
                    Main.AddLog("Windowed detected (0x" + styles.ToString("X8") + "), adjusting window to: " + window.ToString());
                    currentStyle = WindowStyle.WINDOWED;
                }
                else
                {
                    // Assume Fullscreen, don't do anything
                    Main.AddLog("Fullscreen detected (0x" + styles.ToString("X8") + ")");
                    currentStyle = WindowStyle.FULLSCREEN;
                }
                center = new Point(window.Width / 2, window.Height / 2);
                RefreshScaling();
            }
        }
    }

    public struct InventoryItem
    {
        public InventoryItem(string itemName, Rectangle boundingbox)
        {
            name = itemName;
            bounding = boundingbox;
        }

        static public T DeepCopy<T>(T obj)
        {
            BinaryFormatter s = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                s.Serialize(ms, obj);
                ms.Position = 0;
                T t = (T)s.Deserialize(ms);

                return t;
            }
        }

        public string name;
        public Rectangle bounding;
    }
}
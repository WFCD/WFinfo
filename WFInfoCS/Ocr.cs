using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace WFInfoCS
{
    class OCR
    {
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



        public static TesseractEngine firstEngine = new TesseractEngine("", "engbest")
        {
            DefaultPageSegMode = PageSegMode.SingleBlock
        };
        public static TesseractEngine secondEngine = new TesseractEngine("", "engbest")
        {
            DefaultPageSegMode = PageSegMode.SingleBlock
        };
        public static Regex RE = new Regex("[^a-z&//]", RegexOptions.IgnoreCase | RegexOptions.Compiled);



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
        private static Bitmap partialScreenshotExpanded;
        private static Bitmap partialScreenshotFiltered;

        private static WFtheme activeTheme;
        private static List<string> firstChecks;
        private static List<string> secondChecks;
        private static int[] firstProximity = { -1, -1, -1, -1 };
        private static int[] secondProximity = { -1, -1, -1, -1 };
        private static string timestamp;

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

                // Look at me mom, I'm doing fancy shit
                Main.AddLog("Scaling values: Screen_Scaling = " + (screenScaling * 100).ToString("F2") + "%, DPI_Scaling = " + (dpiScaling * 100).ToString("F2") + "%, UI_Scaling = " + (uiScaling * 100).ToString("F0") + "%");


                // Get that theme
                activeTheme = GetThemeWeighted(out _, file);


                bigScreenshot = file ?? CaptureScreenshot();

                // Get the part box and filter it
                partialScreenshotFiltered = FilterPartNames(bigScreenshot, activeTheme);

                Directory.CreateDirectory(Main.appPath + @"\Debug");

                bigScreenshot.Save(Main.appPath + @"\Debug\FullScreenShot " + timestamp + ".png");
                partialScreenshot.Save(Main.appPath + @"\Debug\PartBox " + timestamp + ".png");
                partialScreenshotFiltered.Save(Main.appPath + @"\Debug\PartBoxFilter " + timestamp + ".png");

                firstChecks = SeparatePlayers(partialScreenshotFiltered, firstEngine);
                Task complete = null;
                if (firstChecks != null)
                {
                    if (partialScreenshotFiltered.Height < 70)
                        complete = Task.Factory.StartNew(SlowSecondProcess);
                    int width = (int)(pixleRewardWidth * screenScaling * uiScaling) + 10;
                    int startX = center.X - width / 2 + (int)(width * 0.004);
                    if (firstChecks.Count == 3 && firstChecks[0].Length > 0) { startX += width / 8; }
                    int overWid = (int)(width / (4.1 * dpiScaling));
                    int startY = (int)(center.Y / dpiScaling - 20 * screenScaling * uiScaling);
                    int partNumber = 0;
                    for (int i = 0; i < firstChecks.Count; i++)
                    {
                        string part = firstChecks[i];
                        if (part.Length > 10)
                        {
                            string correctName = Main.dataBase.GetPartName(part, out firstProximity[i]);
                            JObject job = Main.dataBase.marketData.GetValue(correctName).ToObject<JObject>();
                            string plat = job["plat"].ToObject<string>();
                            string ducats = job["ducats"].ToObject<string>();
                            string volume = job["volume"].ToObject<string>();
                            bool vaulted = Main.dataBase.IsPartVaulted(correctName);
                            string partsOwned = Main.dataBase.PartsOwned(correctName);

                            Main.RunOnUIThread(() =>
                            {
                                if (Settings.isOverlaySelected)
                                {
                                    Main.overlays[partNumber].LoadTextData(correctName, plat, ducats, volume, vaulted, partsOwned);
                                    Main.overlays[partNumber].Resize(overWid);
                                    Main.overlays[partNumber].Display((int)((startX + width / 4 * partNumber) / dpiScaling), startY);
                                } else
                                {
                                    Main.window.loadTextData(correctName, plat, ducats, volume, vaulted, partsOwned, partNumber);
                                }
                            });

                        }
                        partNumber++;
                    }
                    var end = watch.ElapsedMilliseconds;
                    Main.StatusUpdate("Completed Processing (" + (end - start) + "ms)", 0);
                    if (complete != null)
                    {
                        complete.Wait();
                        end = watch.ElapsedMilliseconds;
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

                bigScreenshot.Dispose();
                bigScreenshot = null;
                partialScreenshot.Dispose();
                partialScreenshot = null;
                partialScreenshotFiltered.Dispose();
                partialScreenshotFiltered = null;
            }
            catch (Exception ex)
            {
                Main.AddLog(ex.ToString());
                Main.StatusUpdate("ERROR OCCURED DURING PROCESSING", 1);
            }
            processingActive = false;

        }

        private const double ERROR_DETECTION_THRESH = 0.25;
        private static bool CheckIfError()
        {
            for(int i = 0; i < firstChecks.Count; i++)
                if(firstProximity[i] > ERROR_DETECTION_THRESH * firstChecks[i].Length && 
                  (secondProximity[i] == -1 || secondProximity[i] > ERROR_DETECTION_THRESH * secondChecks[i].Length))
                    return true;

            return false;
        }

        public static void SlowSecondProcess()
        {
            Bitmap newFilter = ScaleUpAndFilter(partialScreenshot, activeTheme);
            partialScreenshotExpanded.Save(Main.appPath + @"\Debug\PartShotUpscaled " + timestamp + ".png");
            newFilter.Save(Main.appPath + @"\Debug\PartShotUpscaledFiltered " + timestamp + ".png");
            secondChecks = SeparatePlayers(newFilter, secondEngine);
            List<int> comparisions = new List<int>();

            if (firstChecks.Count != secondChecks.Count)
            {
                //Whelp, we fucked bois
                Main.AddLog("Second check didn't find the same amount of part names");
                return;
            }

            int partNumber = 0;
            for (int i = 0; i < firstChecks.Count; i++)
            {
                string first = firstChecks[i];
                if (first.Length > 10)
                {
                    string second = secondChecks[i];
                    string secondName = Main.dataBase.GetPartName(second, out secondProximity[i]);
                    if (secondProximity[i] < firstProximity[i])
                    {
                        JObject job = Main.dataBase.marketData.GetValue(secondName).ToObject<JObject>();
                        string plat = job["plat"].ToObject<string>();
                        string ducats = job["ducats"].ToObject<string>();
                        string volume = job["volume"].ToObject<string>();
                        bool vaulted = Main.dataBase.IsPartVaulted(secondName);
                        string partsOwned = Main.dataBase.PartsOwned(secondName);

                        Main.RunOnUIThread(() =>
                        {
                            if (Settings.isOverlaySelected)
                            {
                                Main.overlays[partNumber].LoadTextData(secondName, plat, ducats, volume, vaulted, partsOwned);
                            } else
                            {
                                Main.window.loadTextData(secondName, plat, ducats, volume, vaulted, partsOwned, partNumber, false);
                            }
                        });

                    }
                    partNumber++;
                }
            }
        }

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
                    graphics.CopyFromScreen(profileX, profileY, profileX, profileY, profileSize, CopyPixelOperation.SourceCopy);
                    graphics.CopyFromScreen(fissureX, fissureY, fissureX, fissureY, fissureSize, CopyPixelOperation.SourceCopy);
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
                    graphics.CopyFromScreen(fissureX, fissureY, fissureX, fissureY, fissureSize, CopyPixelOperation.SourceCopy);
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

        private static WFtheme GetClosestTheme(Color clr, out int threshold)
        {

            threshold = 999;
            WFtheme minTheme = WFtheme.CORPUS;

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
            return minTheme;
        }


        private static bool ColorThreshold(Color test, Color thresh, int threshold = 10)
        {
            return (Math.Abs(test.R - thresh.R) < threshold) && (Math.Abs(test.G - thresh.G) < threshold) && (Math.Abs(test.B - thresh.B) < threshold);
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
                case WFtheme.VITRUVIAN:
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 4 && test.GetSaturation() >= 0.25 && test.GetBrightness() >= 0.42;
                case WFtheme.LOTUS:
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 5 && test.GetSaturation() >= 0.65 && Math.Abs(test.GetBrightness() - primary.GetBrightness()) <= 0.1
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetBrightness() >= 0.65);
                case WFtheme.OROKIN:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 5 && test.GetBrightness() <= 0.42 && test.GetSaturation() >= 0.1)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 5 && test.GetBrightness() <= 0.5 && test.GetBrightness() >= 0.25 && test.GetSaturation() >= 0.25);
                case WFtheme.STALKER:
                    return ((Math.Abs(test.GetHue() - primary.GetHue()) < 4 && test.GetSaturation() >= 0.5)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetSaturation() >= 0.65)) && test.GetBrightness() >= 0.20;
                case WFtheme.CORPUS:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 4 && test.GetBrightness() >= 0.35 && test.GetSaturation() >= 0.45)
                         || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetBrightness() >= 0.30 && test.GetSaturation() >= 0.35);
                case WFtheme.EQUINOX:
                    return test.GetSaturation() <= 0.1 && test.GetBrightness() >= 0.52;
                case WFtheme.DARK_LOTUS:
                    return (Math.Abs(test.GetHue() - secondary.GetHue()) < 20 && test.GetBrightness() >= 0.42 && test.GetBrightness() <= 0.55 && test.GetSaturation() <= 0.20 && test.GetSaturation() >= 0.07)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetBrightness() >= 0.50 && test.GetSaturation() >= 0.20);
                case WFtheme.FORTUNA:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 4 || Math.Abs(test.GetHue() - secondary.GetHue()) < 3) && test.GetBrightness() >= 0.25 && test.GetSaturation() >= 0.20;
                case WFtheme.HIGH_CONTRAST:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 4 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2) && test.GetSaturation() >= 0.75 && test.GetBrightness() >= 0.25; // || Math.Abs(test.GetHue() - secondary.GetHue()) < 2;
                case WFtheme.LEGACY:
                    return (test.GetBrightness() >= 0.75 && test.GetSaturation() <= 0.2)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 6 && test.GetBrightness() >= 0.5 && test.GetSaturation() >= 0.5);
                case WFtheme.NIDUS:
                    return (Math.Abs(test.GetHue() - (primary.GetHue() + 7.5)) < 10 && test.GetSaturation() >= 0.31)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 15 && test.GetSaturation() >= 0.55);
                case WFtheme.TENNO:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 4 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2) && test.GetSaturation() >= 0.3 && test.GetBrightness() <= 0.6;
                case WFtheme.BARUUK:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 2) && test.GetSaturation() > 0.25 && test.GetBrightness() > 0.5;
                case WFtheme.GRINEER:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 6 && test.GetBrightness() > 0.3)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 6 && test.GetBrightness() > 0.55);
                default:
                    // This shouldn't be ran
                    //   Only for initial testing
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 2 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2;
            }
        }

        private static Bitmap ScaleUpAndFilter(Bitmap image, WFtheme active)
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

            Bitmap filtered = new Bitmap(partialScreenshotExpanded.Width, partialScreenshotExpanded.Height);

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

        private static Bitmap FilterPartNames(Bitmap image, WFtheme active)
        {
            int width = (int)(pixleRewardWidth * screenScaling * uiScaling);
            int lineHeight = (int)(pixelRewardLineHeight * screenScaling * uiScaling);
            int left = (image.Width / 2) - (width / 2);
            int top = (image.Height / 2) - (int)(pixleRewardYDisplay * screenScaling * uiScaling) + (int)(pixleRewardHeight * screenScaling * uiScaling) - lineHeight;

            partialScreenshot = new Bitmap(width + 10, lineHeight + 10);

            Color clr;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < lineHeight; y++)
                {
                    clr = image.GetPixel(left + x, top + y);
                    partialScreenshot.SetPixel(x + 5, y + 5, clr);
                }
            }

            Bitmap filtered = new Bitmap(partialScreenshot.Width, partialScreenshot.Height);
            for (int x = 0; x < filtered.Width; x++)
            {
                for (int y = 0; y < filtered.Height; y++)
                {
                    clr = partialScreenshot.GetPixel(x, y);
                    if (ThemeThresholdFilter(clr, active))
                        filtered.SetPixel(x, y, Color.Black);
                    else
                        filtered.SetPixel(x, y, Color.White);
                }
            }

            return filtered;
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

            if (window == null)
            {
                window = Screen.PrimaryScreen.Bounds;
                center = new Point(window.Width / 2, window.Height / 2);
            }

            int width = window.Width * (int)dpiScaling;
            int height = window.Height * (int)dpiScaling;

            Bitmap image = new Bitmap(width, height);
            Size FullscreenSize = new Size(image.Width, image.Height);
            using (Graphics graphics = Graphics.FromImage(image))
                graphics.CopyFromScreen(window.Left, window.Top, 0, 0, FullscreenSize, CopyPixelOperation.SourceCopy);

            return image;
        }

        //public static Boolean verifyFocus() { // Returns True if warframe is in focuse, False if not
        //	_ = Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out uint processID);
        //	try {
        //		if (processID == Warframe.Id || Settings.debug) { return true; } else {
        //			Main.AddLog("Warframe is not focused");
        //			Main.StatusUpdate("Warframe is out of focus", 2);
        //			return false;
        //		}
        //	}
        //	catch (Exception ex) {
        //		Console.WriteLine(Warframe.ToString());
        //		Console.WriteLine(ex.ToString());
        //		return false;
        //	}
        //}

        public static bool VerifyWarframe()
        {
            if (Warframe != null && !Warframe.HasExited) { return true; }
            foreach (Process process in Process.GetProcesses())
            {
                if (process.MainWindowTitle == "Warframe")
                {
                    HandleRef = new HandleRef(process, process.MainWindowHandle);
                    Warframe = process;
                    Main.AddLog("Found Warframe in list of current active processes");
                    return true;
                }
            }
            if (!Settings.debug)
            {
                Main.AddLog("Unable to detect Warframe in list of current active processes");
                Main.StatusUpdate("Unable to detect Warframe process", 1);
            }
            return false;

        }

        private static void RefreshDPIScaling()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                dpiScaling = graphics.DpiX / 96; //assuming that y and x axis dpi scaling will be uniform. So only need to check one value

            uiScaling = Settings.scaling / 100.0;
        }

        public static void UpdateWindow(Bitmap image = null)
        {
            RefreshDPIScaling();
            if (image != null)
            {
                int width = image?.Width ?? Screen.PrimaryScreen.Bounds.Width * (int)dpiScaling;
                int height = image?.Height ?? Screen.PrimaryScreen.Bounds.Height * (int)dpiScaling;
                window = new Rectangle(0, 0, width, height);
                center = new Point(window.Width / 2, window.Height / 2);

                if (window.Width * 9 > window.Height * 16)  // image is less than 16:9 aspect
                    screenScaling = window.Height / 1080.0;
                else
                    screenScaling = window.Width / 1920.0; //image is higher than 16:9 aspect
                return;
            }


            if (!VerifyWarframe())
                return;

            if (!Win32.GetWindowRect(HandleRef, out Win32.r osRect))
            { // get window size of warframe
                if (Settings.debug)
                { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
                    Main.AddLog("No warframe detected, thus using center of screen");
                    int width = Screen.PrimaryScreen.Bounds.Width * (int)dpiScaling;
                    int height = Screen.PrimaryScreen.Bounds.Height * (int)dpiScaling;
                    window = new Rectangle(0, 0, width, height);
                    center = new Point(window.Width / 2, window.Height / 2);
                    if (window.Width * 9 > window.Height * 16)  // image is less than 16:9 aspect
                        screenScaling = window.Height / 1080.0;
                    else
                        screenScaling = window.Width / 1920.0; //image is higher than 16:9 aspect
                    return;
                } else
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
            } else if (window.Left != osRect.Left || window.Right != osRect.Right || window.Top != osRect.Top || window.Bottom != osRect.Bottom)
            { // checks if old window size is the right size if not change it
                window = new Rectangle(osRect.Left, osRect.Top, osRect.Right - osRect.Left, osRect.Bottom - osRect.Top); // get Rectangle out of rect
                                                                                                                         // Rectangle is (x, y, width, height) RECT is (x, y, x+width, y+height) 
                Main.AddLog("Window size detected: " + window.ToString());
                int GWL_style = -16;
                uint WS_BORDER = 0x00800000;
                uint WS_POPUP = 0x80000000;


                uint styles = Win32.GetWindowLongPtr(HandleRef, GWL_style);
                if ((styles & WS_POPUP) != 0)
                {
                    // Borderless, don't do anything
                    currentStyle = WindowStyle.BORDERLESS;
                    Main.AddLog("Borderless detected (0x" + styles.ToString("X8") + ")");
                } else if ((styles & WS_BORDER) != 0)
                {
                    // Windowed, adjust for thicc border
                    window = new Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38);
                    Main.AddLog("Windowed detected (0x" + styles.ToString("X8") + "), adjusting window to: " + window.ToString());
                    currentStyle = WindowStyle.WINDOWED;
                } else
                {
                    // Assume Fullscreen, don't do anything
                    Main.AddLog("Fullscreen detected (0x" + styles.ToString("X8") + ")");
                    currentStyle = WindowStyle.FULLSCREEN;
                }
                center = new Point(window.Width / 2, window.Height / 2);
                if (window.Width * 9 > window.Height * 16)  // image is less than 16:9 aspect
                    screenScaling = window.Height / 1080.0;
                else
                    screenScaling = window.Width / 1920.0; //image is higher than 16:9 aspect
            }
        }

        // WIP - please change accordingly to new logic Should be canned? Old system
        public static void ParseFile(string filename)
        {
            Main.AddLog("Parsing file: " + filename);
            Bitmap debugFile = new Bitmap(filename);
            ProcessRewardScreen(debugFile);
        }
    }
}
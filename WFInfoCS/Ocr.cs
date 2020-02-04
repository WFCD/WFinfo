﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
            DARK_LOTUS
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
        private static Process Warframe = null;

        private static Point center;
        public static Rectangle window;

        //public static float dpi;
        //private static double ScreenScaling; // Additional to settings.scaling this is used to calculate any widescreen or 4:3 aspect content.
        //private static double TotalScaling;

        public static double DPI_Scaling;
        public static double UI_Scaling;
        public static double Screen_Scaling;



        public static TesseractEngine limitedEngine = new TesseractEngine("", "englimited")
        {
            DefaultPageSegMode = PageSegMode.SingleBlock
        };
        public static TesseractEngine bestEngine = new TesseractEngine("", "engbest")
        {
            DefaultPageSegMode = PageSegMode.SingleBlock
        };
        public static Regex RE = new Regex("[^a-z&//]", RegexOptions.IgnoreCase | RegexOptions.Compiled);



        // Pixel measurements for reward screen @ 1920 x 1080 with 100% scale https://docs.google.com/drawings/d/1Qgs7FU2w1qzezMK-G1u9gMTsQZnDKYTEU36UPakNRJQ/edit
        public static int pixRwrdWid = 968;
        public static int pixRwrdHei = 235;
        public static int pixRwrdYDisp = 316;
        public static int pixRwrdLineHei = 44;
        public static int pixRwrdLineWid = 240;

        // Pixel measurement for player bars for player count
        //   Width is same as pixRwrdWid
        // public static int pixRareWid = pixRwrdWid;
        //   Height is always 1px
        // public static int pixRareHei = 1;
        //   Box is centered horizontally
        // public static int pixRareXDisp = ???;
        public static int pixRareYDisp = 58;
        public static int pixOverlayPos = 30;

        public static int pixProfWid = 48;
        public static int pixProfTotWid = 192;
        // Height is always 1px
        // public static int pixProfHei = 1;
        public static int pixProfXDisp = 93;
        public static int pixProfYDisp = 87;
        public static double pixProfXSpecial = 117;
        public static double pixProfYSpecial = 87;

        // Pixel measurements for detecting reward screen
        public static int pixFissWid = 354;
        public static int pixFissHei = 45;
        public static int pixFissXDisp = 285;
        public static int pixFissYDisp = 43;


        private static bool ERROR_DETECTED = false;
        private static bool PROCESSING_ACTIVE = false;

        private static Bitmap bigScreenshot;
        private static Bitmap partialScreenshot;
        private static Bitmap partialScreenshotFiltered;



        internal static void ProcessRewardScreen(Bitmap file = null)
        {
            if (PROCESSING_ACTIVE)
            {
                Main.StatusUpdate("Already Processing Reward Screen", 2);
                return;
            }
            PROCESSING_ACTIVE = true;
            Main.AddLog("----  Triggered Reward Screen Processing  ------------------------------------------------------------------");

            Main.StatusUpdate("Processing...", 0);

            var watch = Stopwatch.StartNew();
            long start = watch.ElapsedMilliseconds;

            // Look at me mom, I'm doing fancy shit
            bigScreenshot = file ?? CaptureScreenshot();

            if (bigScreenshot.Width * 9 > bigScreenshot.Height * 16)  // image is less than 16:9 aspect
                Screen_Scaling = bigScreenshot.Height / 1080.0;
            else
                Screen_Scaling = bigScreenshot.Width / 1920.0; //image is higher than 16:9 aspect

            UI_Scaling = Settings.scaling / 100.0;

            Main.AddLog("Scaling values: Screen_Scaling = " + (Screen_Scaling * 100).ToString("F2") + "%, DPI_Scaling = " + (DPI_Scaling * 100).ToString("F2") + "%, UI_Scaling = " + (UI_Scaling * 100).ToString("F0") + "%");


            // Get that theme
            WFtheme active = GetTheme(bigScreenshot);


            // Get the part box and filter it
            partialScreenshotFiltered = FilterPartNames(bigScreenshot, active);
            List<string> players = SeparatePlayers(partialScreenshotFiltered);
            int startX = center.X - partialScreenshotFiltered.Width / 2 + (int)(partialScreenshotFiltered.Width * 0.004);
            if (players.Count == 3 && players[0].Length > 0) { startX += partialScreenshotFiltered.Width / 8; }
            int overWid = (int)(partialScreenshotFiltered.Width / (4.1 * DPI_Scaling));
            int startY = (int)(center.Y / DPI_Scaling - 20 * Screen_Scaling * UI_Scaling);


            int partNumber = 0;
            foreach (string part in players)
            {
                if (part.Length > 10)
                {
                    string correctName = Main.dataBase.GetPartName(part, out _);
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
                            Main.overlays[partNumber].Display((int)((startX + partialScreenshotFiltered.Width / 4 * partNumber) / DPI_Scaling), startY);
                        }
                        else
                        {
                            Main.window.loadTextData(correctName, plat, ducats, volume, vaulted, partsOwned, partNumber);
                        }
                    });

                }
                partNumber++;
            }


            var end = watch.ElapsedMilliseconds;
            Main.AddLog(("----  Total Processing Time " + (end - start) + " ms  ------------------------------------------------------------------------------------------").Substring(0, 108));

            watch.Stop();

            Main.StatusUpdate("Completed Processing (" + (end - start) + "ms)", 0);

            Directory.CreateDirectory(Main.appPath + @"\Debug");

            string[] files = Directory.GetFiles(Main.appPath + @"\Debug\", "FullScreenShot *");
            for (int i = 0; i < files.Length - 4; i++)
                File.Delete(files[i]);

            files = Directory.GetFiles(Main.appPath + @"\Debug\", "PartBox *");
            for (int i = 0; i < files.Length - 4; i++)
                File.Delete(files[i]);

            files = Directory.GetFiles(Main.appPath + @"\Debug\", "PartBoxFilter *");
            for (int i = 0; i < files.Length - 4; i++)
                File.Delete(files[i]);

            bigScreenshot.Save(Main.appPath + @"\Debug\FullScreenShot " + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff") + ".png");
            partialScreenshot.Save(Main.appPath + @"\Debug\PartBox " + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff") + ".png");
            partialScreenshotFiltered.Save(Main.appPath + @"\Debug\PartBoxFilter " + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff") + ".png");

            bigScreenshot.Dispose();
            bigScreenshot = null;
            partialScreenshot.Dispose();
            partialScreenshot = null;
            partialScreenshotFiltered.Dispose();
            partialScreenshotFiltered = null;

            ERROR_DETECTED = false;
            PROCESSING_ACTIVE = false;
        }

        private static WFtheme GetTheme(Bitmap image)
        {
            // Tests Scaling from 40% to 120%
            double scalingMod = Screen_Scaling * 0.4;

            int startX = (int)(pixProfXSpecial * scalingMod);
            int startY = (int)(pixProfYSpecial * scalingMod);
            int endX = (int)(pixProfXSpecial * scalingMod * 3);
            int endY = (int)(pixProfYSpecial * scalingMod * 3);
            int height = endY - startY;
            int width = endX - startX;

            int closestThresh = 999;
            WFtheme closestTheme = WFtheme.CORPUS;
            double estimatedScaling = 0;
            Color closestColor = ThemePrimary[0];
            Color clr;

            //using (Bitmap bmp = new Bitmap(endX - startX, endY - startY))
            for (int y = 1; y <= height; y++)
            {
                int coorY = endY - y;
                int coorX = endX - (int)(1.0 * width * y / height);

                clr = image.GetPixel(coorX, coorY);

                int minThresh = 999;
                WFtheme minTheme = WFtheme.CORPUS;

                foreach (WFtheme theme in (WFtheme[])Enum.GetValues(typeof(WFtheme)))
                {
                    Color themeColor = ThemePrimary[(int)theme];
                    int tempThresh = ColorDifference(clr, themeColor);
                    if (tempThresh < minThresh)
                    {
                        minThresh = tempThresh;
                        minTheme = theme;
                    }
                }

                if (estimatedScaling < .5 && minThresh < 10)
                    estimatedScaling = (coorX / pixProfXSpecial) / Screen_Scaling;
                if (minThresh < closestThresh)
                {
                    closestThresh = minThresh;
                    closestTheme = minTheme;
                    closestColor = clr;
                }
            }
            /*if (estimatedScaling > .5)
            {
                Main.AddLog("ESTIMATED SCALING: " + (int)(100 * estimatedScaling) + "%");
                Main.AddLog("USER INPUT SCALING: " + Settings.Scaling + "%");
            }*/
            Main.AddLog("CLOSEST THEME(" + closestThresh + "): " + closestTheme.ToString() + " - (" + closestColor.R + "," + closestColor.G + "," + closestColor.B + ")");
            return closestTheme;
        }

        private static bool ColorThreshold(Color test, Color thresh, int threshold = 10)
        {
            return (Math.Abs(test.R - thresh.R) < threshold) && (Math.Abs(test.G - thresh.G) < threshold) && (Math.Abs(test.B - thresh.B) < threshold);
        }

        private static int ColorDifference(Color test, Color thresh)
        {
            return Math.Abs(test.R - thresh.R) + Math.Abs(test.G - thresh.G) + Math.Abs(test.B - thresh.B);
        }

        private static bool ThemeThresholdFilter(Color test, WFtheme theme)
        {
            Color primary = ThemePrimary[(int)theme];
            Color secondary = ThemeSecondary[(int)theme];

            switch (theme)
            {
                case WFtheme.VITRUVIAN:
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 2 && test.GetSaturation() >= 0.25 && test.GetBrightness() >= 0.42;
                case WFtheme.LOTUS:
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 3 && test.GetSaturation() >= 0.65 && Math.Abs(test.GetBrightness() - primary.GetBrightness()) <= 0.1
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 2 && test.GetBrightness() >= 0.65);
                case WFtheme.OROKIN:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 5 && test.GetBrightness() <= 0.42 && test.GetSaturation() >= 0.1)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 5 && test.GetBrightness() <= 0.5 && test.GetBrightness() >= 0.25 && test.GetSaturation() >= 0.25);
                case WFtheme.STALKER:
                    return ((Math.Abs(test.GetHue() - primary.GetHue()) < 2 && test.GetSaturation() >= 0.5)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 2 && test.GetSaturation() >= 0.65)) && test.GetBrightness() >= 0.25;
                case WFtheme.CORPUS:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 2 && test.GetBrightness() >= 0.35 && test.GetSaturation() >= 0.45)
                         || (Math.Abs(test.GetHue() - secondary.GetHue()) < 2 && test.GetBrightness() >= 0.30 && test.GetSaturation() >= 0.35);
                case WFtheme.EQUINOX:
                    return test.GetSaturation() <= 0.1 && test.GetBrightness() >= 0.52;
                case WFtheme.DARK_LOTUS:
                    return (Math.Abs(test.GetHue() - secondary.GetHue()) < 20 && test.GetBrightness() >= 0.42 && test.GetBrightness() <= 0.55 && test.GetSaturation() <= 0.20 && test.GetSaturation() >= 0.07)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 2 && test.GetBrightness() >= 0.50 && test.GetSaturation() >= 0.20);
                case WFtheme.FORTUNA:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 2 || Math.Abs(test.GetHue() - secondary.GetHue()) < 3) && test.GetBrightness() >= 0.25 && test.GetSaturation() >= 0.20;
                case WFtheme.HIGH_CONTRAST:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 2 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2) && test.GetSaturation() >= 0.75 && test.GetBrightness() >= 0.25; // || Math.Abs(test.GetHue() - secondary.GetHue()) < 2;
                case WFtheme.LEGACY:
                    return (test.GetBrightness() >= 0.75 && test.GetSaturation() <= 0.2)
                        || (Math.Abs(test.GetHue() - secondary.GetHue()) < 4 && test.GetBrightness() >= 0.5 && test.GetSaturation() >= 0.5);
                case WFtheme.NIDUS:
                    return (Math.Abs(test.GetHue() - (primary.GetHue() + 7.5)) < 8 && test.GetSaturation() >= 0.31)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 15 && test.GetSaturation() >= 0.55);
                case WFtheme.TENNO:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 2 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2) && test.GetSaturation() >= 0.3 && test.GetBrightness() <= 0.6;
                case WFtheme.BARUUK:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 1.2) && test.GetSaturation() > 0.25 && test.GetBrightness() > 0.5;
                case WFtheme.GRINEER:
                    return (Math.Abs(test.GetHue() - primary.GetHue()) < 5 && test.GetBrightness() > 0.3)
                    || (Math.Abs(test.GetHue() - secondary.GetHue()) < 5 && test.GetBrightness() > 0.55);
                default:
                    // This shouldn't be ran
                    //   Only for initial testing
                    return Math.Abs(test.GetHue() - primary.GetHue()) < 2 || Math.Abs(test.GetHue() - secondary.GetHue()) < 2;
            }
        }

        private static Bitmap FilterPartNames(Bitmap image, WFtheme active)
        {
            int width = (int)(pixRwrdWid * Screen_Scaling * UI_Scaling);
            int lineHeight = (int)(pixRwrdLineHei * Screen_Scaling * UI_Scaling);
            int left = (image.Width / 2) - (width / 2);
            int top = (image.Height / 2) - (int)(pixRwrdYDisp * Screen_Scaling * UI_Scaling) + (int)(pixRwrdHei * Screen_Scaling * UI_Scaling) - lineHeight;

            partialScreenshotFiltered = new Bitmap(width + 10, lineHeight + 10);
            partialScreenshot = new Bitmap(width + 10, lineHeight + 10);

            for (int x = 0; x < partialScreenshotFiltered.Width; x++)
                for (int y = 0; y < partialScreenshotFiltered.Height; y++)
                    partialScreenshotFiltered.SetPixel(x, y, Color.White);


            Color clr;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < lineHeight; y++)
                {
                    clr = image.GetPixel(left + x, top + y);
                    partialScreenshot.SetPixel(x + 5, y + 5, clr);

                    if (ThemeThresholdFilter(clr, active))
                        partialScreenshotFiltered.SetPixel(x + 5, y + 5, Color.Black);
                }

            return partialScreenshotFiltered;
        }

        internal static List<string> SeparatePlayers(Bitmap image)
        {

            // Values to determine whether there's an even or odd number of players
            int hei = (int)(pixRwrdLineHei * Screen_Scaling * UI_Scaling / 2);
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
            using (Page page = bestEngine.Process(image))
            {
                using (var iter = page.GetIterator())
                {
                    Tesseract.Rect outRect;
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
            updateWindow();

            if (window == null)
            {
                window = Screen.PrimaryScreen.Bounds;
                center = new Point(window.Width / 2, window.Height / 2);
            }

            int width = window.Width * (int)DPI_Scaling;
            int height = window.Height * (int)DPI_Scaling;

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

        public static bool verifyWarframe()
        {
            if (Warframe != null && !Warframe.HasExited) { return true; }
            foreach (Process process in Process.GetProcesses())
            {
                if (process.MainWindowTitle == "Warframe")
                {
                    HandleRef = new HandleRef(process, process.MainWindowHandle);
                    Warframe = process;
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

        private static void refreshScaling()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                DPI_Scaling = graphics.DpiX / 96; //assuming that y and x axis dpi scaling will be uniform. So only need to check one value
        }

        public static void updateWindow(Bitmap image = null)
        {
            refreshScaling();
            if (image != null)
            {
                int width = image?.Width ?? Screen.PrimaryScreen.Bounds.Width * (int)DPI_Scaling;
                int height = image?.Height ?? Screen.PrimaryScreen.Bounds.Height * (int)DPI_Scaling;
                window = new Rectangle(0, 0, width, height);
                center = new Point(window.Width / 2, window.Height / 2);
                return;
            }


            if (!verifyWarframe())
                return;

            if (!Win32.GetWindowRect(HandleRef, out Win32.r osRect))
            { // get window size of warframe
                if (Settings.debug)
                { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
                    Main.AddLog("No warframe detected, thus using center of screen");
                    int width = Screen.PrimaryScreen.Bounds.Width * (int)DPI_Scaling;
                    int height = Screen.PrimaryScreen.Bounds.Height * (int)DPI_Scaling;
                    window = new Rectangle(0, 0, width, height);
                    center = new Point(window.Width / 2, window.Height / 2);
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
            else if (window.Left != osRect.Left || window.Right != osRect.Right || window.Top != osRect.Top || window.Bottom != osRect.Bottom)
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
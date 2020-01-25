using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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


        private static double TotalScaling;
        public static WindowStyle currentStyle;
        public enum WindowStyle
        {
            FULLSCREEN,
            BORDERLESS,
            WINDOWED
        }
        public static HandleRef HandleRef { get; private set; }
        public static float dpi { get; set; }
        private static Process Warframe = null;
        private static Point center;
        public static Rectangle window { get; set; }
        private HandleRef handelRef;
        private static double xPrecentFirstReward = 379;
        private static double xPrecentSecondReward = 184;
        private static double yPrecentReward = 75;
        private static Rectangle firstRewardRectangle;
        private static Rectangle secondRewardRectangle;
        private static double screenScaling; // Additional to settings.scaling this is used to calculate any widescreen or 4:3 aspect content.
                                             //todo  implemenet Tesseract
                                             //      implemenet pre-prossesing




        // Pixel measurements for reward screen @ 1920 x 1080 with 100% scale https://docs.google.com/drawings/d/1Qgs7FU2w1qzezMK-G1u9gMTsQZnDKYTEU36UPakNRJQ/edit
        public static int pixRwrdWid = 972;
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
        public static Double pixProfXSpecial = 117;
        public static Double pixProfYSpecial = 87;

        // Pixel measurements for detecting reward screen
        public static int pixFissWid = 354;
        public static int pixFissHei = 45;
        public static int pixFissXDisp = 285;
        public static int pixFissYDisp = 43;


        internal static void ProcessRewardScreen(Bitmap file = null)
        {
            // Look at me mom, I'm doing fancy shit
            Bitmap image = file ?? CaptureScreenshot();

            // Get that scaling
            screenScaling = Settings.Scaling / 100 * dpi;
            if (image.Width / image.Height > 16 / 9)  // image is less than 16:9 aspect
                screenScaling *= image.Height / 1080;
            else
                screenScaling *= image.Width / 1920; //image is higher than 16:9 aspect

            // Get that theme
            WFtheme active = GetTheme(image);

            // Get the part box and filter it
            Bitmap partBox = FilterPartNames(image, active);
            int playerCount = countRewards(partBox);

            partBox.Save(Main.appPath + @"\Debug\PartBoxDebug " + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
            image.Save(Main.appPath + @"\Debug\FullScreenShotDebug " + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
        }

        private static WFtheme GetTheme(Bitmap image)
        {
            Double scalingMod = screenScaling * 40 / Settings.Scaling;

            int startX = (int)(pixProfXSpecial * scalingMod);
            int startY = (int)(pixProfYSpecial * scalingMod);
            int endX = (int)(pixProfXSpecial * scalingMod * 3);
            int endY = (int)(pixProfYSpecial * scalingMod * 3);

            int closestThresh = 999;
            WFtheme closestTheme = WFtheme.CORPUS;
            Double estimatedScaling = 0;
            Color closestColor = ThemePrimary[0];
            Color clr;

            using (Bitmap bmp = new Bitmap(endX - startX, endY - startY))
            {
                using (Graphics graph = Graphics.FromImage(bmp))
                    graph.CopyFromScreen(window.X + startX, window.Y + startY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                for (int y = 1; y <= bmp.Height; y++)
                {
                    int newY = bmp.Height - y;
                    int newX = (bmp.Width * newY) / bmp.Height;

                    clr = bmp.GetPixel(newX, newY);
                    image.SetPixel(startX + newX - 1, startY + newY, Color.Red);
                    image.SetPixel(startX + newX + 1, startY + newY, Color.Red);

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
                        estimatedScaling = (startX + newX) / (pixProfXSpecial * screenScaling);

                    if (minThresh < closestThresh)
                    {
                        closestThresh = minThresh;
                        closestTheme = minTheme;
                        closestColor = clr;
                    }
                }
            }
            if (estimatedScaling > .5)
            {
                Main.AddLog("ESTIMATED SCALING: " + (int)(100 * estimatedScaling) + "%");
                Main.AddLog("USER INPUT SCALING: " + Settings.Scaling + "%");
            }
            Main.AddLog("CLOSEST THEME(" + closestThresh + "): " + closestTheme.ToString() + " - " + closestColor.ToString());
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
            Color filter = ThemePrimary[(int)theme];

            switch (theme)
            {
                case WFtheme.VITRUVIAN:
                    return Math.Abs(test.GetHue() - filter.GetHue()) < 2 && test.GetSaturation() >= 0.25 && test.GetBrightness() >= 0.42;
                case WFtheme.LOTUS:
                    return Math.Abs(test.GetHue() - filter.GetHue()) < 3 && test.GetSaturation() >= 0.65 && Math.Abs(test.GetBrightness() - filter.GetBrightness()) <= 0.1;
                default:
                    return ColorThreshold(test, filter);
            }
        }

        private static Bitmap FilterPartNames(Bitmap image, WFtheme active)
        {
            int width = (int)(pixRwrdWid * screenScaling);
            int lineHeight = (int)(pixRwrdLineHei * screenScaling);
            int left = (image.Width / 2) - (width / 2);
            int top = (image.Height / 2) - (int)(pixRwrdYDisp * screenScaling) + (int)(pixRwrdHei * screenScaling) - lineHeight;

            Color clr;

            Bitmap ret = new Bitmap(width + 10, lineHeight + 10);
            Bitmap ret2 = new Bitmap(width + 10, lineHeight + 10);
            for (int x = 0; x < ret.Width; x++)
                for (int y = 0; y < ret.Height; y++)
                {
                    ret.SetPixel(x, y, Color.White);
                    ret2.SetPixel(x, y, Color.White);
                }


            var csv = new StringBuilder();
            csv.Append("X,Y,R,G,B,Hue,Saturation,Brightness\n");
            for (int x = 0; x < width; x++)
                for (int y = 0; y < lineHeight; y++)
                {
                    clr = image.GetPixel(left + x, top + y);
                    if (ThemeThresholdFilter(clr, active))
                    {
                        csv.Append((left + x) + ", " + (top + y) + ", " + clr.R + ", " + clr.G + ", " + clr.B + ", " + clr.GetHue() + ", " + clr.GetSaturation() + ", " + clr.GetBrightness() + "\n");
                        ret.SetPixel(x + 5, y + 5, clr);
                        ret2.SetPixel(x + 5, y + 5, Color.Black);
                    }
                }
            ret2.Save(Main.appPath + @"\Debug\PartBox2Debug " + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
            File.WriteAllText(Main.appPath + @"\Debug\pixels.csv", csv.ToString());
            return ret;
        }
        internal static int countRewards(Bitmap image)
        {
            // Firstly check at the first possible possition with 4 rewards, which is at Width = 0.3097 % and Height = 0.4437 %
            // If not found, check first possible possition with 2 rewards, which is 0.4218 %
            // If also not found, there are 3 rewards

            /*screenScaling = Settings.Scaling / 100 * dpi;
			if (image.Width / image.Height > 16 / 9)  // image is less than 16:9 aspect
				screenScaling *= image.Height / 1080;
            else
                screenScaling *= image.Width / 1920; //image is higher than 16:9 aspect

			center = new Point(image.Width / 2, image.Height / 2);




			firstRewardRectangle = getAdjustedRectangle((int)(xPrecentFirstReward * screenScaling), (int)(yPrecentReward * screenScaling), gemBox);
			secondRewardRectangle = getAdjustedRectangle((int)(xPrecentSecondReward * screenScaling), (int)(yPrecentReward * screenScaling), gemBox);

			Bitmap firstReward = image.Clone(firstRewardRectangle, image.PixelFormat);
			Bitmap secondReward = image.Clone(secondRewardRectangle, image.PixelFormat);

			doDebugDrawing(image);
			image.Dispose();
			if (verrifyReward(firstReward)) { 
				Console.WriteLine("4 rewards");
				return 4;
			} else if (verrifyReward(secondReward)) { 
				Console.WriteLine("2 rewards");
				return 2;
			} else { 
				Console.WriteLine("3 rewards");
				return 3;
			}*/
            return 0;
        }

        private static Rectangle getAdjustedRectangle(int x, int y, Size size)
        { //adjust the rectangle to capture from the center of the screen instead of from origin.
            int left = center.X - x;
            int top = center.Y - y;
            Console.WriteLine("Center cords: " + center.ToString());
            Console.WriteLine("Calculated position of rectangle " + left + "," + top);
            Console.WriteLine("pixels away from center " + x + "," + y);


            return new Rectangle(left, top, size.Width, size.Height);
        }

        private static Boolean verrifyReward(Bitmap image)
        {
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    for (int color = 0; color < 3; color++)
                    {
                        if (ColorThreshold(image.GetPixel(x, y), ColorTranslator.FromHtml("#" + Settings.colorArray["rarityColor"][color].ToString())))
                        {
                            Main.AddLog("Found color: " + ColorTranslator.FromHtml("#" + Settings.colorArray["rarityColor"][color].ToString()));
                            return true;
                        }

                    }
                }
            }
            return false;
        }

        private static void doDebugDrawing(Bitmap image)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.DrawRectangle(new Pen(Color.Red), new Rectangle(new Point(window.X, window.Y), new Size(window.Width - 1, window.Height - 1)));
                graphics.DrawRectangle(new Pen(Color.Red), firstRewardRectangle);
                graphics.DrawRectangle(new Pen(Color.Red), secondRewardRectangle);
                graphics.FillRectangle(Brushes.Pink, center.X, center.Y, 5, 5);
                image.Save(Main.appPath + @"\Debug\FullScreenShotDebug " + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
            }
        }




        internal static Bitmap CaptureScreenshot()
        {
            Bitmap image;
            OCR.updateWindow();

            int height = Screen.PrimaryScreen.Bounds.Height * (int)OCR.dpi;
            int width = Screen.PrimaryScreen.Bounds.Width * (int)OCR.dpi;
            Bitmap Fullscreen = new Bitmap(width, height);
            Size FullscreenSize = new Size(Fullscreen.Width, Fullscreen.Height);
            using (Graphics graphics = Graphics.FromImage(Fullscreen))
            {
                graphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.Left, Screen.PrimaryScreen.Bounds.Top, 0, 0, FullscreenSize, CopyPixelOperation.SourceCopy);
            }
            Fullscreen.Save(Main.appPath + @"\Debug\Fullscreenshot.png");

            image = Fullscreen;
            return image;
        }


        public static Boolean verifyFocus()
        { // Returns True if warframe is in focuse, False if not
            uint processID = 0;
            uint threadID = Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out processID);
            try
            {
                if (processID == Warframe.Id || Settings.debug) { return true; }
                else
                {
                    Main.AddLog("Warframe is not focused");
                    Main.updatedStatus("Warframe is out of focus", 2);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Warframe.ToString());
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static Boolean verifyWarframe()
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
            Main.AddLog("Unable to detect Warframe in list of current active processes");
            Main.updatedStatus("Unable to detect Warframe process", 1);
            return false;

        }

        private static Bitmap turnBW(Bitmap source)
        {
            throw new NotImplementedException();
        }

        private static void refresScaling()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                dpi = graphics.DpiX / 96; //assuming that y and x axis dpi scaling will be uniform. So only checking one value
                TotalScaling = dpi * (Settings.Scaling / 100);
                Main.AddLog("Scaling updated to: " + TotalScaling + ". User has a DPI scaling of: " + dpi + " And a set UI scaling of: " + Settings.Scaling + "%");
            }
        }


        public static void updateWindow(Bitmap image = null)
        {
            if (!verifyWarframe())
                return;

            Win32.RECT osRect;
            refresScaling();
            if (!Win32.GetWindowRect(HandleRef, out osRect))
            { // get window size of warframe
                if (Settings.debug && Warframe == null)
                { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
                    Main.AddLog("No warframe detected, thus using center of image");
                    window = new Rectangle(0, 0, image.Width, image.Height);
                    center = new Point(window.Width / 2, window.Height / 2);
                    Console.WriteLine("Window is: " + window + " And center is: " + center);
                    return;
                }
                else
                {
                    Main.AddLog("Failed to get window bounds");
                    Main.updatedStatus("Failed to get window bounds", 1);
                    return;
                }
            }
            if (window.X < -20000 || window.Y < -20000) { Warframe = null; window = Rectangle.Empty; return; }
            // if the window is in the VOID delete current process and re-set window to nothing
            if (window.Left != osRect.Left || window.Right != osRect.Right || window.Top != osRect.Top || window.Bottom != osRect.Bottom)
            { // checks if old window size is the right size if not change it
                window = new Rectangle(osRect.Left, osRect.Top, osRect.Right - osRect.Left, osRect.Bottom - osRect.Top); // gett Rectangle out of rect
                                                                                                                         //Rectangle is (x, y, width, height) RECT is (x, y, x+width, y+height) 
                Main.AddLog("Window size updated to: " + window.ToString());
                int GWL_style = -16;
                uint Fullscreen = 885981184;
                uint Borderless = 2483027968;
                uint styles = Win32.GetWindowLongPtr(HandleRef, GWL_style);
                if (styles == Fullscreen) { currentStyle = WindowStyle.FULLSCREEN; Main.AddLog("Fullscreen detected"); } //Fullscreen, don't do anything
                else if (styles == Borderless) { currentStyle = WindowStyle.BORDERLESS; Main.AddLog("Borderless detected"); } //Borderless, don't do anything
                else
                { // Windowed, adjust for thicc border
                    window = new Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38);
                    Main.AddLog("Windowed detected, compensating to to: " + window.ToString());
                    currentStyle = WindowStyle.WINDOWED;
                }
                //center = new Point(window.Width / 2, window.Height / 2);
            }

        }

        internal static Bitmap getReward(int rewardSlot, int TotalRewards)
        {
            return null;
            throw new NotImplementedException();
        }

        internal static void proces(Bitmap reward)
        {
            return;
            reward = turnBW(reward);
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace WFInfo.Services.OCR
{
    public class ThemeHelpers
    {
        private static short[,,] GetThemeCache = new short[256, 256, 256];
        private static short[,,] GetThresholdCache = new short[256, 256, 256];

        /// <summary>
        /// Processes the theme, parse image to detect the theme in the image. Parse null to detect the theme from the screen.
        /// closeestThresh is used for getting the most "Accuracte" result, anything over 100 is sure to be correct.
        /// </summary>
        /// <param name="closestThresh"></param>
        /// <param name="screenScaling"></param>
        /// <param name="addLog"></param>
        /// <param name="cultureInfo"></param>
        /// <param name="captureScreenshot"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static WFtheme GetThemeWeighted(out double closestThresh, double screenScaling, Action<string> addLog, CultureInfo cultureInfo, Bitmap image = null)
        {
            int lineHeight = (int)(OcrConstants.pixelRewardLineHeight / 2 * screenScaling);
            // int width = image == null ? window.Width * (int)dpiScaling : image.Width;
            // int height = image == null ? window.Height * (int)dpiScaling : image.Height;
            int mostWidth = (int)(OcrConstants.pixleRewardWidth * screenScaling);
            // int mostLeft = (width / 2) - (mostWidth / 2);
            // int mostTop = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight) * screenScaling);
            // int mostBot = height / 2 - (int)((pixleRewardYDisplay - pixleRewardHeight) * screenScaling * 0.5);

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
                Debug.Write(weights[i].ToString("F2", cultureInfo) + " ");
                if (weights[i] > max)
                {
                    max = weights[i];
                    active = (WFtheme)i;
                }
            }
            addLog("CLOSEST THEME(" + max.ToString("F2", cultureInfo) + "): " + active.ToString());
            closestThresh = max;
            return active;
        }

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
                    Color themeColor = OcrConstants.ThemePrimary[(int)theme];
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

        private static int ColorDifference(Color test, Color thresh)
        {
            return Math.Abs(test.R - thresh.R) + Math.Abs(test.G - thresh.G) + Math.Abs(test.B - thresh.B);
        }

        public static bool ThemeThresholdFilter(Color test, WFtheme theme)
        {
            Color primary = OcrConstants.ThemePrimary[(int)theme];
            Color secondary = OcrConstants.ThemeSecondary[(int)theme];

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
    }
}
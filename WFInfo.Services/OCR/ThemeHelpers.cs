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
    }
}
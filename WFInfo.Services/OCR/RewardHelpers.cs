using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace WFInfo.Services.OCR
{
    public static class RewardHelpers
    {
        public static List<Bitmap> FilterAndSeparatePartsFromPartBox(Bitmap partBox, WFtheme active, Action<string> addLog, CultureInfo cultureInfo, Action<string, int> updateStatus, string appPath, string timestampParam, Action<bool> updateProcessingActive)
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
                    if (ThemeHelpers.ThemeThresholdFilter(clr, active))
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
                updateStatus("Filter and separate failed, report to dev", 1);
                updateProcessingActive(false);
                throw new Exception("Unable to find any parts");
            }

            double total = totalEven + totalOdd;
            addLog("EVEN DISTRIBUTION: " + (totalEven / total * 100).ToString("F2", cultureInfo) + "%");
            addLog("ODD DISTRIBUTION: " + (totalOdd / total * 100).ToString("F2", cultureInfo) + "%");

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
                newBox.Save(appPath + @"\Debug\PartBox(" + i + ") " + timestampParam + ".png");
            }
            filtered.Dispose();
            return ret;
        }

        public static List<Bitmap> ExtractPartBoxAutomatically(out double scaling, out WFtheme active,
            Bitmap fullScreen, CultureInfo cultureInfo, string appPath, CultureInfo culture, string timestampParam,
            Action<bool> updateProcessingActive, Action<string, int> statusUpdate, Action<string> addLog,
            double screenScalingParam, int width, int height)
        {
            var watch = new Stopwatch();
            watch.Start();
            long start = watch.ElapsedMilliseconds;
            long beginning = start;

            int lineHeight = (int)(OcrConstants.pixelRewardLineHeight / 2 * screenScalingParam);

            Color clr;
            int mostWidth = (int)(OcrConstants.pixleRewardWidth * screenScalingParam);
            int mostLeft = (width / 2) - (mostWidth / 2 );
            // Most Top = pixleRewardYDisplay - pixleRewardHeight + pixelRewardLineHeight
            //                   (316          -        235        +       44)    *    1.1    =    137
            int mostTop = height / 2 - (int)((OcrConstants.pixleRewardYDisplay - OcrConstants.pixleRewardHeight + OcrConstants.pixelRewardLineHeight) * screenScalingParam);
            int mostBot = height / 2 - (int)((OcrConstants.pixleRewardYDisplay - OcrConstants.pixleRewardHeight) * screenScalingParam * 0.5);
            //Bitmap postFilter = new Bitmap(mostWidth, mostBot - mostTop);
            var rectangle = new Rectangle((int)(mostLeft), (int)(mostTop), mostWidth, mostBot - mostTop);
            Bitmap preFilter;

            try
            {
                addLog($"Fullscreen is {fullScreen.Size}:, trying to clone: {rectangle.Size} at {rectangle.Location}");
                preFilter = fullScreen.Clone(new Rectangle(mostLeft, mostTop, mostWidth, mostBot - mostTop), fullScreen.PixelFormat);
            }
            catch (Exception ex)
            {
                addLog("Something went wrong with getting the starting image: " + ex.ToString());
                throw;
            }


            long end = watch.ElapsedMilliseconds;
            addLog("Grabbed images " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;
            
            active = ThemeHelpers.GetThemeWeighted(out var closest, screenScalingParam, addLog, cultureInfo, fullScreen);
            addLog("CLOSEST THEME(" + closest.ToString("F2", cultureInfo) + "): " + active);

            end = watch.ElapsedMilliseconds;
            addLog("Got theme " + (end - start) + "ms");
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
                    if (ThemeHelpers.ThemeThresholdFilter(clr, active))
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
            addLog("Filtered Image " + (end - start) + "ms");
            start = watch.ElapsedMilliseconds;

            double[] percWeights = new double[51];
            double[] topWeights = new double[51];
            double[] midWeights = new double[51];
            double[] botWeights = new double[51];

            int topLine_100 = preFilter.Height - lineHeight;
            int topLine_50 = lineHeight / 2;

            scaling = -1;
            double lowestWeight = 0;
            Rectangle uidebug = new Rectangle((topLine_100 - topLine_50) / 50 + topLine_50, (int)(preFilter.Height/screenScalingParam), preFilter.Width, 50);
            for (int i = 0; i <= 50; i++)
            {
                int yFromTop = preFilter.Height - (i * (topLine_100 - topLine_50) / 50 + topLine_50);

                int scale = (50 + i);
                int scaleWidth = preFilter.Width * scale / 100;

                int textTop = (int)(screenScalingParam * OcrConstants.TextSegments[0] * scale / 100);
                int textTopBot = (int)(screenScalingParam * OcrConstants.TextSegments[1] * scale / 100);
                int textBothBot = (int)(screenScalingParam * OcrConstants.TextSegments[2] * scale / 100);
                int textTailBot = (int)(screenScalingParam * OcrConstants.TextSegments[3] * scale / 100);

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

            addLog("Got scaling " + (end - start) + "ms");

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
                addLog("RANK " + (5 - i) + " SCALE: " + (topFive[i] + 50) + "%\t\t" + percWeights[topFive[i]].ToString("F2", cultureInfo) + " -- " + topWeights[topFive[i]].ToString("F2", cultureInfo) + ", " + midWeights[topFive[i]].ToString("F2", cultureInfo) + ", " + botWeights[topFive[i]].ToString("F2", cultureInfo));
            }

            using (Graphics g = Graphics.FromImage(fullScreen))
            {
                g.DrawRectangle(Pens.Red, rectangle);
                g.DrawRectangle(Pens.Chartreuse, uidebug);
            }
            fullScreen.Save(appPath + @"\Debug\BorderScreenshot " + timestampParam + ".png");


            //postFilter.Save(Main.appPath + @"\Debug\DebugBox1 " + timestamp + ".png");
            preFilter.Save(appPath + @"\Debug\FullPartArea " + timestampParam + ".png");
            scaling = topFive[4] + 50; //scaling was sometimes going to 50 despite being set to 100, so taking the value from above that seems to be accurate.

            scaling /= 100;
            double highScaling = scaling < 1.0 ? scaling + 0.01 : scaling;
            double lowScaling = scaling > 0.5 ? scaling - 0.01 : scaling;

            int cropWidth = (int)(OcrConstants.pixleRewardWidth * screenScalingParam * highScaling);
            int cropLeft = (preFilter.Width / 2) - (cropWidth / 2);
            int cropTop = height / 2 - (int)((OcrConstants.pixleRewardYDisplay - OcrConstants.pixleRewardHeight + OcrConstants.pixelRewardLineHeight) * screenScalingParam * highScaling);
            int cropBot = height / 2 - (int)((OcrConstants.pixleRewardYDisplay - OcrConstants.pixleRewardHeight) * screenScalingParam * lowScaling);
            int cropHei = cropBot - cropTop;
            cropTop -= mostTop;
            Bitmap partialScreenshot;
            try
            {
                Rectangle rect = new Rectangle(cropLeft, cropTop, cropWidth, cropHei);
                partialScreenshot = preFilter.Clone(rect, System.Drawing.Imaging.PixelFormat.DontCare);
                if (partialScreenshot.Height == 0 || partialScreenshot.Width == 0)
                    throw new ArithmeticException("New image was null");
            }
            catch (Exception ex)
            {
                addLog("Something went wrong while trying to copy the right part of the screen into partial screenshot: " + ex.ToString());
                throw;
            }

            preFilter.Dispose();

            end = watch.ElapsedMilliseconds;
            addLog("Finished function " + (end - beginning) + "ms");
            partialScreenshot.Save(appPath + @"\Debug\PartialScreenshot" + timestampParam + ".png");
            // Main.RunOnUIThread(() =>
            // {
            // Main.StatusUpdate("Filter and separate failed, report to dev", 1);
            // });
            return RewardHelpers.FilterAndSeparatePartsFromPartBox(partialScreenshot, active, addLog, culture, statusUpdate, appPath, timestampParam, updateProcessingActive);
        }
    }
}
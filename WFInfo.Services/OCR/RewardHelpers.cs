using System;
using System.Collections.Generic;
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
    }
}
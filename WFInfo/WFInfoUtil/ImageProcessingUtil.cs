using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using Tesseract;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WFInfo.WFInfoUtil
{
    public static class ImageProcessingUtil
    {
        static Regex MatchIllegalInventoryChars = new Regex("[^a-z가-힣0-9\\ ]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<Tuple<String, Rectangle>> GetTextWithBoundsFromImage(TesseractEngine engine, Bitmap image, int rectXOffset, int rectYOffset)
        {
            List<Tuple<String, Rectangle>> data = new List<Tuple<String, Rectangle>>();

            Debug.WriteLine($"Getting text from image");
            using (var page = engine.Process(image, PageSegMode.SparseText))
            {
                using (var iterator = page.GetIterator())
                {

                    iterator.Begin();
                    do
                    {
                        string currentWord = iterator.GetText(PageIteratorLevel.TextLine);
                        iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect tempbounds);
                        Rectangle bounds = new Rectangle(tempbounds.X1 + rectXOffset, tempbounds.Y1 + rectYOffset, tempbounds.Width, tempbounds.Height);
                        if (currentWord != null)
                        {
                            currentWord = MatchIllegalInventoryChars.Replace(currentWord, string.Empty);
                            currentWord = currentWord.TrimEnd();
                            if (currentWord.Length > 0)
                            { //word is valid start comparing to others
                                Debug.WriteLine($"Found valid word {currentWord}");
                                data.Add(Tuple.Create(currentWord, bounds));
                            }
                        }
                    }
                    while (iterator.Next(PageIteratorLevel.TextLine));
                }
            }
            return data;
        }

        public static List<WordMatch> GetWordMatches(Bitmap filteredImage, Bitmap filteredImageClean, int[] rowHits, int[] colHits, TesseractEngine[] engines, double screenScaling)
        {
            List<Tuple<string, Rectangle>>[] threadResults = GetThreadResults(filteredImage, filteredImageClean, rowHits, colHits, engines);
            List<WordMatch> matches = new List<WordMatch>();
            foreach (List<Tuple<string, Rectangle>> threadResult in threadResults)
            {
                foreach (Tuple<string, Rectangle> wordResult in threadResult)
                {
                    WordMatch match = new WordMatch(wordResult);
                    match.SetValidity(screenScaling);
                    matches.Add(match);
                }
            }

            return matches;
        }

        public static List<Tuple<string, Rectangle>>[] GetThreadResults(Bitmap filteredImage, Bitmap filteredImageClean, int[] rowHits, int[] colHits, TesseractEngine[] engines)
        {
            //divide image up into zones if we're doing multithreading
            List<Tuple<Bitmap, Rectangle>> zones;
            if (Settings.snapMultiThreaded)
            {
                zones = DivideSnapZones(filteredImage, filteredImageClean, rowHits, colHits);
            }
            else
            {
                zones = new List<Tuple<Bitmap, Rectangle>>();
                zones.Add(Tuple.Create(filteredImageClean, new Rectangle(0, 0, filteredImageClean.Width, filteredImageClean.Height)));
            }

            // start threads to process image
            int snapThreads = Settings.snapMultiThreaded ? 4 : 1;
            Task<List<Tuple<string, Rectangle>>>[] snapTasks = new Task<List<Tuple<String, Rectangle>>>[snapThreads];
            for (int i = 0; i < snapThreads; i++)
            {
                int tempI = i;
                snapTasks[i] = Task.Factory.StartNew(() =>
                {
                    List<Tuple<string, Rectangle>> taskResults = new List<Tuple<String, Rectangle>>();
                    for (int j = tempI; j < zones.Count; j += snapThreads)
                    {
                        //process images
                        List<Tuple<string, Rectangle>> currentResult = GetTextWithBoundsFromImage(engines[tempI], zones[j].Item1, zones[j].Item2.X, zones[j].Item2.Y);
                        taskResults.AddRange(currentResult);
                    }
                    return taskResults;
                });
            }

            //await completion of all tasks
            Task.WaitAll(snapTasks);

            return TaskUtil.MapTasksResult<List<Tuple<String, Rectangle>>>(snapTasks);
        }

            //draws information onto the filtered image based on the matches validity
        public static void DrawMatchesOnImage(Bitmap filteredImage, List<WordMatch> matches, GraphicFns graphicFns)
        {
            if(matches != null && graphicFns != null)
            {
                foreach (WordMatch match in matches)
                {
                    DrawMatchOnImage(filteredImage, match, graphicFns);
                }
            }
        }

        //Draws on the input image, returns true if valid box
        private static void DrawMatchOnImage(Bitmap filteredImage, WordMatch match, GraphicFns graphicFns)
        {
            if (match == null)
                return;

            using (Graphics g = Graphics.FromImage(filteredImage))
            {
                switch(match.validity)
                {
                    case WordMatchValidity.GoodMatch:
                        g.DrawRectangle(graphicFns.pinkP, match.paddedBounds);
                        break;
                    case WordMatchValidity.TooLargeButEnoughCharacters:
                        g.DrawRectangle(graphicFns.orange, match.paddedBounds);
                        break;
                    case WordMatchValidity.TooLarge:
                        g.FillRectangle(graphicFns.red, match.paddedBounds);
                        return;
                    case WordMatchValidity.TooFewCharacters:
                        g.FillRectangle(graphicFns.green, match.paddedBounds);
                        return;
                }

                g.DrawRectangle(graphicFns.greenp, match.bounds);
                g.DrawString(match.word, graphicFns.font, Brushes.Pink, new Point(match.paddedBounds.X, match.paddedBounds.Y));
            }
        }

        private static List<Tuple<Bitmap, Rectangle>> DivideSnapZones(Bitmap filteredImage, Bitmap filteredImageClean, int[] rowHits, int[] colHits)
        {
            List<Tuple<Bitmap, Rectangle>> zones = new List<Tuple<Bitmap, Rectangle>>();
            Pen brown = new Pen(Brushes.Brown);
            Pen white = new Pen(Brushes.White);

            //find rows
            List<Tuple<int, int>> rows = new List<Tuple<int, int>>(); //item1 = row top, item2 = row height
            int i = 0;
            int rowHeight = 0;
            while (i < filteredImage.Height)
            {
                if ((double)(rowHits[i]) / filteredImage.Width > Settings.snapRowTextDensity)
                {
                    int j = 0;
                    while (i + j < filteredImage.Height && (double)(rowHits[i + j]) / filteredImage.Width > Settings.snapRowEmptyDensity)
                    {
                        j++;
                    }
                    if (j > 3) //only add "rows" of reasonable height
                    {
                        rows.Add(Tuple.Create(i, j));
                        rowHeight += j;
                    }

                    i += j;
                }
                else
                {
                    i++;
                }
            }
            rowHeight = rowHeight / Math.Max(rows.Count, 1);

            //combine adjacent rows into one block of text
            i = 0;

            using (Graphics g = Graphics.FromImage(filteredImage))
            {
                using (Graphics gClean = Graphics.FromImage(filteredImageClean))
                {
                    while (i + 1 < rows.Count)
                    {

                        g.DrawLine(brown, 0, rows[i].Item1 + rows[i].Item2, 10000, rows[i].Item1 + rows[i].Item2);
                        gClean.DrawLine(white, 0, rows[i].Item1 + rows[i].Item2, 10000, rows[i].Item1 + rows[i].Item2);
                        if (rows[i].Item1 + rows[i].Item2 + rowHeight > rows[i + 1].Item1)
                        {
                            rows[i + 1] = Tuple.Create(rows[i].Item1, rows[i + 1].Item1 - rows[i].Item1 + rows[i + 1].Item2);
                            rows.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }

            //find columns
            List<Tuple<int, int>> cols = new List<Tuple<int, int>>(); //item1 = col start, item2 = col width

            int colStart = 0;
            i = 0;
            while (i + 1 < filteredImage.Width)
            {
                if ((double)(colHits[i]) / filteredImage.Height < Settings.snapColEmptyDensity)
                {
                    int j = 0;
                    while (i + j + 1 < filteredImage.Width && (double)(colHits[i + j]) / filteredImage.Width < Settings.snapColEmptyDensity)
                    {
                        j++;
                    }
                    if (j > rowHeight / 2)
                    {
                        if (i != 0)
                        {
                            cols.Add(Tuple.Create(colStart, i - colStart));
                        }
                        colStart = i + j + 1;
                    }
                    i += j;
                }
                i += 1;
            }
            if (i != colStart)
            {
                cols.Add(Tuple.Create(colStart, i - colStart));
            }

            //divide image into text blocks
            for (i = 0; i < rows.Count; i++)
            {
                for (int j = 0; j < cols.Count; j++)
                {
                    int top = Math.Max(rows[i].Item1 - (rowHeight / 2), 0);
                    int height = Math.Min(rows[i].Item2 + rowHeight, filteredImageClean.Height - top - 1);
                    int left = Math.Max(cols[j].Item1 - (rowHeight / 4), 0);
                    int width = Math.Min(cols[j].Item2 + (rowHeight / 2), filteredImageClean.Width - left - 1);
                    Rectangle cloneRect = new Rectangle(left, top, width, height);
                    Tuple<Bitmap, Rectangle> temp = Tuple.Create(filteredImageClean.Clone(cloneRect, filteredImageClean.PixelFormat), cloneRect);
                    zones.Add(temp);
                }
            }

            using (Graphics g = Graphics.FromImage(filteredImage))
            {
                foreach (Tuple<Bitmap, Rectangle> tup in zones)
                {
                    g.DrawRectangle(brown, tup.Item2);
                }
                g.DrawRectangle(brown, 0, 0, rowHeight / 2, rowHeight);
            }

            brown.Dispose();
            white.Dispose();
            return zones;
        }
    }
}
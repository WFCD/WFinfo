using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for errorDialogue.xaml
    /// </summary>
    public partial class ErrorDialogue : Window
    {

        string startPath = Main.AppPath + @"\Debug";
        string zipPath = Main.AppPath + @"\generatedZip";
        readonly int segmentSize = 8 * 1024 * 1024; // 8m segments

        private int distance;
        private DateTime closest;

        public ErrorDialogue(DateTime timeStamp, int gap)
        {
            distance = gap;
            closest = timeStamp;

            InitializeComponent();
            Show();
            Focus();
        }

        public void YesClick(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(zipPath);

            List<FileInfo> files = (new DirectoryInfo(startPath)).GetFiles()
                .Where(f => f.CreationTimeUtc > closest.AddSeconds(-1 * distance))
                .Where(f => f.CreationTimeUtc < closest.AddSeconds(distance))
                .ToList();

            try
            {
                var filePathsToCheck = new List<string>
                {
                    startPath + @"\..\eqmt_data.json",
                    startPath + @"\..\market_data.json",
                    startPath + @"\..\market_items.json",
                    startPath + @"\..\name_data.json",
                    startPath + @"\..\relic_data.json",
                    startPath + @"\..\settings.json",
                    startPath + @"\..\debug.log"
                };

                var fullZipPath = zipPath + @"\WFInfoError_" + closest.ToString("yyyy-MM-dd_HH-mm-ssff") + ".zip";
                using (ZipFile zip = new ZipFile())
                {
                    filePathsToCheck.Where(
                        path => File.Exists(path)
                    ).ToList().Concat(
                        files.Select(
                            file => file.FullName
                        )
                    ).ToList().ForEach(
                        filename => zip.AddFile(filename, "")
                    );

                    zip.MaxOutputSegmentSize64 = segmentSize; // 8m segments
                    zip.Save(fullZipPath);
                }
            }
            catch (Exception ex)
            {
                Main.AddLog("Unable to zip due to: " + ex.ToString());
                throw;
            }

            Process.Start(zipPath);
            Close();
        }

        private void NoClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}

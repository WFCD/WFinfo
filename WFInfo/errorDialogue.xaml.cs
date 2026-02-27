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
        const int segmentSize = 8 * 1024 * 1024; // 8m segments

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

                var fullZipPath = zipPath + @"\WFInfoError_" + closest.ToString("yyyy-MM-dd_HH-mm-ssff") + ".zip";
                using (ZipFile zip = new ZipFile())
                {
                    // Priority files: debug.log and settings JSON files
                    string parentDir = Path.GetDirectoryName(startPath);
                    var priorityFiles = new[]
                    {
                        Path.Combine(parentDir, "debug.log"),
                        Path.Combine(parentDir, "settings.json")
                    };

                    // Other data files
                    var otherDataFiles = new[]
                    {
                        Path.Combine(parentDir, "eqmt_data.json"),
                        Path.Combine(parentDir, "market_data.json"),
                        Path.Combine(parentDir, "market_items.json"),
                        Path.Combine(parentDir, "name_data.json"),
                        Path.Combine(parentDir, "relic_data.json")
                    };

                    // Add debug folder files first (will end up in later segments)
                    foreach (FileInfo file in files)
                    {
                        zip.AddFile(file.FullName, "");
                    }

                    // Add other data files next
                    foreach (string path in otherDataFiles)
                    {
                        if (File.Exists(path))
                        {
                            zip.AddFile(path, "");
                        }
                    }

                    // Add priority files last (will end up in first segment .z01)
                    foreach (string path in priorityFiles)
                    {
                        if (File.Exists(path))
                        {
                            zip.AddFile(path, "");
                        }
                    }

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

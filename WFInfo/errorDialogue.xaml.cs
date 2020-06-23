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

            List<FileInfo> files = (new DirectoryInfo(Main.AppPath + @"\Debug\")).GetFiles()
                .Where(f => f.CreationTimeUtc > closest.AddSeconds(-1 * distance))
                .Where(f => f.CreationTimeUtc < closest.AddSeconds(distance))
                .ToList();

            var fullZipPath = zipPath + @"\WFInfoError" + closest.ToString("yyyy-MM-dd HH-mm-ssff") + ".zip";
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    foreach (FileInfo file in files)
                        zip.AddFile(file.FullName, "");
                    if (File.Exists(startPath + @"\..\eqmt_data.json"))
                    {
                        zip.AddFile(startPath + @"\..\eqmt_data.json", "");
                    }
                    else
                        Main.AddLog("eqmt_data.json didn't exist.");
                    if (File.Exists(startPath + @"\..\market_data.json"))
                    { 
                        zip.AddFile(startPath + @"\..\market_data.json", "");
                    }
                    else
                        Main.AddLog("market_data.json didn't exist.");
                    if (File.Exists(@"\..\market_items.json"))
                    {
                        zip.AddFile(startPath + @"\..\market_items.json", "");
                    }
                    else
                        Main.AddLog("market_items.json didn't exist.");
                    if (File.Exists(startPath + @"\..\name_data.json"))
                    {
                        zip.AddFile(startPath + @"\..\name_data.json", "");
                    }
                    else
                        Main.AddLog("name_data.json didn't exist.");
                    if (File.Exists(startPath + @"\..\relic_data.json.json"))
                    {
                        zip.AddFile(startPath + @"\..\relic_data.json.json", "");
                    }
                    else
                        Main.AddLog("relic_data.json didn't exist.");
                    if (File.Exists(startPath + @"\..\settings.json"))
                    {
                        zip.AddFile(startPath + @"\..\settings.json", "");
                    }
                    else
                        Main.AddLog("settings.json didn't exist.");
                    zip.AddFile(startPath + @"\..\debug.log", "");
                    zip.Comment = "This zip was created at " + closest.ToString("yyyy-MM-dd HH-mm-ssff");
                    zip.MaxOutputSegmentSize64 = 8000 * 1024; // 8m segments
                    zip.Save(fullZipPath);
                }
            }
            catch (Exception ex)
            {
                Main.AddLog("Unable to zip due to: " + ex.ToString());
                throw;
            }

            Process.Start(Main.AppPath + @"\generatedZip");
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

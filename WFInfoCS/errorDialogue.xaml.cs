using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for errorDialogue.xaml
    /// </summary>
    public partial class ErrorDialogue : System.Windows.Window
    {

        string startPath = Main.appPath + @"\Debug";
        string zipPath = Main.appPath + @"\generatedZip";

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

            List<FileInfo> files = (new DirectoryInfo(Main.appPath + @"\Debug\")).GetFiles()
                .Where(f => f.CreationTimeUtc > closest.AddSeconds(-1 * distance))
                .Where(f => f.CreationTimeUtc < closest.AddSeconds(distance))
                .ToList();

            var fullZipPath = zipPath + @"\WFInfoError" + closest.ToString("yyyy-MM-dd HH-mm-ssff") + ".zip";
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    foreach (FileInfo file in files)
                        zip.AddFile(file.FullName,"");
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

            Process.Start(Main.appPath + @"\generatedZip");
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

using Ionic.Zip;
using System;
using System.Diagnostics;
using System.IO;
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
        public ErrorDialogue()
        {
            InitializeComponent();
            Show();
            Focus();
        }

        public void YesClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(zipPath))
            {
                _ = Directory.CreateDirectory(zipPath);
            }
            var fullZipPath = zipPath + @"\WFInfoError" + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff") + ".zip";
            if (File.Exists(startPath + @"\debug.log")) File.Delete(startPath + @"\debug.log");
            File.Copy(startPath + @"\..\debug.log", startPath + @"\debug.log");
            int segmentsCreated;
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddDirectory(startPath);
                    zip.Comment = "This zip was created at " + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff");
                    zip.MaxOutputSegmentSize64 = 8000 * 1024; // 8m segments
                    zip.Save(zipPath + @"\WFInfoError.zip");

                    segmentsCreated = zip.NumberOfSegmentsForMostRecentSave;
                }
            }
            catch (Exception ex)
            {
                Main.AddLog("Unable to zip due to: " + ex.ToString());
                throw;
            }

            OCR.errorDetected = false;
            Process.Start(Main.appPath + @"\generatedZip");
            Close();
        }

        private void NoClick(object sender, RoutedEventArgs e)
        {
            OCR.errorDetected = false;
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

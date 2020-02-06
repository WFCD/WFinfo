using System;
using System.Diagnostics;
using System.IO.Compression;
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

        string startPath = Main.appPath + @"\debug";
        string zipPath = Main.appPath + @"\generatedZip";
        public ErrorDialogue()
        {
            InitializeComponent();
            Show();
            Focus();
        }

        private void YesClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(zipPath))
            {
                _ = Directory.CreateDirectory(zipPath);
            }
            try
            {
                ZipFile.CreateFromDirectory(startPath, zipPath + @"\WFInfoError" + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff") + ".zip");
            }
            catch (Exception)
            {

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

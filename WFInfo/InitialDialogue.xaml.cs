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
    public partial class InitialDialogue : System.Windows.Window
    {
        private int filesTotal = 0;
        private int filesDone = 1;
        private int percentage = 0;
        public InitialDialogue()
        {
            InitializeComponent();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Exit(object sender, EventArgs e)
        {
            CustomEntrypoint.stopDownloadTask.Cancel();
        }

        internal void SetFilesNeed(int filesNeeded)
        {
            filesTotal = filesNeeded;
            Progress.Text = "0% (" + filesDone + "/" + filesTotal + ")";
            Progress.Visibility = Visibility.Visible;
        }

        internal void UpdatePercentage(double perc)
        {
            Progress.Text = perc.ToString("F0") + "% (" + filesDone + "/" + filesTotal + ")";
        }

        internal void FileComplete()
        {
            filesDone++;
            Progress.Text = "0% (" + filesDone + "/" + filesTotal + ")";
        }
    }
}

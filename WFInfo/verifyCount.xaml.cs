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
    public partial class VerifyCount : Window
    {

        string itemPath = Main.AppPath + @"\eqmt_data.json";

        private List<InventoryItem> latestSnap;

        public VerifyCount( List<InventoryItem> itemList)
        {
            latestSnap = itemList;
            InitializeComponent();
            Show();
            Focus();
        }

        public void YesClick(object sender, RoutedEventArgs e)
        {
            //TODO 
            Close();
        }

        private void BackupClick(object sender, RoutedEventArgs e)
        {
            File.Copy(itemPath, itemPath+".bak");
            //TODO Clear old
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

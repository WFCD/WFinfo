using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for verifyCount.xaml
    /// </summary>
    public partial class VerifyCount : Window
    {

        private static string itemPath = Main.AppPath + @"\eqmt_data.json";
        private static string backupPath = Main.AppPath + @"\eqmt_data.json.bak";

        private List<InventoryItem> latestSnap;
        public static VerifyCount INSTANCE;
        private DateTime triggerTime;

        public VerifyCount()
        {
            InitializeComponent();
            INSTANCE = this;
            latestSnap = new List<InventoryItem>();
            triggerTime = DateTime.UtcNow;
        }
        public static void ShowVerifyCount( List<InventoryItem> itemList)
        {
            if (INSTANCE != null)
            {
                INSTANCE.latestSnap = itemList;
                INSTANCE.triggerTime = DateTime.UtcNow;
                INSTANCE.BackupButton.Visibility = Visibility.Visible;
                INSTANCE.Show();
                INSTANCE.Focus();
            }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            bool saveFailed = false;
            foreach (InventoryItem item in latestSnap)
            {
                if (item.Name.Contains("Prime"))
                {
                    string[] nameParts = item.Name.Split(new string[] { "Prime" }, 2, StringSplitOptions.None);
                    string primeName = nameParts[0] + "Prime";
                    string partName = primeName + ( ( nameParts[1].Length > 10 && !nameParts[1].Contains("Kubrow") ) ? nameParts[1].Replace(" Blueprint", "") : nameParts[1]);

                    Main.AddLog("Saving count \"" + item.Count + "\" for part \"" + partName + "\"");
                    try
                    {
                        Main.dataBase.equipmentData[primeName]["parts"][partName]["owned"] = item.Count;
                    }
                    catch (Exception ex)
                    {
                        Main.AddLog("FAILED to save count. Count: " + item.Count + ", Name: " + item.Name + ", primeName: " + primeName + ", partName: " + partName);
                        saveFailed = true;
                    }
                }
            }
            Main.dataBase.SaveAllJSONs();
            EquipmentWindow.INSTANCE.reloadItems();
            if (saveFailed)
            {
                //shouldn't need Main.RunOnUIThread since this is already on the UI Thread
                //adjust for time diff between snap-it finishing and save being pressed, in case of long delay
                Main.SpawnErrorPopup(DateTime.UtcNow, (int)((DateTime.UtcNow - triggerTime).TotalSeconds) + 30);
                Main.StatusUpdate("Failed to save one or more item, report to dev", 2);
            }
            Hide();
        }

        private void BackupClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            File.Copy(itemPath, backupPath);
            foreach (KeyValuePair<string, JToken> prime in Main.dataBase.equipmentData)
            {
                string primeName = prime.Key.Substring(0, prime.Key.IndexOf("Prime") + 5);
                if (prime.Key.Contains("Prime"))
                {
                    foreach (KeyValuePair<string, JToken> primePart in prime.Value["parts"].ToObject<JObject>())
                    {
                        string partName = primePart.Key;
                        Main.dataBase.equipmentData[primeName]["parts"][partName]["owned"] = 0;
                    }
                }
            }
            BackupButton.Visibility = Visibility.Hidden;
            Main.dataBase.SaveAllJSONs();
            EquipmentWindow.INSTANCE.reloadItems();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}

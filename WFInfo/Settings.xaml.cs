using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : System.Windows.Window
    {

        private static readonly string settingsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json";  //change to WFInfo after release
        public static JObject settingsObj; // contains settings {<SettingName>: "<Value>", ...}
        public static Key activationKey;
        public static Point mainWindowLocation;
        public static bool isOverlaySelected;
        public static bool debug;
        public static long autoDelay;
        public static int imageRetentionTime;

        public static int scaling { get; internal set; }
        public static bool auto { get; internal set; }
        public static bool clipboard { get; internal set; }


        public Settings()
        {
            InitializeComponent();
        }

        public void populate()
        {
            DataContext = this;

            scaleBar.Value = scaling;
            if (settingsObj.GetValue("Display").ToString() == "Overlay")
                OverlayRadio.IsChecked = true;
            else
                WindowRadio.IsChecked = true;

            if (Convert.ToBoolean(settingsObj.GetValue("Auto")))
                autoCheckbox.IsChecked = true;

            if (Convert.ToBoolean(settingsObj.GetValue("Clipboard")))
                clipboardCheckbox.IsChecked = true;

            //Activation_key_box.Text = "Snapshot";
            Scaling_box.Text = scaling.ToString() + "%";
            Activation_key_box.Text = settingsObj.GetValue("ActivationKey").ToString();
            Focus();
        }

        public static void Save()
        {
            File.WriteAllText(settingsDirectory, JsonConvert.SerializeObject(settingsObj, Formatting.Indented));
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            Save();
            Hide();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(hidden);
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void WindowChecked(object sender, RoutedEventArgs e)
        {
            settingsObj["Display"] = "Window";
            isOverlaySelected = false;
            Save();
        }

        private void OverlayChecked(object sender, RoutedEventArgs e)
        {
            settingsObj["Display"] = "Overlay";
            isOverlaySelected = true;
            Save();
        }

        private void AutoClicked(object sender, RoutedEventArgs e)
        {
            settingsObj["Auto"] = autoCheckbox.IsChecked.Value;
            auto = autoCheckbox.IsChecked.Value;
            if (auto)
            {
                var message = "Do you want to enable the new auto mode?" + Environment.NewLine +
                "This connects to the warframe debug logger to detect the reward window." + Environment.NewLine +
                "The logger contains info about your pc specs, your public IP, and your email." + Environment.NewLine +
                "We will be ignoring all of that and only looking for the Fissure Reward Screen." + Environment.NewLine +
                "We will begin listening after your approval, and it is completely inactive currently." + Environment.NewLine +
                "If you opt-in, we will be using a windows method to receive this info quicker, but it is the same info being written to EE.log, which you can check before agreeing." + Environment.NewLine +
                "If you want more information or have questions, please contact us on Discord.";
                MessageBoxResult messageBoxResult = MessageBox.Show(message, "Automation Mode Opt-In", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    Main.dataBase.EnableLogcapture();
                }
                else
                {
                    settingsObj["Auto"] = false;
                    auto = false;
                    autoCheckbox.IsChecked = false;
                    Main.dataBase.DisableLogCapture();
                }
            }
            else
            {
                settingsObj["Auto"] = false;
                auto = false;
                Main.dataBase.DisableLogCapture();
            }
            Save();
        }

        private void ScalingValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                int newVal = (int)Math.Round(e.NewValue);
                scaleBar.Value = newVal;
                settingsObj["Scaling"] = newVal;
                scaling = newVal;
                Scaling_box.Text = newVal.ToString() + "%";
                Save();
            }
        }

        private void ScaleLeave(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = Regex.Replace(Scaling_box.Text.ToString(), "[^0-9]", "");
                if (input.Length > 0)
                {
                    int value = Convert.ToInt32(input);
                    if (value < 50)
                        value = 50;
                    else if (value > 100)
                        value = 100;

                    settingsObj["Scaling"] = value;
                    scaleBar.Value = value;
                    Scaling_box.Text = value + "%";
                    Save();
                }
                else
                    Scaling_box.Text = settingsObj.GetValue("Scaling").ToString() + "%";
            }
            catch
            {
                Scaling_box.Text = settingsObj.GetValue("Scaling").ToString() + "%";
                Main.AddLog("Couldn't save scaling from text input"); //works don't ask me how
            }

        }

        private void ScaleDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                scaleBar.Focus();
            }
        }

        private void scaleFocus(object sender, RoutedEventArgs e)
        {
            Scaling_box.Text = "";
        }

        private void ActivationDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void ActivationFocus(object sender, RoutedEventArgs e)
        {
            Activation_key_box.Text = "";
        }

        private void ActivationUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            activationKey = e.Key;
            Activation_key_box.Text = e.Key.ToString();
            settingsObj["ActivationKey"] = e.Key.ToString();
            hidden.Focus();
            Save();
        }

        private void ActivationLost(object sender, RoutedEventArgs e)
        {
            Activation_key_box.Text = activationKey.ToString();
        }

        private void ClickCreateDebug(object sender, RoutedEventArgs e)
        {
            Main.SpawnErrorPopup(DateTime.UtcNow, 1800);
        }

        private void clipboardCheckbox_Checked(object sender, RoutedEventArgs e) {
            settingsObj["Clipboard"] = clipboardCheckbox.IsChecked.Value;
            clipboard = clipboardCheckbox.IsChecked.Value;
        }
    }
}

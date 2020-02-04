using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : System.Windows.Window
    {

        private static readonly string settingsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\settings.json";  //change to WFInfo after release
        public static JObject settingsObj; // contains settings {<SettingName>: "<Value>", ...}
        public static Key activationKey;
        public static bool isOverlaySelected;
        public static bool debug;
        public static int scaling { get; internal set; }
        public static bool auto { get; internal set; }


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
                Auto.IsChecked = true;

            //if (Convert.ToBoolean(settingsObj.GetValue("Debug")))
            //    Debug.IsChecked = true;

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
        /*
        private void DebugClicked(object sender, RoutedEventArgs e)
        {
            settingsObj["Debug"] = Debug.IsChecked.Value;
            debug = Debug.IsChecked.Value;
            Save();
        }*/

        private void AutoClicked(object sender, RoutedEventArgs e)
        {
            settingsObj["Auto"] = Auto.IsChecked.Value;
            auto = Auto.IsChecked.Value;
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
                //Keyboard.ClearFocus();
                //ScaleLeave(sender, e);
                this.scaleBar.Focus();
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
            Save();
        }

        private void ActivationLost(object sender, RoutedEventArgs e)
        {
            Activation_key_box.Text = activationKey.ToString();
        }
    }
}

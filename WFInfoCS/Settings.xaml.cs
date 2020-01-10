using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        private string settingsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\settings.json";  //change to WFInfo after release
        public static JObject settingsObj; // contains settings {<SettingName>: "<Value>", ...}
        public bool boolWindow { get; set; } //Choseen for multiple booleans over a single one due to the posibility of aditional options in the future.
        public static Key activationKey { get; set; }
        public bool boolOverlay { get; set; }
        public static bool debug { get; set; }
        public static int Scaling { get; internal set; }
        public static bool auto { get; internal set; }

        Main main = new Main();

        public Settings()
        {


            InitializeComponent();

            if (settingsObj.GetValue("Display").ToString() == "Window") {
                boolWindow = true;
            } else {
                boolOverlay = true;
            }
            if (Convert.ToBoolean(settingsObj.GetValue("Auto"))){
                Auto.IsChecked = true;
            }
            if (Convert.ToBoolean(settingsObj.GetValue("Debug")))
            {
                Debug.IsChecked = true;
            }
            this.DataContext = this;
            Activation_key_box.Text = "Snapshot";
            Scaling_box.Text = Scaling.ToString() + "%";
            Activation_key_box.Text = settingsObj.GetValue("ActivationKey").ToString();
            scaleBar.Value = Scaling;

        }


        private void save()
        {
            File.WriteAllText(settingsDirectory, JsonConvert.SerializeObject(settingsObj, Formatting.Indented));
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            save();
            this.Close();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_checked(object sender, RoutedEventArgs e)
        {
            settingsObj["Display"] = "Window";
            boolWindow = true;
            boolOverlay = false;
            save();
        }

        private void Overlay_checked(object sender, RoutedEventArgs e)
        {
            settingsObj["Display"] = "Overlay";
            boolWindow = false;
            boolOverlay = true;
            save();
        }

        private void Debug_Clicked(object sender, RoutedEventArgs e)
        {
            settingsObj["Debug"] = Debug.IsChecked.Value;
            debug = Debug.IsChecked.Value;
            save();
        }
        private void Auto_Clicked(object sender, RoutedEventArgs e)
        {
            settingsObj["Auto"] = Auto.IsChecked.Value;
            auto = Auto.IsChecked.Value;
            save();
        }

        private void Scaling_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            settingsObj["Scaling"] = Math.Round(e.NewValue);
            Scaling_box.Text = Math.Round(e.NewValue).ToString() + "%";
            save();
        }

        private void ScaleLeave(object sender, RoutedEventArgs e)
        {
            try{
                string input = Regex.Replace(Scaling_box.Text.ToString(), "[^0-9.]", "");
                int value = Convert.ToInt32(input);
                if (value < 50)
                {
                    value = 50;
                }
                else if (value > 100 || value == 0)
                {
                    value = 100;
                }
                scaleBar.Value = value;
                settingsObj["Scaling"] = value;
                Scaling_box.Text = value + "%";
                save();
            }
            catch
            {
                Scaling_box.Text = settingsObj.GetValue("Scaling").ToString() + "%";
                main.AddLog("Couldn't save scaling from text input"); //works
            }
            
        }

        private void ScaleDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                Keyboard.ClearFocus();
                ScaleLeave(sender, e);
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
            save();
        }
    }
}

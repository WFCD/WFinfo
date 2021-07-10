using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WebSocketSharp;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        private static readonly string settingsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json";  //change to WFInfo after release
        public static JObject settingsObj; // contains settings {<SettingName>: "<Value>", ...}

        public static MouseButton backupMouseVal = MouseButton.Left;
        private static MouseButton activeMouseVal = MouseButton.Left;
        public static MouseButton ActivationMouseButton
        {
            get { return activeMouseVal; }
            set
            {
                activeMouseVal = value;
                backupMouseVal = value;
            }
        }

        public static Key backupKeyVal = Key.None;
        private static Key activeKeyVal = Key.None;
        public static Key ActivationKey
        {
            get { return activeKeyVal; }
            set
            {
                activeKeyVal = value;
                backupKeyVal = value;
            }
        }
        public static string locale;
        public static Key DebugModifierKey;
        public static Key SnapitModifierKey;
        public static Key SearchItModifierKey;
        public static Key MasterItModifierKey;
        public static KeyConverter converter = new KeyConverter();
        public static Point mainWindowLocation;
        public static bool isOverlaySelected;
        public static bool isLightSelected;
        public static bool debug;
        public static long autoDelay;
        public static int imageRetentionTime;
        public static string ClipboardTemplate;
        internal static int delay;
        public static bool Highlight;
        internal static bool highContrast;
        public static int overlayXOffsetValue;
        public static int overlayYOffsetValue;
        public static double maximumEfficiencyValue;
        public static double minimumEfficiencyValue;
        public static bool automaticListing;
        public static bool doDoubleCheck;
        public static bool doSnapItCount;
        public static int snapItCountThreshold;
        public static int snapItEdgeWidth;
        public static int snapItEdgeRadius;
        public static double snapItHorizontalNameMargin;
        public static bool doCustomNumberBoxWidth;
        public static double snapItNumberBoxWidth;
        public static bool auto { get; internal set; }
        public static bool clipboard { get; internal set; }
        public static bool detectScaling { get; internal set; }
        public static bool SnapitExport { get; internal set; }
        public static bool ClipboardVaulted { get; internal set; }
        public static bool SnapItCount { get; internal set; }

        public Settings()
        {
            InitializeComponent();
        }

        public void populate()
        {
            DataContext = this;

            Overlay_sliders.Visibility = Visibility.Collapsed; // default hidden for the majority of states

            if (settingsObj.GetValue("Display").ToString() == "Overlay")
            {
                OverlayRadio.IsChecked = true;
                Overlay_sliders.Visibility = Visibility.Visible;
                Height = 594;
            }
            else if (settingsObj.GetValue("Display").ToString() == "Light")
            {
                LightRadio.IsChecked = true;
                Height = 524;
            }
            else
            {
                WindowRadio.IsChecked = true;
                Height = 524;
            }

            if (Convert.ToBoolean(settingsObj.GetValue("Auto")))
            {
                autoCheckbox.IsChecked = true;
                Autolist.IsEnabled = true;
            }
            else
            {
                Autolist.IsEnabled = false;
            }

            if (Convert.ToBoolean(settingsObj.GetValue("Clipboard")))
                clipboardCheckbox.IsChecked = true;

            if (Convert.ToBoolean(settingsObj.GetValue("HighlightRewards")))
                HighlightCheckbox.IsChecked = true;

            if (Convert.ToBoolean(settingsObj.GetValue("HighContrast")))
                HighContrastCheckbox.IsChecked = true;

            if (Convert.ToBoolean(settingsObj.GetValue("AutoList")))
                Autolist.IsChecked = true;

            string settingLocale = Convert.ToString(settingsObj.GetValue("Locale"));
            foreach (ComboBoxItem localeItem in localeCombobox.Items)
            {
                if(settingLocale.Equals(localeItem.Tag.ToString()))
                {
                    localeItem.IsSelected = true;
                }
            }

            OverlayXOffset_number_box.Text = overlayXOffsetValue.ToString(Main.culture);
            OverlayYOffset_number_box.Text = (-1 * overlayYOffsetValue).ToString(Main.culture);

            EfficiencyMax_number_box.Text = maximumEfficiencyValue.ToString(Main.culture);
            EfficiencyMin_number_box.Text = minimumEfficiencyValue.ToString(Main.culture);
            Displaytime_number_box.Text = delay.ToString(Main.culture);

            if (Convert.ToBoolean(settingsObj.GetValue("DoSnapItCount")))
                SnapItemCountCheckbox.IsChecked = true;

            SnapItCountThreshold_number_box.Text = snapItCountThreshold.ToString(Main.culture);

            ResetActivationKeyText();
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
            isLightSelected = false;
            Overlay_sliders.Visibility = Visibility.Collapsed;
            clipboardCheckbox.IsChecked = (bool)settingsObj["Clipboard"];
            clipboardCheckbox.IsEnabled = true;
            Height = 524;
            Save();
        }

        private void OverlayChecked(object sender, RoutedEventArgs e)
        {
            settingsObj["Display"] = "Overlay";
            isOverlaySelected = true;
            isLightSelected = false;
            Overlay_sliders.Visibility = Visibility.Visible;
            clipboardCheckbox.IsChecked = (bool)settingsObj["Clipboard"];
            clipboardCheckbox.IsEnabled = true;
            Height = 594;
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
                    Main.dataBase.EnableLogCapture();
                    Autolist.IsEnabled = true;
                }
                else
                {
                    settingsObj["Auto"] = false;
                    auto = false;
                    autoCheckbox.IsChecked = false;
                    Main.dataBase.DisableLogCapture();
                    Autolist.IsEnabled = false;
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


        private void ActivationDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void ActivationMouseDown(object sender, MouseEventArgs e)
        {
            if (activeKeyVal == Key.None && ActivationMouseButton == MouseButton.Left)
            {
                MouseButton key = MouseButton.Left;

                if (e.MiddleButton == MouseButtonState.Pressed)
                    key = MouseButton.Middle;
                else if (e.XButton1 == MouseButtonState.Pressed)
                    key = MouseButton.XButton1;
                else if (e.XButton2 == MouseButtonState.Pressed)
                    key = MouseButton.XButton2;

                if (key != MouseButton.Left)
                {
                    e.Handled = true;
                    //Set key to disabled 
                    ActivationKey = Key.None;

                    ActivationMouseButton = key;
                    Activation_key_box.Text = key.ToString();
                    settingsObj["ActivationKey"] = key.ToString();
                    hidden.Focus();
                    Save();
                }
            }
        }

        private void ActivationFocus(object sender, RoutedEventArgs e)
        {
            activeKeyVal = Key.None;
            activeMouseVal = MouseButton.Left;
            Activation_key_box.Text = "";
        }

        private void ActivationUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            //Set mouse button to disabled (never gonna use left as a trigger)
            ActivationMouseButton = MouseButton.Left;

            if (e.Key == SearchItModifierKey || e.Key == SnapitModifierKey || e.Key == MasterItModifierKey)
            {
                Activation_key_box.Text = GetKeyName(ActivationKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            ActivationKey = key;
            Activation_key_box.Text = GetKeyName(ActivationKey);
            settingsObj["ActivationKey"] = key.ToString();
            hidden.Focus();
            Save();
        }

        private void ResetActivationKeyText()
        {
            if (backupKeyVal != Key.None)
            {
                activeKeyVal = backupKeyVal;
                Activation_key_box.Text = GetKeyName(ActivationKey);
            }
            else
            {
                activeMouseVal = backupMouseVal;
                Activation_key_box.Text = ActivationMouseButton.ToString();
            }

            Searchit_key_box.Text = GetKeyName(SearchItModifierKey);
            Snapit_key_box.Text = GetKeyName(SnapitModifierKey);
            Masterit_key_box.Text = GetKeyName(MasterItModifierKey);
        }

        private void ActivationLost(object sender, RoutedEventArgs e)
        {
            ResetActivationKeyText();
        }

        private void ClickCreateDebug(object sender, RoutedEventArgs e)
        {
            Main.SpawnErrorPopup(DateTime.UtcNow, 1800);
        }

        private void clipboardCheckboxClicked(object sender, RoutedEventArgs e)
        {
            settingsObj["Clipboard"] = clipboardCheckbox.IsChecked.Value;
            clipboard = clipboardCheckbox.IsChecked.Value;
            Save();
        }
        private void localeComboboxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ComboBoxItem item = (ComboBoxItem) localeCombobox.SelectedItem;
            
            string selectedLocale = item.Tag.ToString();
            settingsObj["Locale"] = selectedLocale;
            locale = selectedLocale;
            Save();

            _ = OCR.updateEngineAsync();

            _ = Task.Run(async () =>
              {
                  Main.dataBase.ReloadItems();
              });
        }

        public static string GetKeyName(Key key)
        {
            char temp = GetCharFromKey(key);
            switch (key)
            {
                case Key.OemTilde:
                    return "Tilde";
                case Key.Return:
                    return "Enter";
                case Key.Next:
                    return "PageDown";
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                    return key.ToString();
                case Key.Decimal:
                    return "NumpadDot";
                case Key.Add:
                case Key.Subtract:
                case Key.Multiply:
                case Key.Divide:
                    return "NumPad" + key.ToString().Substring(0, 3);
            }
            if (temp > ' ')
                return temp.ToString().ToUpper();
            return key.ToString();
        }

        // Black magic below - blame: https://stackoverflow.com/a/5826175

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        //[DllImport("user32.dll")]
        //public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            //  Disabled to avoid Shifted variants   EX: Shift + \ => |
            //  But we don't care about the character, we just want the key
            //  So ignore they current keyboard state
            //GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }

        private void LightRadioChecked(object sender, RoutedEventArgs e)
        {
            settingsObj["Display"] = "Light";
            isOverlaySelected = false;
            isLightSelected = true;
            Overlay_sliders.Visibility = Visibility.Collapsed;
            clipboard = true;
            clipboardCheckbox.IsChecked = true;
            clipboardCheckbox.IsEnabled = false;
            Height = 524;
            Save();
        }

        private void HighlightRewardCheckbox_Click(object sender, RoutedEventArgs e)
        {
            settingsObj["HighlightRewards"] = HighlightCheckbox.IsChecked.Value;
            Highlight = HighlightCheckbox.IsChecked.Value;
            Save();
        }

        private void Searchit_key_box_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchItModifierKey = Key.None;
            Searchit_key_box.Text = "";
            backupKeyVal = activeKeyVal;
            activeKeyVal = Key.NoName;
        }

        private void Searchit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == backupKeyVal || e.Key == SnapitModifierKey || e.Key == MasterItModifierKey)
            {
                Searchit_key_box.Text = GetKeyName(SearchItModifierKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            SearchItModifierKey = key;
            Searchit_key_box.Text = GetKeyName(key);
            settingsObj["SearchItModifierKey"] = key.ToString();
            hidden.Focus();
            Save();
        }

        private void Snapit_key_box_LostFocus(object sender, RoutedEventArgs e)
        {
            ResetActivationKeyText();
        }

        private void Masterit_key_box_LostFocus(object sender, RoutedEventArgs e)
        {
            ResetActivationKeyText();
        }

        private void Searchit_key_box_LostFocus(object sender, RoutedEventArgs e)
        {
            ResetActivationKeyText();
        }

        private void Snapit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == backupKeyVal || e.Key == SearchItModifierKey || e.Key == MasterItModifierKey)
            {
                Snapit_key_box.Text = GetKeyName(SnapitModifierKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            SnapitModifierKey = key;
            Snapit_key_box.Text = GetKeyName(key);
            settingsObj["SnapitModifierKey"] = key.ToString();
            hidden.Focus();
            Save();
        }

        private void Snapit_key_box_GotFocus(object sender, RoutedEventArgs e)
        {
            SnapitModifierKey = Key.None;
            Snapit_key_box.Text = "";
            backupKeyVal = activeKeyVal;
            activeKeyVal = Key.NoName;
        }

        private void Snapit_key_box_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void Masterit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == backupKeyVal || e.Key == SearchItModifierKey || e.Key == SnapitModifierKey)
            {
                Masterit_key_box.Text = GetKeyName(MasterItModifierKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            MasterItModifierKey = key;
            Masterit_key_box.Text = GetKeyName(key);
            settingsObj["MasterItModifierKey"] = key.ToString();
            hidden.Focus();
            Save();
        }

        private void Masterit_key_box_GotFocus(object sender, RoutedEventArgs e)
        {
            MasterItModifierKey = Key.None;
            Masterit_key_box.Text = "";
            backupKeyVal = activeKeyVal;
            activeKeyVal = Key.NoName;
        }

        private void Masterit_key_box_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void Searchit_key_box_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key != Key.Enter) return;
            Displaytime_number_box_KeyUp(sender, e);
            hidden.Focus();
        }

        private void HighContrastCheckbox_Click(object sender, RoutedEventArgs e)
        {
            settingsObj["HighContrast"] = HighContrastCheckbox.IsChecked.Value;
            highContrast = HighContrastCheckbox.IsChecked.Value;
            Save();
        }

        private void Autolist_Click(object sender, RoutedEventArgs e)
        {
            settingsObj["AutoList"] = Autolist.IsChecked.Value;
            automaticListing = Autolist.IsChecked.Value;
            Save();
        }

        private void Displaytime_number_box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                hidden.Focus();
            }
            var num = Regex.Replace(Displaytime_number_box.Text, "[^0-9.]", "");
            Displaytime_number_box.Text = num;
        }

        private void Displaytime_number_box_GotFocus(object sender, RoutedEventArgs e)
        {
            Displaytime_number_box.Text = "";
        }

        private void Displaytime_number_box_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                var num = Regex.Replace(Displaytime_number_box.Text, "[^0-9.]", "");
                delay = int.Parse(num);
                settingsObj["Delay"] = delay;
                Save();
            }
            catch (Exception exception)
            {
                Main.AddLog($"Unable to parse display time change, new val would have been: {Displaytime_number_box.Text} Exception: {exception}");
                Displaytime_number_box.Text = settingsObj["Delay"].ToString();
            }
        }

        private void OverlayXOffset_number_box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                hidden.Focus();
            }
            var numStr = Regex.Replace(OverlayXOffset_number_box.Text, @"[^-?\d]+$", "");
            OverlayXOffset_number_box.Text = numStr;
        }

        private void OverlayXOffset_number_box_GotFocus(object sender, RoutedEventArgs e)
        {
            OverlayXOffset_number_box.Text = "";
        }
        
        private void OverlayXOffset_number_box_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsValidOverlayOffset(OverlayXOffset_number_box.Text))
            {
                overlayXOffsetValue = ParseOverlayOffsetStringToInt(OverlayXOffset_number_box.Text);

                int width = 2000; // presume bounding
                if (OCR.VerifyWarframe())
                {
                    if (OCR.window == null || OCR.window.Width == 0 || OCR.window.Height == 0)
                    {
                        OCR.UpdateWindow(); // ensures our window bounds are set, or at least marked for BS
                    }
                    width = OCR.window.Width;
                }
                overlayXOffsetValue = (overlayXOffsetValue <= -1 * width / 2) ? (-1 * width / 2) : (overlayXOffsetValue >= width / 2) ? (width / 2) : overlayXOffsetValue; // clamp value to valid bound

                settingsObj["OverlayXOffsetValue"] = overlayXOffsetValue;
                OverlayXOffset_number_box.Text = overlayXOffsetValue.ToString(Main.culture);
                Save();
            }
        }

        private void OverlayYOffset_number_box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                hidden.Focus();
            }
            var numStr = Regex.Replace(OverlayYOffset_number_box.Text, @"[^-?\d]+$", "");
            OverlayYOffset_number_box.Text = numStr;
        }

        private void OverlayYOffset_number_box_GotFocus(object sender, RoutedEventArgs e)
        {
            OverlayYOffset_number_box.Text = "";
        }

        private void OverlayYOffset_number_box_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsValidOverlayOffset(OverlayYOffset_number_box.Text))
            {
                // -1 is for inverting the y-coord so that the user is presented with an increasing value from bottom to top
                overlayYOffsetValue = (-1) * ParseOverlayOffsetStringToInt(OverlayYOffset_number_box.Text);

                int height = 2000; // presume bounding
                if (OCR.VerifyWarframe())
                {
                    if (OCR.window == null || OCR.window.Width == 0 || OCR.window.Height == 0)
                    {
                        OCR.UpdateWindow(); // ensures our window bounds are set, or at least marked for BS
                    }
                    height = OCR.window.Height;
                }
                overlayYOffsetValue = (overlayYOffsetValue <= -1 * height / 2) ? (-1 * height / 2) : (overlayYOffsetValue >= height / 2) ? (height / 2) : overlayYOffsetValue; // clamp value to valid bound

                settingsObj["OverlayYOffsetValue"] = overlayYOffsetValue;
                OverlayYOffset_number_box.Text = (-1 * overlayYOffsetValue).ToString(Main.culture);
                Save();
            }
        }

        private bool IsValidOverlayOffset(string offsetValue)
        {
            string pattern = @"[^\d]+$";
            return !Regex.IsMatch(offsetValue, pattern);
        }

        private int ParseOverlayOffsetStringToInt(string offset)
        {
            try
            {
                var num = Regex.Replace(offset, @"[^-?\d]+$", "");
                return int.Parse(num, Main.culture);
            }
            catch (Exception exception)
            {
                Main.AddLog($"Unable to parse overlay offset value, new val would have been: {offset} Exception: {exception}");
                return 0;
            }
        }

        private void EfficiencyMin_number_box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                hidden.Focus();
            }
            var num = Regex.Replace(EfficiencyMin_number_box.Text, "[^0-9.,]", "");
            EfficiencyMin_number_box.Text = num;
        }

        private void EfficiencyMax_number_box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                hidden.Focus();
            }
            var num = Regex.Replace(EfficiencyMax_number_box.Text, "[^0-9.,]", "");
            EfficiencyMax_number_box.Text = num;
        }

        private void EfficiencyMin_number_box_GotFocus(object sender, RoutedEventArgs e)
        {
            EfficiencyMin_number_box.Text = "";
        }

        private void EfficiencyMax_number_box_GotFocus(object sender, RoutedEventArgs e)
        {
            EfficiencyMax_number_box.Text = "";
        }

        private void EfficiencyMin_number_box_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var num = Regex.Replace(EfficiencyMin_number_box.Text, "[^0-9.,]", "");
                num = num.Replace(',', '.');
                minimumEfficiencyValue = double.Parse(num, Main.culture);
                if (minimumEfficiencyValue > maximumEfficiencyValue)
                    throw new Exception("Minimum efficiency can not be more than maximum.");
                EfficiencyMin_number_box.Text = num;
                settingsObj["MinimumEfficiencyValue"] = minimumEfficiencyValue;
                Save();
            }
            catch (Exception exception)
            {
                Main.AddLog($"Unable to parse efficinecy min change, new val would have been: {EfficiencyMin_number_box.Text} Exception: {exception}");
                EfficiencyMin_number_box.Text = settingsObj["MinimumEfficiencyValue"].ToString();
            }
        }

        private void EfficiencyMax_number_box_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var num = Regex.Replace(EfficiencyMax_number_box.Text, "[^0-9.,]", "");
                num = num.Replace(',', '.');
                maximumEfficiencyValue = double.Parse(num, Main.culture);
                if (maximumEfficiencyValue < minimumEfficiencyValue)
                    throw new Exception("Maximum efficiency can not be less than minimum.");
                EfficiencyMax_number_box.Text = num;
                settingsObj["MaximumEfficiencyValue"] = maximumEfficiencyValue;
                Save();
            }
            catch (Exception exception)
            {
                Main.AddLog($"Unable to parse efficinecy max change, new val would have been: {Displaytime_number_box.Text} Exception: {exception}");
                EfficiencyMax_number_box.Text = settingsObj["MaximumEfficiencyValue"].ToString();
            }
            
        }

        private void SnapItCountThreshold_number_box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                hidden.Focus();
            }
            var num = Regex.Replace(SnapItCountThreshold_number_box.Text, "[^0-9.]", "");
            SnapItCountThreshold_number_box.Text = num;
        }

        private void SnapItCountThreshold_number_box_GotFocus(object sender, RoutedEventArgs e)
        {
            SnapItCountThreshold_number_box.Text = "";
        }

        private void SnapItCountThreshold_number_box_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                var num = Regex.Replace(SnapItCountThreshold_number_box.Text, "[^0-9.]", "");
                snapItCountThreshold = int.Parse(num);
                settingsObj["SnapItCountThreshold"] = snapItCountThreshold;
                Save();
            }
            catch (Exception exception)
            {
                Main.AddLog($"Unable to parse snapit threshold change, new val would have been: {SnapItCountThreshold_number_box.Text} Exception: {exception}");
                SnapItCountThreshold_number_box.Text = settingsObj["SnapItCountThreshold"].ToString();
            }
        }

        private void SnapItemCountCheckbox_Click(object sender, RoutedEventArgs e)
        {
            settingsObj["DoSnapItCount"] = SnapItemCountCheckbox.IsChecked.Value;
            doSnapItCount = SnapItemCountCheckbox.IsChecked.Value;
            Save();
        }
    }
}

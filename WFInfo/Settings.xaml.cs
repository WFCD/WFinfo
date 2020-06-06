using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

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
        public static Key DebugModifierKey;
        public static Key SnapitModifierKey;
        public static Key SearchItModifierKey;
        public static KeyConverter converter = new KeyConverter();
        public static Point mainWindowLocation;
        public static bool isOverlaySelected;
        public static bool isLightSlected;
        public static bool debug;
        public static long autoDelay;
        public static int imageRetentionTime;
        public static string ClipboardTemplate;
        internal static int delay;
        public static bool Highlight;
        internal static bool highContrast;

        public static bool auto { get; internal set; }
        public static bool clipboard { get; internal set; }
        public static bool detectScaling { get; internal set; }
        public static bool SnapitExport { get; internal set; }
        public static bool ClipboardVaulted { get; internal set; }

        public Settings()
        {
            InitializeComponent();
        }

        public void populate()
        {
            DataContext = this;

            if (settingsObj.GetValue("Display").ToString() == "Overlay")
            {
                OverlayRadio.IsChecked = true;
            }
            else if (settingsObj.GetValue("Display").ToString() == "Light")
            {
                LightRadio.IsChecked = true;
            }
            else
            {
                WindowRadio.IsChecked = true;
            }

            if (Convert.ToBoolean(settingsObj.GetValue("Auto")))
                autoCheckbox.IsChecked = true;

            if (Convert.ToBoolean(settingsObj.GetValue("Clipboard")))
                clipboardCheckbox.IsChecked = true;

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
            isLightSlected = false;
            clipboardCheckbox.IsChecked = (bool)settingsObj["Clipboard"];
            clipboardCheckbox.IsEnabled = true;
            Save();
        }

        private void OverlayChecked(object sender, RoutedEventArgs e)
        {
            settingsObj["Display"] = "Overlay";
            isOverlaySelected = true;
            isLightSlected = false;
            clipboardCheckbox.IsChecked = (bool)settingsObj["Clipboard"];
            clipboardCheckbox.IsEnabled = true;
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
            isLightSlected = true;
            clipboard = true;
            clipboardCheckbox.IsChecked = true;
            clipboardCheckbox.IsEnabled = false;
            Save();
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using WebSocketSharp;

namespace WFInfo
{
    public class ApplicationSettings : IReadOnlyApplicationSettings
    {
        public static IReadOnlyApplicationSettings GlobalReadonlySettings => GlobalSettings;
        internal static ApplicationSettings GlobalSettings { get; } = new ApplicationSettings();
        [JsonIgnore]
        public bool Initialized { get; set; } = false;
        public Display Display { get; set; } = Display.Overlay;
        [JsonProperty]
        public double MainWindowLocation_X { get; private set; } = 300;
        [JsonProperty]
        public double MainWindowLocation_Y { get; private set; } = 300;

        [JsonIgnore]
        public Point MainWindowLocation
        {
            get => new Point(MainWindowLocation_X, MainWindowLocation_Y);
            set
            {
                MainWindowLocation_X = value.X;
                MainWindowLocation_Y = value.Y;
            }
        }

        [JsonIgnore]
        public bool IsOverlaySelected => Display == Display.Overlay;
        [JsonIgnore]
        public bool IsLightSelected => Display == Display.Light;
        public string ActivationKey { get; set; } = "Snapshot";
        // public Key ActivationKeyKey { get; } = Key.None;
        // public MouseButton ActivationMouseButton { get;} = MouseButton.Left;
        public Key DebugModifierKey { get; set; } = Key.LeftShift;
        public Key SearchItModifierKey { get; set; } = Key.OemTilde;
        public Key SnapitModifierKey { get; set; } = Key.LeftCtrl;
        public Key MasterItModifierKey { get; set; } = Key.RightCtrl;
        public bool Debug { get; set; } = false;
        public string Locale { get; set; } = "en";
        public bool Clipboard { get; set; } = false;
        public long AutoDelay { get; set; } = 250L;
        public int ImageRetentionTime { get; set; } = 12;
        public string ClipboardTemplate { get; set; } = "-- by WFInfo (smart OCR with pricecheck)";
        public bool SnapitExport { get; set; } = false;
        public int Delay { get; set; } = 10000;
        public bool HighlightRewards { get; set; } = true;
        public bool ClipboardVaulted { get; set; } = false;
        public bool Auto { get; set; } = false;
        public bool HighContrast { get; set; } = false;
        public int OverlayXOffsetValue { get; set; } = 0;
        public int OverlayYOffsetValue { get; set; } = 0;
        public bool AutoList { get; set; } = false;
        public bool DoDoubleCheck { get; set; } = true;
        public double MaximumEfficiencyValue { get; set; } = 9.5;
        public double MinimumEfficiencyValue { get; set; } = 4.5;
        public bool DoSnapItCount { get; set; } = true;
        public int SnapItCountThreshold { get; set; } = 0;
        public int SnapItEdgeWidth { get; set; } = 1;
        public int SnapItEdgeRadius { get; set; } = 1;
        public double SnapItHorizontalNameMargin { get; set; } = 0;
        public bool DoCustomNumberBoxWidth { get; set; } = false;
        public double SnapItNumberBoxWidth { get; set; } = 0.4;
        public bool SnapMultiThreaded { get; set; } = true;
        public double SnapRowTextDensity { get; set; } = 0.015;
        public double SnapRowEmptyDensity { get; set; } = 0.01;
        public double SnapColEmptyDensity { get; set; } = 0.005;
        public string Ignored { get; set; } = null;
        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            Main.AddLog("Failed to parse settings file: " + errorContext.Error.Message);
            errorContext.Handled = true;
        }
 
    }

    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private readonly SettingsViewModel _viewModel;
        public SettingsViewModel SettingsViewModel => _viewModel;

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
        public static KeyConverter converter = new KeyConverter();

        public Settings()
        {
            
            InitializeComponent();
            DataContext = this;
            // DataContext = SettingsViewModel.Instance;
            _viewModel = SettingsViewModel.Instance;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(SettingsViewModel.OverlayYOffsetValue))
            {
                
            }
        }

        public void populate()
        {
            // DataContext = this;

            Overlay_sliders.Visibility = Visibility.Collapsed; // default hidden for the majority of states

            if (_viewModel.Display == Display.Overlay)
            {
                OverlayRadio.IsChecked = true;
                Overlay_sliders.Visibility = Visibility.Visible;
            }
            else if (_viewModel.Display == Display.Light)
            {
                LightRadio.IsChecked = true;
            }
            else
            {
                WindowRadio.IsChecked = true;
            }

            if (_viewModel.Auto)
            {
                autoCheckbox.IsChecked = true;
                Autolist.IsEnabled = true;
            }
            else
            {
                Autolist.IsEnabled = false;
            }

            foreach (ComboBoxItem localeItem in localeCombobox.Items)
            {
                if(_viewModel.Locale.Equals(localeItem.Tag.ToString()))
                {
                    localeItem.IsSelected = true;
                }
            }

            OverlayXOffset_number_box.Text = _viewModel.OverlayXOffsetValue.ToString(Main.culture);
            OverlayYOffset_number_box.Text = (-1 * _viewModel.OverlayYOffsetValue).ToString(Main.culture);

            EfficiencyMax_number_box.Text = _viewModel.MaximumEfficiencyValue.ToString(Main.culture);
            EfficiencyMin_number_box.Text = _viewModel.MinimumEfficiencyValue.ToString(Main.culture);
            Displaytime_number_box.Text = _viewModel.Delay.ToString(Main.culture);

            SnapItCountThreshold_number_box.Text = _viewModel.SnapItCountThreshold.ToString(Main.culture);

            ResetActivationKeyText();
            Focus();
        }

        public static void Save()
        {
            WFInfo.SettingsViewModel.Instance.Save();
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
            _viewModel.Display = Display.Window;
            Overlay_sliders.Visibility = Visibility.Collapsed;
            clipboardCheckbox.IsEnabled = true;
            Save();
        }

        private void OverlayChecked(object sender, RoutedEventArgs e)
        {
            _viewModel.Display = Display.Overlay;
            Overlay_sliders.Visibility = Visibility.Visible;
            clipboardCheckbox.IsEnabled = true;
            Save();
        }

        private void AutoClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.Auto = autoCheckbox.IsChecked.Value;
            if (_viewModel.Auto)
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
                    _viewModel.Auto = false;
                    autoCheckbox.IsChecked = false;
                    Main.dataBase.DisableLogCapture();
                    Autolist.IsEnabled = false;
                }
            }
            else
            {
                _viewModel.Auto = false;
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
                    _viewModel.ActivationKey = key.ToString();
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

            if (e.Key == _viewModel.SearchItModifierKey || e.Key == _viewModel.SnapitModifierKey || e.Key == _viewModel.MasterItModifierKey)
            {
                Activation_key_box.Text = GetKeyName(ActivationKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            ActivationKey = key;
            Activation_key_box.Text = GetKeyName(ActivationKey);
            _viewModel.ActivationKey = key.ToString();
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

            Searchit_key_box.Text = GetKeyName(_viewModel.SearchItModifierKey);
            Snapit_key_box.Text = GetKeyName(_viewModel.SnapitModifierKey);
            Masterit_key_box.Text = GetKeyName(_viewModel.MasterItModifierKey);
        }

        private void ActivationLost(object sender, RoutedEventArgs e)
        {
            ResetActivationKeyText();
        }

        private void ClickCreateDebug(object sender, RoutedEventArgs e)
        {
            Main.SpawnErrorPopup(DateTime.UtcNow, 1800);
        }

        private void localeComboboxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ComboBoxItem item = (ComboBoxItem) localeCombobox.SelectedItem;
            
            string selectedLocale = item.Tag.ToString();
            _viewModel.Locale = selectedLocale;
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
            _viewModel.Display = Display.Light;
            Overlay_sliders.Visibility = Visibility.Collapsed;
            _viewModel.Clipboard = true;
            clipboardCheckbox.IsChecked = true;
            clipboardCheckbox.IsEnabled = false;
            Save();
        }

        private void Searchit_key_box_GotFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.SearchItModifierKey = Key.None;
            Searchit_key_box.Text = "";
            backupKeyVal = activeKeyVal;
            activeKeyVal = Key.NoName;
        }

        private void Searchit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == backupKeyVal || e.Key == _viewModel.SnapitModifierKey || e.Key == _viewModel.MasterItModifierKey)
            {
                Searchit_key_box.Text = GetKeyName(_viewModel.SearchItModifierKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            _viewModel.SearchItModifierKey = key;
            Searchit_key_box.Text = GetKeyName(key);
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

            if (e.Key == backupKeyVal || e.Key == _viewModel.SearchItModifierKey || e.Key == _viewModel.MasterItModifierKey)
            {
                Snapit_key_box.Text = GetKeyName(_viewModel.SnapitModifierKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            _viewModel.SnapitModifierKey = key;
            Snapit_key_box.Text = GetKeyName(key);
            hidden.Focus();
            Save();
        }

        private void Snapit_key_box_GotFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.SnapitModifierKey = Key.None;
            Snapit_key_box.Text = "";
            backupKeyVal = activeKeyVal;
            activeKeyVal = Key.NoName;
        }

        private void Masterit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == backupKeyVal || e.Key == _viewModel.SearchItModifierKey || e.Key == _viewModel.SnapitModifierKey)
            {
                Masterit_key_box.Text = GetKeyName(_viewModel.MasterItModifierKey);
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            _viewModel.MasterItModifierKey = key;
            Masterit_key_box.Text = GetKeyName(key);
            hidden.Focus();
            Save();
        }

        private void Masterit_key_box_GotFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.MasterItModifierKey = Key.None;
            Masterit_key_box.Text = "";
            backupKeyVal = activeKeyVal;
            activeKeyVal = Key.NoName;
        }


    }
}

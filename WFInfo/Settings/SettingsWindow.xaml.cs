using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WFInfo.Settings
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;
        public SettingsViewModel SettingsViewModel => _viewModel;

        public static KeyConverter converter = new KeyConverter();

        public SettingsWindow()
        {
            
            InitializeComponent();
            DataContext = this;
            _viewModel = SettingsViewModel.Instance;
        }

        public void populate()
        {
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

            Focus();
        }

        public static void Save()
        {
            SettingsViewModel.Instance.Save();
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


        public bool IsActivationFocused => Activation_key_box.IsFocused;

        private void ActivationMouseDown(object sender, MouseEventArgs e)
        {
            if (IsActivationFocused)
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
                    _viewModel.ActivationKey = key.ToString();
                    hidden.Focus();
                }
            }
        }

        private void ActivationUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == _viewModel.SearchItModifierKey || e.Key == _viewModel.SnapitModifierKey || e.Key == _viewModel.MasterItModifierKey)
            {
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            _viewModel.ActivationKey = key.ToString();
            hidden.Focus();
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


        private void LightRadioChecked(object sender, RoutedEventArgs e)
        {
            _viewModel.Display = Display.Light;
            Overlay_sliders.Visibility = Visibility.Collapsed;
            _viewModel.Clipboard = true;
            clipboardCheckbox.IsChecked = true;
            clipboardCheckbox.IsEnabled = false;
            Save();
        }

        private void Searchit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if  (e.Key == _viewModel.SnapitModifierKey || e.Key == _viewModel.MasterItModifierKey)
            {
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            _viewModel.SearchItModifierKey = key;
            hidden.Focus();
        }

        private void Snapit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == _viewModel.SearchItModifierKey || e.Key == _viewModel.MasterItModifierKey)
            {
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            _viewModel.SnapitModifierKey = key;
            hidden.Focus();
        }


        private void Masterit_key_box_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == _viewModel.SearchItModifierKey || e.Key == _viewModel.SnapitModifierKey)
            {
                hidden.Focus();
                return;
            }

            Key key = e.Key != Key.System ? e.Key : e.SystemKey;
            _viewModel.MasterItModifierKey = key;
            hidden.Focus();
        }

        private void ConfigureTheme_button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ThemeAdjuster.ShowThemeAdjuster();
        }
    }
}
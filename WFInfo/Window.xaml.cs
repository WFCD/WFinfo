using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window : System.Windows.Window
    {

        public Window()
        {
            InitializeComponent();
        }
        public void loadTextData(string name, string plat, string ducats, string volume, bool vaulted, string owned, int partNumber, bool resize = true)
        {
            Top = MainWindow.INSTANCE.Top + 150;
            Show();
            switch (partNumber)
            {
                case 0:
                    firstPartText.Text = name;
                    firstPlatText.Text = plat;
                    firstDucatText.Text = ducats;
                    firstVolumeText.Text = volume + " sold last 48hrs";
                    firstVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    firstOwnedText.Text = owned.Length > 0 ? owned + " owned" : "";
                    if (resize)
                        Width = 251;
                    break;

                case 1:
                    secondPartText.Text = name;
                    secondPlatText.Text = plat;
                    secondDucatText.Text = ducats;
                    secondVolumeText.Text = volume + " sold last 48hrs";
                    secondVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    secondOwnedText.Text = owned.Length > 0 ? owned + " owned" : "";
                    if (resize)
                        Width = 501;
                    break;

                case 2:
                    thirdPartText.Text = name;
                    thirdPlatText.Text = plat;
                    thirdDucatText.Text = ducats;
                    thirdVolumeText.Text = volume + " sold last 48hrs";
                    thirdVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    thirdOwnedText.Text = owned.Length > 0 ? owned + " owned" : "";
                    if (resize)
                        Width = 751;
                    break;

                case 3:
                    fourthPartText.Text = name;
                    fourthPlatText.Text = plat;
                    fourthDucatText.Text = ducats;
                    fourthVolumeText.Text = volume + " sold last 48hrs";
                    fourthVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    fourthOwnedText.Text = owned.Length > 0 ? owned + " owned" : "";
                    if (resize)
                        Width = 1000;
                    break;

                default:
                    Main.AddLog("something went wrong while displaying: " + name);
                    Main.StatusUpdate("something went wrong while displaying: " + name + " in window", 1);
                    break;
            }
            if (resize)
                Left = MainWindow.INSTANCE.Left + 150 - (Width / 2);
        }
        private void Exit(object sender, RoutedEventArgs e)
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

using System;
using System.CodeDom;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class RewardWindow : Window
    {
        public RewardWindow()
        {
            InitializeComponent();
        }
        public void loadTextData(string name, string plat, string ducats, string volume, bool vaulted, bool mastered, string owned, int partNumber, bool resize = true, bool hideReward = false)
        {
            Show();
            Topmost = true;
            switch (partNumber)
            {
                case 0:
                    firstPartText.Text = name;
                    if (hideReward)
                    {
                        platImage.Visibility = Visibility.Hidden;
                        firstDucatImage.Visibility = Visibility.Hidden;
                        firstPlatText.Text = string.Empty;
                        firstDucatText.Text = string.Empty;
                        firstVolumeText.Text = string.Empty;
                        firstVaultedMargin.Visibility = Visibility.Hidden;
                        firstOwnedText.Text = string.Empty;
                        if (resize)
                            Width = 251;
                        break;
                    }
                    platImage.Visibility = Visibility.Visible;
                    firstDucatImage.Visibility = Visibility.Visible;
                    firstPlatText.Text = plat;
                    firstDucatText.Text = ducats;
                    firstVolumeText.Text = volume + " sold last 48hrs";
                    firstVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    firstOwnedText.Text = owned.Length > 0 ? (mastered ? "✓ " : "") + owned + " OWNED" : "";
                    if (resize)
                        Width = 251;
                    break;

                case 1:
                    secondPartText.Text = name;
                    if (hideReward)
                    {
                        platImage1.Visibility = Visibility.Hidden;
                        firstDucatImage1.Visibility = Visibility.Hidden;
                        secondPlatText.Text = string.Empty;
                        secondDucatText.Text = string.Empty;
                        secondVolumeText.Text = string.Empty;
                        secondVaultedMargin.Visibility = Visibility.Hidden;
                        secondOwnedText.Text = string.Empty;
                        if (resize)
                            Width = 501;
                    }
                    platImage1.Visibility = Visibility.Visible;
                    firstDucatImage1.Visibility = Visibility.Visible;
                    secondPlatText.Text = plat;
                    secondDucatText.Text = ducats;
                    secondVolumeText.Text = volume + " sold last 48hrs";
                    secondVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    secondOwnedText.Text = owned.Length > 0 ? (mastered ? "✓ " : "") + owned + " OWNED" : "";
                    if (resize)
                        Width = 501;
                    break;

                case 2:
                    thirdPartText.Text = name;
                    if (hideReward)
                    {
                        platImage2.Visibility = Visibility.Hidden;
                        firstDucatImage2.Visibility = Visibility.Hidden;
                        thirdPlatText.Text = string.Empty;
                        thirdDucatText.Text = string.Empty;
                        thirdVolumeText.Text = string.Empty;
                        thirdVaultedMargin.Visibility = Visibility.Hidden;
                        thirdOwnedText.Text = string.Empty;
                        if (resize)
                            Width = 751;
                    }
                    platImage2.Visibility = Visibility.Visible;
                    firstDucatImage2.Visibility = Visibility.Visible;
                    thirdPlatText.Text = plat;
                    thirdDucatText.Text = ducats;
                    thirdVolumeText.Text = volume + " sold last 48hrs";
                    thirdVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    thirdOwnedText.Text = owned.Length > 0 ? (mastered ? "✓ " : "") + owned + " OWNED" : "";
                    if (resize)
                        Width = 751;
                    break;

                case 3:
                    fourthPartText.Text = name;
                    if (hideReward)
                    {
                        platImage3.Visibility = Visibility.Hidden;
                        firstDucatImage3.Visibility = Visibility.Hidden;
                        fourthPlatText.Text = string.Empty;
                        fourthDucatText.Text = string.Empty;
                        fourthVolumeText.Text = string.Empty;
                        fourthVaultedMargin.Visibility = Visibility.Hidden;
                        fourthOwnedText.Text = string.Empty;
                        if (resize)
                            Width = 1000;
                    }
                    platImage3.Visibility = Visibility.Visible;
                    firstDucatImage3.Visibility = Visibility.Visible;
                    fourthPlatText.Text = plat;
                    fourthDucatText.Text = ducats;
                    fourthVolumeText.Text = volume + " sold last 48hrs";
                    fourthVaultedMargin.Visibility = vaulted ? Visibility.Visible : Visibility.Hidden;
                    fourthOwnedText.Text = owned.Length > 0 ? (mastered ? "✓ " : "") + owned + " OWNED" : "";
                    if (resize)
                        Width = 1000;
                    break;

                default:
                    Main.AddLog("something went wrong while displaying: " + name);
                    Main.StatusUpdate("something went wrong while displaying: " + name + " in window", 1);
                    break;
            }
        }
        private void Exit(object sender, RoutedEventArgs e)
        {
            Topmost = false;
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

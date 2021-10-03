using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WebSocketSharp;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for SearchIt.xaml
    /// </summary>
    /// 

    public partial class SearchIt : Window
    {

        public SearchIt()
        {
            InitializeComponent();
        }

        public bool IsInUse { get; set; } = false;
        /// <summary>
        /// Launch snapit, prompts user if not logged in
        /// </summary>
        public void Start()
        {
            Main.searchBox.Show();
            MainWindow.INSTANCE.Topmost = true;
            Main.searchBox.placeholder.Content = "Search for warframe.market Items";
            if (!Main.dataBase.IsJwtAvailable())
            {
                Main.searchBox.placeholder.Content = "Please log in first";
                Main.login.MoveLogin(Left, Main.searchBox.Top - 130);
                return;
            }
            MainWindow.INSTANCE.Topmost = false;
            IsInUse = true;
            Main.searchBox.Show();
            searchField.Focusable = true;
            Main.searchBox.Topmost = true;
            Win32.BringToFront(Process.GetCurrentProcess());
        }
        /// <summary>
        /// Stats a search, it will try to get the closest item from the search box and spawn a create listing screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search(object sender, RoutedEventArgs e)
        {
            try
            {
                var closest = Main.dataBase.GetPartNameHuman(searchField.Text, out _);
                var primeRewards = new List<string> { closest };
                var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(primeRewards, false)).Result;
                Main.listingHelper.ScreensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
                if (!Main.listingHelper.IsVisible)
                {
                    Main.listingHelper.SetScreen(Main.listingHelper.ScreensList.Count - 1);
                }
                Main.listingHelper.Show();
                Main.listingHelper.BringIntoView();
            }
            catch (Exception exception)
            {
                Main.AddLog(exception.ToString());
            }
            Finish();
        }

        /// <summary>
        /// Reset the search box back to original status and then hide it
        /// </summary>
        internal void Finish()
        {
            searchField.Text = "";
            placeholder.Visibility = Visibility.Visible;
            searchField.Focusable = false;
            IsInUse = false;
            Hide();
        }
        /// <summary>
        /// Helper method to remove the placeholder text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!searchField.Text.IsNullOrEmpty())
                placeholder.Visibility = Visibility.Hidden;
        }
    }
}



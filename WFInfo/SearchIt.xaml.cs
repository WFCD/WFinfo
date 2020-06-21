using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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

        public bool isInUse = false;
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
            isInUse = true;
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
                var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(primeRewards)).Result;
                Main.listingHelper.screensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
                Main.listingHelper.SetScreen(Main.listingHelper.screensList.Count-1);
                Main.listingHelper.Show();
            }
            catch (Exception exception)
            {
	            Main.AddLog(exception.ToString());
            }
            finish();
        }

        /// <summary>
        /// Reset the search box back to original status and then hide it
        /// </summary>
        internal void finish()
        {
            searchField.Text = "";
            placeholder.Visibility = Visibility.Visible;
            searchField.Focusable = false;
            isInUse = false;
            Hide();
        }
        /// <summary>
        /// Helper method to remove the placeholder text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textChanged(object sender, TextChangedEventArgs e)
        {
            if (searchField.Text != "")
                placeholder.Visibility = Visibility.Hidden;
        }
    }
}



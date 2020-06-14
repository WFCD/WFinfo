﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
        public async void Start()
        {
	        Search(null, null);
	        //     Main.searchBox.Show();
	        //     MainWindow.INSTANCE.Topmost = true;
	        //     Main.searchBox.placeholder.Content = "Search for warframe.market Items";
	        //        if (!await Main.dataBase.IsJWTvalid())
	        //        {
	        //            Main.searchBox.placeholder.Content = "Please log in first";
	        //Main.login.MoveLogin(Left, Main.searchBox.Top - 130);
	        //            return;
	        //     }
	        //        MainWindow.INSTANCE.Topmost = false;
	        //        isInUse = true;
	        //        Main.searchBox.Show();
	        //        searchField.Focusable = true;
        }
        /// <summary>
        /// Stats a search, it will try to get the closest item from the search box and spawn a create listing screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search(object sender, RoutedEventArgs e)
        {
            //var closest = Main.dataBase.GetPartNameHuman(searchField.Text, out _);
            //var primeRewards = new List<string> { closest };
            //var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(primeRewards)).Result;
            //Main.listingHelper.screensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
            //Main.listingHelper.SetScreen(0);
            try
            {
	            foreach (var rewardscreen in Main.listingHelper.primeRewards) {
		            var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(rewardscreen)).Result;
		            Main.listingHelper.screensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
	            }
	            Main.listingHelper.SetScreen(0);
	            Main.listingHelper.Show();
            }
            catch (Exception exception)
            {
	            Console.WriteLine(exception);
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



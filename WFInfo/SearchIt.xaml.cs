using System;
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
        }

        private void Search(object sender, RoutedEventArgs e)
        {
            var closest = Main.dataBase.GetPartNameHuman(searchField.Text, out _);
            var primeRewards = new List<string> { closest };
            var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(primeRewards)).Result;
            Main.listingHelper.screensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
            Main.listingHelper.SetScreen(0);
            Main.listingHelper.Show();


            finish();
        }
        internal void finish()
        {
            searchField.Text = "";
            placeholder.Visibility = Visibility.Visible;
            searchField.Focusable = false;
            isInUse = false;
            Hide();
        }

        private void textChanged(object sender, TextChangedEventArgs e)
        {
            if (searchField.Text != "")
                placeholder.Visibility = Visibility.Hidden;
            
        }
    }
}



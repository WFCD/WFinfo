using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for CreateListing.xaml
	/// </summary>
	public partial class CreateListing : Window {

		#region default methods
		public CreateListing() {
			InitializeComponent();
		}

		private void Hide(object sender, RoutedEventArgs e) {
			Hide();
		}

		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
		#endregion

		public void populate()
		{

		}

		public MarketListing getMarketListing(string primeName) 
		{
			var results = Main.dataBase.getTopListings(primeName);
			Console.WriteLine(results.ToString());
			return null;
		}
	}




	/// <summary>
	/// Class to represent a single "sheet" of the create listing screen, consisting of up to 4 rewards and up to 5 active "online in game" listings 
	/// </summary>
	public class RewardCollection
	{
		private List<string> primeNames = new List<string>(); // the reward items in case user wants to change selection
		private List<int> platinumValues = new List<int>();
		private List<MarketListing> listings = new List<MarketListing>();
	}
	/// <summary>
	/// Class to represent a single listing of an item, usually comes in groups of 5
	/// </summary>
	public class MarketListing
	{
		private int platinum; // plat amount of listing
		private int amount; //amount user lists
		private int ranking; //n'th ranking of the listing 0 through 4

		public MarketListing(int platinum, int amount, int ranking)
		{
			platinum = this.platinum;
			amount = this.amount;
			ranking = this.ranking;
		}
	}
}

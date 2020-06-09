using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json.Linq;

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

		public RewardCollection GetRewardCollection(List<string> primeName)
		{
			List<int> platinumValues = new List<int>(4);
			List<int> listedQuantity = new List<int>(4);
			List<MarketListing> marketListings = new List<MarketListing>(4);
			foreach (string primeItem in primeName)
			{
				platinumValues.Add(Main.dataBase.marketData.GetValue(primeItem).ToObject<JObject>()["plat"].ToObject<int>());
				listedQuantity.Add(Main.dataBase.GetCurrentListedAmount(primeItem).Result);
			}
			return null;
		}

		public List<MarketListing> getMarketListing(string primeName) 
		{
			var results = Main.dataBase.getTopListings(primeName);
			List<MarketListing> listings = new List<MarketListing>();
			JArray sellOrders = new JArray(results["payload"]["sell_orders"].Children());
			foreach (var item in sellOrders)
			{
				var platinum = item.Value<int>("platinum");
				var amount = item.Value<int>("quantity");
				var reputation = item["user"].Value<int>("reputation");
				listings.Add(new MarketListing(platinum, amount, reputation));
				
				//Console.WriteLine("Current item: \n" +item);
			}
			//Console.WriteLine("All from sellOrders: \n" +sellOrders);
			return listings;
		}
	}




	/// <summary>
	/// Class to represent a single "sheet" of the create listing screen, consisting of up to 4 rewards and up to 5 active "online in game" listings 
	/// </summary>
	public class RewardCollection
	{
		private List<string> primeNames = new List<string>(4); // the reward items in case user wants to change selection
		private List<int> platinumValues = new List<int>(4);
		private List<int> listedQuantity = new List<int>(4);
		private List<MarketListing> marketListings = new List<MarketListing>(4);

		public RewardCollection(List<string> primeNames, List<int> platinumValues, List<MarketListing> marketListings, List<int> listedQuantity)
		{
			this.primeNames = primeNames;
			this.platinumValues = platinumValues;
			this.marketListings = marketListings;
			this.listedQuantity = listedQuantity;
		}
	}
	/// <summary>
	/// Class to represent a single listing of an item, usually comes in groups of 5
	/// </summary>
	public class MarketListing
	{
		private int platinum; // plat amount of listing
		private int amount; //amount user lists
		private int reputation; // user's reputation

		public MarketListing(int platinum, int amount, int reputation)
		{
			this.platinum = platinum;
			this.amount = amount;
			this.reputation = reputation;
		}

		public override string ToString()
		{
			return "Platinum: " + platinum + " Amount: " + amount + " Reputation: " + reputation;
		}
	}
}

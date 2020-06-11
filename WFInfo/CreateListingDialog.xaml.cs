using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
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

		/// <summary>
		/// returns the data for an entire "Create listing" screen
		/// </summary>
		/// <param name="primeNames">The human friendly name to search listings for</param>
		/// <returns>the data for an entire "Create listing" screen</returns>
		public async Task<RewardCollection> GetRewardCollection(List<string> primeNames)
		{
			var platinumValues = new List<int>(4);
			var listedQuantity = new List<int>(4);
			var marketListings = new List<List<MarketListing>>(5);

			foreach (var primeItem in primeNames)
			{
				var tempListings = getMarketListing(primeItem);
				marketListings.Add(tempListings);
				platinumValues.Add(tempListings[1].platinum);
				var listing = await Main.dataBase.GetCurrentListing(primeItem);
				listedQuantity.Add((int)listing?["quantity"]);
			}
			return new RewardCollection(primeNames, platinumValues, marketListings, listedQuantity);
		}
		/// <summary>
		/// Gets the top 5 current market listings
		/// </summary>
		/// <param name="primeName">The human friendly name to search listings for</param>
		/// <returns>the top 5 current market listings</returns>
		public List<MarketListing> getMarketListing(string primeName) 
		{
			var results = Main.dataBase.GetTopListings(primeName);
			var listings = new List<MarketListing>();
			var sellOrders = new JArray(results["payload"]["sell_orders"].Children());
			foreach (var item in sellOrders)
			{
				var platinum = item.Value<int>("platinum");
				var amount = item.Value<int>("quantity");
				var reputation = item["user"].Value<int>("reputation");
				listings.Add(new MarketListing(platinum, amount, reputation));
			}
			return listings;
		}
	}

	/// <summary>
	/// Class to represent a single "sheet" of the create listing screen, consisting of up to 4 possible rewards for which are unique plat, quantity and market listings 
	/// </summary>
	public class RewardCollection
	{
		public List<string> primeNames = new List<string>(4); // the reward items in case user wants to change selection
		public List<int> platinumValues = new List<int>(4);
		public List<int> listedQuantity = new List<int>(4);
		public List<List<MarketListing>> marketListings = new List<List<MarketListing>>(5);

		public RewardCollection(List<string> primeNames, List<int> platinumValues, List<List<MarketListing>> marketListings, List<int> listedQuantity)
		{
			this.primeNames = primeNames;
			this.platinumValues = platinumValues;
			this.marketListings = marketListings;
			this.listedQuantity = listedQuantity;
		}
		/// <summary>
		/// Gets a human friendly version back for logging.
		/// </summary>
		/// <returns></returns>
		public string ToHumanString()
		{
			var msg = "Reward collection screen:\n";
			foreach (var item in primeNames)
			{
				var index = primeNames.IndexOf(item);
				msg += $"Prime item: \"{item}\", Platinum value: \"{platinumValues[index]}\", Quantity: \"{listedQuantity[index]}\",  Market listings: \n";
				msg = marketListings[index].Aggregate(msg, (current, listing) => current + (listing.ToHumanString() + "\n"));
			}
			return msg;
		}
	}
	/// <summary>
	/// Class to represent a single listing of an item, usually comes in groups of 5
	/// </summary>
	public class MarketListing
	{
		public int platinum; // plat amount of listing
		public int amount; //amount user lists
		public int reputation; // user's reputation

		public MarketListing(int platinum, int amount, int reputation)
		{
			this.platinum = platinum;
			this.amount = amount;
			this.reputation = reputation;
		}
		/// <summary>
		/// Gets a human friendly version back for logging.
		/// </summary>
		/// <returns></returns>
		public string ToHumanString()
		{
			return "Platinum: " + platinum + " Amount: " + amount + " Reputation: " + reputation;
		}
	}
}

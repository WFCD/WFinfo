using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for CreateListing.xaml
	/// </summary>
	public partial class CreateListing : Window {

		//public List<RewardCollection> screensList = new List<RewardCollection>();
		public List<KeyValuePair<string, RewardCollection>> screensList = new List<KeyValuePair<string, RewardCollection>>();
		//KVP is for sucess status, 0 = initial, 1 = successful 2+ = error
		public int pageIndex = 0;
		private bool updating;
		#region default methods
		public CreateListing() {
			InitializeComponent();
		}

		private void Hide(object sender, RoutedEventArgs e) {
			screensList = new List<KeyValuePair<string, RewardCollection>>();
			pageIndex = 0;
			Hide();
		}

		// Allows the draging of the window
		private void OnMouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
		#endregion


		/// <summary>
		/// Sets the screen to one of the screen-lists indicated by it's index
		/// </summary>
		/// <param name="index">The index needed for the screen</param>
		public void SetScreen(int index)
		{
			SetCurrentStatus();

			if (screensList.Count < index || 0 > index )
			{
				Console.WriteLine($"Screen list is {screensList.Count} long and is: {screensList.Count}");
				throw new Exception("Tried setting screen to an item that didn't exist");
			}

			var screen = screensList[index];
			updating = true;
			SetListings(0);
			ComboBox.Items.Clear();
			ComboBox.SelectedIndex = 0;
			foreach (var primeItem in screen.Value.primeNames.Where(primeItem => !primeItem.IsNullOrEmpty()))
			{
				ComboBox.Items.Add(primeItem);
			}

			updating = false;
		}
		/// <summary>
		/// changes screen over if there is a follow up screen
		/// </summary>
		public void NextScreen(object sender, RoutedEventArgs e)
		{
			Back.IsEnabled = true;
			pageIndex++;
			SetScreen(pageIndex);
			if (screensList.Count - 1 == pageIndex) //reached the end of the list
				Next.IsEnabled = false;
			SetCurrentStatus();
			updating = true;
		}

		/// <summary>
		/// changes screen back if there is a previous screen
		/// </summary>
		public void PreviousScreen(object sender, RoutedEventArgs e)
		{
			Next.IsEnabled = true;
			pageIndex--;
			SetScreen(pageIndex);
			if (pageIndex == 0) //reached start of the list
				Back.IsEnabled = false;
			SetCurrentStatus();
		}

		private void SetCurrentStatus()
		{
			switch (screensList[pageIndex].Key)
			{
				//listing already successfully posted
				case "successful":
					ListingGrid.Visibility = Visibility.Collapsed;
					Height = 180;
					ConfirmListingLabel.IsEnabled = false;
					Status.Content = "Listing already successfully posted";
					Status.Visibility = Visibility.Visible;
					break;
				case "": //listing is not yet assigned anything
					Height = 255;
					Status.Visibility = Visibility.Collapsed;
					break;
				default: //an error occured.
					Height = 270;
					Status.Content = screensList[pageIndex].Key;
					Status.Visibility = Visibility.Visible;
					break;
			}
		}

		/// <summary>
		/// returns the data for an entire "Create listing" screen
		/// </summary>
		/// <param name="primeNames">The human friendly name to search listings for</param>
		/// <returns>the data for an entire "Create listing" screen</returns>
		public RewardCollection GetRewardCollection(List<string> primeNames)
		{
			var platinumValues = new List<int>(4);
			var marketListings = new List<List<MarketListing>>(5);

			foreach (var primeItem in primeNames)
			{
				if(primeItem.IsNullOrEmpty())
					continue;
				var tempListings = GetMarketListing(primeItem);
				marketListings.Add(tempListings);
				platinumValues.Add(tempListings[1].platinum);
			}
			return new RewardCollection(primeNames, platinumValues, marketListings);
		}
		/// <summary>
		/// Gets the top 5 current market listings
		/// </summary>
		/// <param name="primeName">The human friendly name to search listings for</param>
		/// <returns>the top 5 current market listings</returns>
		public List<MarketListing> GetMarketListing(string primeName) 
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
		
		/// <summary>
		/// List the current selected prime item with it's currently filled in plat value.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConfirmListing(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var success = Task.Run(async () => await PlaceListing()).Result;
				if (success) {
					var newEntry = new KeyValuePair<string, RewardCollection>("", screensList[pageIndex].Value);
					screensList.RemoveAt(pageIndex);
					screensList.Insert(pageIndex, newEntry);
				} else {
					var newEntry = new KeyValuePair<string, RewardCollection>("Something uncaught went wrong", screensList[pageIndex].Value);
					screensList.RemoveAt(pageIndex);
					screensList.Insert(pageIndex, newEntry);
				}
			}
			catch (Exception exception)
			{
				var newEntry = new KeyValuePair<string, RewardCollection>(exception.ToString(), screensList[pageIndex].Value);
				screensList.RemoveAt(pageIndex);
				screensList.Insert(pageIndex, newEntry);
			}

		}

		private async Task<bool> PlaceListing()
		{
			try
			{
				var screen = screensList[pageIndex];
				var primeItem = screen.Value.primeNames[ComboBox.SelectedIndex];
				var listing = await Main.dataBase.GetCurrentListing(primeItem);
				var platinum = int.Parse(PlatinumTextBox.Text);
				if (listing != null) return await Main.dataBase.ListItem(primeItem, platinum, 1);
				//listing already exists, thus update it
				var listingId = (string)listing?["id"];
				var quantity = (int)listing?["quantity"];
				return await Main.dataBase.updateListing(listingId, platinum, quantity);
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			if (!ComboBox.IsLoaded || updating) //Prevent firing off to early
				return;
			SetListings(ComboBox.SelectedIndex);
		}

		/// <summary>
		/// Sets the listing to the current selected prime item
		/// </summary>
		/// <param name="index">the currently selected prime item</param>
		private void SetListings(int index)
		{
			PlatinumTextBox.Text = screensList[pageIndex].Value.platinumValues[index].ToString();

			Platinum0.Content = screensList[pageIndex].Value.marketListings[index][0].platinum;
			Amount0.Content = screensList[pageIndex].Value.marketListings[index][0].amount;
			Reputation0.Content = screensList[pageIndex].Value.marketListings[index][0].reputation;

			Platinum1.Content = screensList[pageIndex].Value.marketListings[index][1].platinum;
			Amount1.Content = screensList[pageIndex].Value.marketListings[index][1].amount;
			Reputation1.Content = screensList[pageIndex].Value.marketListings[index][1].reputation;

			Platinum2.Content = screensList[pageIndex].Value.marketListings[index][2].platinum;
			Amount2.Content = screensList[pageIndex].Value.marketListings[index][2].amount;
			Reputation2.Content = screensList[pageIndex].Value.marketListings[index][2].reputation;

			Platinum3.Content = screensList[pageIndex].Value.marketListings[index][3].platinum;
			Amount3.Content = screensList[pageIndex].Value.marketListings[index][3].amount;
			Reputation3.Content = screensList[pageIndex].Value.marketListings[index][3].reputation;

			Platinum4.Content = screensList[pageIndex].Value.marketListings[index][4].platinum;
			Amount4.Content = screensList[pageIndex].Value.marketListings[index][4].amount;
			Reputation4.Content = screensList[pageIndex].Value.marketListings[index][4].reputation;
		}

		/// <summary>
		/// Cancels the current selection, removing it from the list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Cancel(object sender, RoutedEventArgs e) {
			if (screensList.Count == 1) // if it's the last item
				Hide(null, null);
			if (pageIndex == 0) // if looking at the first screen
			{
				SetScreen(1);
				screensList.RemoveAt(0);
			}else
			{
				screensList.RemoveAt(pageIndex);
				--pageIndex;
				SetScreen(pageIndex);
			}
		}
	}

	/// <summary>
	/// Class to represent a single "sheet" of the create listing screen, consisting of up to 4 possible rewards for which are unique plat, quantity and market listings 
	/// </summary>
	public class RewardCollection
	{
		public List<string> primeNames = new List<string>(4); // the reward items in case user wants to change selection
		public List<int> platinumValues = new List<int>(4);
		public List<List<MarketListing>> marketListings = new List<List<MarketListing>>(5);

		public RewardCollection(List<string> primeNames, List<int> platinumValues, List<List<MarketListing>> marketListings)
		{
			this.primeNames = primeNames;
			this.platinumValues = platinumValues;
			this.marketListings = marketListings;
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
				if (item.IsNullOrEmpty())
					continue;
				var index = primeNames.IndexOf(item);

				msg += $"Prime item: \"{item}\", Platinum value: \"{platinumValues[index]}\",  Market listings: \n";
				

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


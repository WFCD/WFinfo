using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for CreateListing.xaml
	/// </summary>
	public partial class ListingHelper : Window {

		public List<KeyValuePair<string, RewardCollection>> ScreensList { get; set; } = new List<KeyValuePair<string, RewardCollection>>();
		public List<List<string>> PrimeRewards { get; set; } = new List<List<string>>();
		//Helper, allowing to store the rewards until needed to be processed
		private int PageIndex { get; set; } = 0;
		private bool updating;
		public short SelectedRewardIndex = 0;
		private static readonly int SucsesHeight = 180; 
		private static readonly int FailedHeight = 270; 
		private static readonly int NormalHeight = 255; 

		#region default methods
		public ListingHelper() {
			InitializeComponent();
		}

		private void Minimize(object sender, RoutedEventArgs e) {
			WindowState = WindowState.Minimized;
		}

		private void Close(object sender, RoutedEventArgs e)
		{
			Hide();
			ScreensList = new List<KeyValuePair<string, RewardCollection>>();
			PageIndex = 0;
		}

		// Allows the draging of the window
		private void OnMouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
		#endregion

		#region frontend
		
		/// <summary>
		/// Sets the screen to one of the screen-lists indicated by it's index
		/// </summary>
		/// <param name="index">The index needed for the screen</param>
		public void SetScreen(int index)
		{
			Main.AddLog($"Screen list is {ScreensList.Count} long and setting to index: {index}");
			if (ScreensList.Count == 0)
				Close();
			if (ScreensList.Count < index || 0 > index)
			{
				throw new Exception("Tried setting screen to an item that didn't exist");
			}
			SetCurrentStatus();
			
			var screen = ScreensList[index];
			updating = true;
			SetListings(screen.Value.RewardIndex);
			ComboBox.Items.Clear();
			ComboBox.SelectedIndex = screen.Value.RewardIndex;
			foreach (var primeItem in screen.Value.PrimeNames.Where(primeItem => !primeItem.IsNullOrEmpty()))
			{
				ComboBox.Items.Add(primeItem);
			}

			updating = false;
		}
		/// <summary>
		/// changes screen over if there is a follow up screen
		/// </summary>
		public void NextScreen(object sender, RoutedEventArgs e) //todo throwing out of range error
		{
			Back.IsEnabled = true;
			if (PrimeRewards.Count > 0)
			{ // if there are new prime rewards
				Next.Content = "...";
				var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(PrimeRewards.First())).Result;
				if (rewardCollection.PrimeNames.Count != 0)
					Main.listingHelper.ScreensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
				PrimeRewards.RemoveAt(0);
				Next.Content = "Next";
			}
			if (ScreensList.Count - 1 == PageIndex) //reached the end of the list
			{
				Next.IsEnabled = false;
				return;
			}

			PageIndex++;
			SetScreen(PageIndex);
		}

		/// <summary>
		/// changes screen back if there is a previous screen
		/// </summary>
		public void PreviousScreen(object sender, RoutedEventArgs e) {
			
			Next.IsEnabled = true;

			Debug.WriteLine($"There are {ScreensList.Count} screens and: {PrimeRewards} prime rewards. Currently on screen {PageIndex} and trying to go to the previous screen");

			if (PageIndex == 0) {//reached start of the list
				Back.IsEnabled = false;
				return;
			}
			PageIndex--;
			SetScreen(PageIndex);

		}
		/// <summary>
		/// Updates the screen to reflect status
		/// </summary>
		private void SetCurrentStatus()
		{
			Debug.WriteLine($"Current status is: {ScreensList[PageIndex].Key}");
			switch (ScreensList[PageIndex].Key)
			{
				//listing already successfully posted
				case "successful":
					ListingGrid.Visibility = Visibility.Collapsed;
					Height = SucsesHeight;
					ConfirmListingButton.IsEnabled = false;
					Status.Content = "Listing already successfully posted";
					Status.Visibility = Visibility.Visible;
					ComboBox.IsEnabled = false;
					ConfirmListingButton.IsEnabled = false;
					break;
				case "": //listing is not yet assigned anything
					Height = NormalHeight;
					Status.Visibility = Visibility.Collapsed;
					ListingGrid.Visibility = Visibility.Visible;
					ComboBox.IsEnabled = true;
					ConfirmListingButton.IsEnabled = true;
					break;
				default: //an error occured.
					Height = FailedHeight;
					Status.Content = ScreensList[PageIndex].Key;
					Status.Visibility = Visibility.Visible;
					ListingGrid.Visibility = Visibility.Visible;
					ComboBox.IsEnabled = true;
					ConfirmListingButton.IsEnabled = true;
					break;
			}
		}

		/// <summary>
		/// List the current selected prime item with it's currently filled in plat value.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConfirmListing(object sender, RoutedEventArgs e) {
			Main.AddLog("Trying to place listing");
			try
			{
				var primeItem = (string) ComboBox.Items[ComboBox.SelectedIndex];
				var platinum = int.Parse(PlatinumTextBox.Text, Main.culture);
				var success = Task.Run(async () => await PlaceListing(primeItem, platinum)).Result;
				if (success) {
					var newEntry = new KeyValuePair<string, RewardCollection>("successful", ScreensList[PageIndex].Value);
					ScreensList.RemoveAt(PageIndex);
					ScreensList.Insert(PageIndex, newEntry);
					ConfirmListingButton.IsEnabled = true;
				} else {
					var newEntry = new KeyValuePair<string, RewardCollection>("Something uncaught went wrong", ScreensList[PageIndex].Value);
					ScreensList.RemoveAt(PageIndex);
					ScreensList.Insert(PageIndex, newEntry);
				}
				SetCurrentStatus();
			}
			catch (Exception exception) {
				Main.AddLog(exception.ToString());
				var newEntry = new KeyValuePair<string, RewardCollection>(exception.ToString(), ScreensList[PageIndex].Value);
				ScreensList.RemoveAt(PageIndex);
				ScreensList.Insert(PageIndex, newEntry);
			}

		}

		/// <summary>
		/// Changes the top 5 listings when the user selects a new item
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			if (!ComboBox.IsLoaded || updating) //Prevent firing off to early
				return;
			SetListings(ComboBox.SelectedIndex);
		}

		/// <summary>
		/// Sets the listing to the current selected prime item
		/// </summary>
		/// <param name="index">the currently selected prime item</param>
		private void SetListings(int index) {
			Debug.WriteLine($"There are {ScreensList[PageIndex].Value.PrimeNames.Count} of plat values, Setting index to: {index}");
			
			PlatinumTextBox.Text = ScreensList[PageIndex].Value.PlatinumValues[index].ToString(Main.culture);

			if (IsItemBanned(ScreensList[PageIndex].Value.PrimeNames[index])){
				ListingGrid.Visibility = Visibility.Collapsed;
				Height = 180;
				Status.Content = "Cannot list this item";
				Status.Visibility = Visibility.Visible;
				ComboBox.IsEnabled = true;
				ConfirmListingButton.IsEnabled = false;
				return;
			}

			ListingGrid.Visibility = Visibility.Visible;
			Height = 255;
			Status.Content = ScreensList[PageIndex].Key;
			Status.Visibility = Visibility.Collapsed;
			ComboBox.IsEnabled = true;
			ConfirmListingButton.IsEnabled = true;
		

			Platinum0.Content = ScreensList[PageIndex].Value.MarketListings[index][0].Platinum;
			Amount0.Content = ScreensList[PageIndex].Value.MarketListings[index][0].Amount;
			Reputation0.Content = ScreensList[PageIndex].Value.MarketListings[index][0].Reputation;

			Platinum1.Content = ScreensList[PageIndex].Value.MarketListings[index][1].Platinum;
			Amount1.Content = ScreensList[PageIndex].Value.MarketListings[index][1].Amount;
			Reputation1.Content = ScreensList[PageIndex].Value.MarketListings[index][1].Reputation;

			Platinum2.Content = ScreensList[PageIndex].Value.MarketListings[index][2].Platinum;
			Amount2.Content = ScreensList[PageIndex].Value.MarketListings[index][2].Amount;
			Reputation2.Content = ScreensList[PageIndex].Value.MarketListings[index][2].Reputation;

			Platinum3.Content = ScreensList[PageIndex].Value.MarketListings[index][3].Platinum;
			Amount3.Content = ScreensList[PageIndex].Value.MarketListings[index][3].Amount;
			Reputation3.Content = ScreensList[PageIndex].Value.MarketListings[index][3].Reputation;

			Platinum4.Content = ScreensList[PageIndex].Value.MarketListings[index][4].Platinum;
			Amount4.Content = ScreensList[PageIndex].Value.MarketListings[index][4].Amount;
			Reputation4.Content = ScreensList[PageIndex].Value.MarketListings[index][4].Reputation;
		}

		/// <summary>
		/// Cancels the current selection, removing it from the list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Cancel(object sender, RoutedEventArgs e) { //todo:fix the fact that canceling resets status of page from sold to default.
			if (ScreensList.Count == 1 || ScreensList.Count == 0)
			{
				// if it's the last item
				Minimize(null, null);
				ScreensList = new List<KeyValuePair<string, RewardCollection>>();
				PageIndex = 0;
				return;
			}

			if (PageIndex == 0) // if looking at the first screen
			{
				SetScreen(1);
				ScreensList.RemoveAt(0);
			} else {
				ScreensList.RemoveAt(PageIndex);
				--PageIndex;
				SetScreen(PageIndex);
			}
		}

		public void ShowLoading()
		{
			CancelButton.Content = "loading";
			Next.IsEnabled = false;
			Back.IsEnabled = false;
		}

		public void ShowFinished()
		{
			CancelButton.Content = "Cancel";
			Next.IsEnabled = true;
			Back.IsEnabled = true;
		}

		#endregion

		/// <summary>
		/// returns the data for an entire "Create listing" screen
		/// </summary>
		/// <param name="primeNames">The human friendly name to search listings for</param>
		/// <returns>the data for an entire "Create listing" screen</returns>
		public RewardCollection GetRewardCollection(List<string> primeNames)
		{
			var platinumValues = new List<short>(4);
			var marketListings = new List<List<MarketListing>>(5);
			var index = SelectedRewardIndex;
			SelectedRewardIndex = 0;
			if (primeNames == null)
			{
				throw new ArgumentNullException(nameof(primeNames));
			}

			foreach (var primeItem in primeNames)
			{
				try
				{
					var tempListings = GetMarketListing(primeItem);
					marketListings.Add(tempListings);
					platinumValues.Add(tempListings[0].Platinum);
				}
				catch (Exception e)
				{
					Main.RunOnUIThread(() =>
					{
						Main.searchBox.placeholder.Content = $"Could not find {primeItem}";
						Main.searchBox.searchField.Text = string.Empty;
					});
					Debug.WriteLine(e);
				}
				
			}
			return new RewardCollection(primeNames, platinumValues, marketListings, index);
		}

		private static bool IsItemBanned(string item)
		{
			return item.ToLower(Main.culture).Contains("kuva") ||
			       item.ToLower(Main.culture).Contains("exilus") ||
			       item.ToLower(Main.culture).Contains("riven") ||
			       item.ToLower(Main.culture).Contains("ayatan") ||
			       item.ToLower(Main.culture).Contains("forma");
		}

		/// <summary>
		/// Gets the top 5 current market listings
		/// </summary>
		/// <param name="primeName">The human friendly name to search listings for</param>
		/// <returns>the top 5 current market listings</returns>
		public static List<MarketListing> GetMarketListing(string primeName)
		{
			if (IsItemBanned(primeName))
			{
				var bannedListing = new List<MarketListing>();
				for (var i = 0; i < 5; i++)
				{
					bannedListing.Add(new MarketListing(0, 0, 0));
				}
				return bannedListing;
			}
			
			
			Debug.WriteLine($"Getting listing for {primeName}");
			var results = Task.Run(async () => await Main.dataBase.GetTopListings(primeName)).Result;
			var listings = new List<MarketListing>();
			var sellOrders = new JArray(results["payload"]["sell_orders"].Children());
			foreach (var item in sellOrders)
			{
				var platinum = item.Value<short>("platinum");
				var amount = item.Value<short>("quantity");
				var reputation = item["user"].Value<short>("reputation");
				var listing = new MarketListing(platinum, amount, reputation);
				Debug.WriteLine($"Getting listing for {listing.ToHumanString()}");
				listings.Add(listing);
			}
			return listings;
		}

		/// <summary>
		/// Tries to post the current screen to wfm
		/// </summary>
		/// <returns>if it succeeded</returns>
		private async Task<bool> PlaceListing(string primeItem, int platinum)
		{
			var listing = await Main.dataBase.GetCurrentListing(primeItem);
			if (listing == null) return await Main.dataBase.ListItem(primeItem, platinum, 1);
			//listing already exists, thus update it
			var listingId = (string)listing["id"];
			var quantity = (int)listing["quantity"];
			return await Main.dataBase.UpdateListing(listingId, platinum, quantity);
		}

        private void PlatinumTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
			PlatinumTextBox.Text = Regex.Replace(PlatinumTextBox.Text, "[^0-9.]", "");
		}
    }
	
    /// <summary>
    /// Class to represent a single "sheet" of the create listing screen, consisting of up to 4 possible rewards for which are unique plat, quantity and market listings 
    /// </summary>
    public class RewardCollection
	{
		public List<string> PrimeNames { get; set; } = new List<string>(4); // the reward items in case user wants to change selection
		public List<short> PlatinumValues { get; set; } = new List<short>(4);
		public List<List<MarketListing>> MarketListings { get; set; } = new List<List<MarketListing>>(5);
		public short RewardIndex{ get; set; } = 0;

		public RewardCollection(List<string> primeNames, List<short> platinumValues, List<List<MarketListing>> marketListings, short rewardIndex)
		{
			PrimeNames = primeNames;
			PlatinumValues = platinumValues;
			MarketListings = marketListings;
			RewardIndex = rewardIndex;
		}
		/// <summary>
		/// Gets a human friendly version back for logging.
		/// </summary>
		/// <returns></returns>
		public string ToHumanString()
		{
			var msg = "Reward collection screen:\n";
			foreach (var item in PrimeNames)
			{
				if (item.IsNullOrEmpty())
					continue;
				var index = PrimeNames.IndexOf(item);

				msg += $"Prime item: \"{item}\", Platinum value: \"{PlatinumValues[index]}\",  Market listings: \n";
				

				msg = MarketListings[index].Aggregate(msg, (current, listing) => current + (listing.ToHumanString() + "\n"));
			}
			return msg;
		}
	}
	/// <summary>
	/// Class to represent a single listing of an item, usually comes in groups of 5
	/// </summary>
	public class MarketListing
	{
		public short Platinum { get; set; } // plat amount of listing
		public short Amount { get; set; } //amount user lists
		public short Reputation { get; set; } // user's reputation

		public MarketListing(short platinum, short amount, short reputation)
		{
			Platinum = platinum;
			Amount = amount;
			Reputation = reputation;
		}

		/// <summary>
		/// Gets a human friendly version back for logging.
		/// </summary>
		/// <returns></returns>
		public string ToHumanString()
		{
			return "Platinum: " + Platinum + " Amount: " + Amount + " Reputation: " + Reputation;
		}
	}
}


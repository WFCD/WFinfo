﻿using System;
using System.Collections.Generic;
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
	public partial class CreateListing : Window {

		//public List<RewardCollection> screensList = new List<RewardCollection>();
		public List<KeyValuePair<string, RewardCollection>> screensList = new List<KeyValuePair<string, RewardCollection>>();
		//KVP is for sucess status, 0 = initial, 1 = successful 2+ = error
		public List<List<string>> primeRewards = new List<List<string>>();
		//Helper, allowing to store the rewards until needed to be processed
		public int pageIndex = 0;
		private bool updating;
		#region default methods
		public CreateListing() {
			InitializeComponent();
		}

		private void Minimize(object sender, RoutedEventArgs e) {
			WindowState = WindowState.Minimized;
		}

		private void Close(object sender, RoutedEventArgs e)
		{
			Hide();
			screensList = new List<KeyValuePair<string, RewardCollection>>();
			pageIndex = 0;
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
			Main.AddLog($"Screen list is {screensList.Count} long and setting to index: {index}");

			if (screensList.Count < index || 0 > index  || screensList.Count == 0)
			{
				throw new Exception("Tried setting screen to an item that didn't exist");
			}
			SetCurrentStatus();

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
		public void NextScreen(object sender, RoutedEventArgs e) //todo throwing out of range error
		{
			Back.IsEnabled = true;
			if (primeRewards.Count > 0)
			{ // if there are new prime rewards
				Next.Content = "...";
				var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(primeRewards.First())).Result;
				if (rewardCollection.primeNames.Count != 0)
					Main.listingHelper.screensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
				primeRewards.RemoveAt(0);
				Next.Content = "Next";
			}
			if (screensList.Count - 1 == pageIndex) //reached the end of the list
			{
				Next.IsEnabled = false;
				return;
			}

			pageIndex++;
			SetScreen(pageIndex);
		}

		/// <summary>
		/// changes screen back if there is a previous screen
		/// </summary>
		public void PreviousScreen(object sender, RoutedEventArgs e) {
			
			Next.IsEnabled = true;

			Console.WriteLine($"There are {screensList.Count} screens and: {primeRewards} prime rewards. Currently on screen {pageIndex} and trying to go to the previous screen");

			if (pageIndex == 0) {//reached start of the list
				Back.IsEnabled = false;
				return;
			}
			pageIndex--;
			SetScreen(pageIndex);

		}
		/// <summary>
		/// Updates the screen to reflect status
		/// </summary>
		private void SetCurrentStatus()
		{

			Console.WriteLine($"Current status is: {screensList[pageIndex].Key}");
			switch (screensList[pageIndex].Key)
			{
				//listing already successfully posted
				case "successful":
					ListingGrid.Visibility = Visibility.Collapsed;
					Height = 180;
					ConfirmListingButton.IsEnabled = false;
					Status.Content = "Listing already successfully posted";
					Status.Visibility = Visibility.Visible;
					ComboBox.IsEnabled = false;
					ConfirmListingButton.IsEnabled = true;
					break;
				case "": //listing is not yet assigned anything
					Height = 255;
					Status.Visibility = Visibility.Collapsed;
					ListingGrid.Visibility = Visibility.Visible;
					ComboBox.IsEnabled = true;
					break;
				default: //an error occured.
					Height = 270;
					Status.Content = screensList[pageIndex].Key;
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
				var platinum = int.Parse(PlatinumTextBox.Text);
				var success = Task.Run(async () => await PlaceListing(primeItem, platinum)).Result;
				if (success) {
					var newEntry = new KeyValuePair<string, RewardCollection>("successful", screensList[pageIndex].Value);
					screensList.RemoveAt(pageIndex);
					screensList.Insert(pageIndex, newEntry);
				} else {
					var newEntry = new KeyValuePair<string, RewardCollection>("Something uncaught went wrong", screensList[pageIndex].Value);
					screensList.RemoveAt(pageIndex);
					screensList.Insert(pageIndex, newEntry);
				}
				SetCurrentStatus();
			}
			catch (Exception exception) {
				Main.AddLog(exception.ToString());
				var newEntry = new KeyValuePair<string, RewardCollection>(exception.ToString(), screensList[pageIndex].Value);
				screensList.RemoveAt(pageIndex);
				screensList.Insert(pageIndex, newEntry);
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
			if (screensList.Count == 1)
			{
				// if it's the last item
				Minimize(null, null);
				screensList = new List<KeyValuePair<string, RewardCollection>>();
				pageIndex = 0;
				return;
			}

			if (pageIndex == 0) // if looking at the first screen
			{
				SetScreen(1);
				screensList.RemoveAt(0);
			} else {
				screensList.RemoveAt(pageIndex);
				--pageIndex;
				SetScreen(pageIndex);
			}
		}

		public void ShowLoading()
		{
			CancelButton.Content = "loading";
			Next.IsEnabled = false;
			Back.IsEnabled = false;
		}

		public void showFinished()
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
			var platinumValues = new List<int>(4);
			var marketListings = new List<List<MarketListing>>(5);

			foreach (var primeItem in primeNames)
			{
				if(primeItem.IsNullOrEmpty())
					continue;
				var tempListings = GetMarketListing(primeItem);
				marketListings.Add(tempListings);
				platinumValues.Add(tempListings[0].platinum);
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
			var results = Task.Run(async () => await Main.dataBase.GetTopListings(primeName)).Result;
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
		/// Tries to post the current screen to wfm
		/// </summary>
		/// <returns>if it succeeded</returns>
		private async Task<bool> PlaceListing(string primeItem, int platinum)
		{
			try
			{
				var listing = await Main.dataBase.GetCurrentListing(primeItem);
				if (listing == null) return await Main.dataBase.ListItem(primeItem, platinum, 1);
				//listing already exists, thus update it
				var listingId = (string)listing["id"];
				var quantity = (int)listing["quantity"];
				return await Main.dataBase.updateListing(listingId, platinum, quantity);
			}
			catch (Exception e)
			{
				throw e;
			}
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


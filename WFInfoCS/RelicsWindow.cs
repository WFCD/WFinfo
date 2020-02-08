using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WFInfoCS {
	/// <summary>
	/// Interaction logic for RelicsWindow.xaml
	/// </summary>
	public partial class RelicsWindow : System.Windows.Window {

		private bool showAllRelicsNext = true;
		public List<RelicsTreeNode> RelicNodes { get; set; }


		public RelicsWindow() {
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

		private void VaultedClick(object sender, RoutedEventArgs e) {

			if ((bool)vaulted.IsChecked) {
				foreach (RelicsTreeNode item in groupedByAll.Items) {
					if (item.Vaulted == "vaulted") {
						item.HideItem();
					}
				}
				foreach (RelicsTreeNode item in groupedByCollection.Items) {
					if (item.Vaulted == "vaulted") {
						item.HideItem();
					}
				}
				foreach (RelicsTreeNode item in Search.Items) {
					if (item.Vaulted == "vaulted") {
						item.HideItem();
					}
				}
			} else {
				foreach (RelicsTreeNode item in groupedByAll.Items) {
					if (item.Vaulted == "vaulted") {
						item.ShowItem();
					}
				}
				foreach (RelicsTreeNode item in groupedByCollection.Items) {
					if (item.Vaulted == "vaulted") {
						item.ShowItem();
					}
				}
				foreach (RelicsTreeNode item in Search.Items) {
					if (item.Vaulted == "vaulted") {
						item.HideItem();
					}
				}
			}
		}

		private void TextboxTextChanged(object sender, TextChangedEventArgs e) {
			if (textBox.IsLoaded) {
				Console.WriteLine(textBox.Text);
				Search.Visibility = Visibility.Visible;
				groupedByAll.Visibility = Visibility.Hidden;
				groupedByCollection.Visibility = Visibility.Hidden;
				foreach (RelicsTreeNode item in Search.Items) {
					item.HideItem();
					foreach (var child in item.Children) { 
						if (child.Name.Contains(textBox.Text)) { // if there was text found show item.
							item.ShowItem();
						}
					}
					if (item.Name.Contains(textBox.Text)) { // if there was text found show item.
						item.ShowItem();
					}
				}
			}
		}

		private void ComboboxMouseDown(object sender, MouseButtonEventArgs e) {
			Console.WriteLine(comboBox.Text); // compare this to the known results
		}

		private void TextBoxFocus(object sender, RoutedEventArgs e) {
			textBox.Clear();
		}

		private void ComboButton(object sender, RoutedEventArgs e) {
			if (showAllRelicsNext) {
				relicComboButton.Content = "All relics";
				showAllRelicsNext = false;
				groupedByCollection.Visibility = Visibility.Hidden;
				groupedByAll.Visibility = Visibility.Visible;
			} else {
				relicComboButton.Content = "Relic era";
				showAllRelicsNext = true;
				groupedByCollection.Visibility = Visibility.Visible;
				groupedByAll.Visibility = Visibility.Hidden;
			}
		}

		private void WindowLoaded(object sender, RoutedEventArgs e) { // triggers when the window is first loaded, populates all the listviews once.

			#region Populate grouped collection
			RelicNodes = new List<RelicsTreeNode>();

			RelicsTreeNode lith = new RelicsTreeNode("Lith", "");
			RelicsTreeNode meso = new RelicsTreeNode("Meso", "");
			RelicsTreeNode neo = new RelicsTreeNode("Neo", "");
			RelicsTreeNode axi = new RelicsTreeNode("Axi", "");
			RelicNodes.AddRange(new[] { lith, meso, neo, axi });
			foreach (RelicsTreeNode head in RelicNodes) {
				head.SetSilent();
				foreach (JProperty prop in Main.dataBase.relicData[head.Name]) {
					JObject primeItems = (JObject)Main.dataBase.relicData[head.Name][prop.Name];
					string vaulted = primeItems["vaulted"].ToObject<bool>() ? "vaulted" : "";
					RelicsTreeNode relic = new RelicsTreeNode(prop.Name, vaulted);
					foreach (KeyValuePair<string, JToken> kvp in primeItems) {
						if (kvp.Key != "vaulted" && Main.dataBase.marketData.TryGetValue(kvp.Value.ToString(), out JToken marketValues)) {
							RelicsTreeNode part = new RelicsTreeNode(kvp.Value.ToString(), "");
							part.SetPartText(marketValues["plat"].ToObject<double>(), marketValues["ducats"].ToObject<int>(), kvp.Key);
							relic.Children.Add(part);
						}
					}
					relic.SetRelicText();
					head.Children.Add(relic);
					groupedByAll.Items.Add(relic);
					Search.Items.Add(relic);
				}
				groupedByCollection.Items.Add(head);
			}
			
		}

		#endregion
	}
}

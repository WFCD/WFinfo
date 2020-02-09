using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for RelicsWindow.xaml
    /// </summary>
    public partial class RelicsWindow : System.Windows.Window
    {
        private bool searchActive = false;
        private bool showAllRelics = false;
        public static List<RelicTreeNode> RelicNodes { get; set; }
        public static string[] searchText;

        public RelicsWindow()
        {
            InitializeComponent();
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void RefreshVisibleRelics()
        {
            int index = 0;
            if (showAllRelics)
            {
                foreach (RelicTreeNode era in RelicNodes)
                {
                    foreach (RelicTreeNode relic in era.Children)
                    {
                        int curr = RelicTree.Items.IndexOf(relic);

                        if (curr == -1)
                            RelicTree.Items.Insert(index, relic);
                        else if(curr != index)
                            for (;curr > index; curr--)
                                RelicTree.Items.RemoveAt(index);

                        index++;
                    }
                }
                while (index < RelicTree.Items.Count)
                    RelicTree.Items.RemoveAt(index);
            } else
            {
                foreach (RelicTreeNode era in RelicNodes)
                {
                    int curr = RelicTree.Items.IndexOf(era);
                    if (era.Children.Count == 0)
                    {
                        if (curr != -1)
                            RelicTree.Items.RemoveAt(curr);
                    } else
                    {
                        if (curr == -1)
                            RelicTree.Items.Insert(index, era);

                        index++;
                    }
                }
            }

        }

        private void ReapplyFilters()
        {

            foreach (RelicTreeNode era in RelicNodes)
                era.ResetFilter();

            if ((bool)vaulted.IsChecked)
                foreach (RelicTreeNode era in RelicNodes)
                    era.FilterOutVaulted(true);

            if (searchText != null && searchText.Length != 0)
                foreach (RelicTreeNode era in RelicNodes)
                    era.FilterSearchText(false, true);

            RefreshVisibleRelics();
        }

        private void VaultedClick(object sender, RoutedEventArgs e)
        {
            if ((bool)vaulted.IsChecked)
            {
                foreach (RelicTreeNode era in RelicNodes)
                    era.FilterOutVaulted(true);

                RefreshVisibleRelics();
            } else
                ReapplyFilters();
        }

        private void TextboxTextChanged(object sender, TextChangedEventArgs e)
        {
            searchActive = textBox.Text.Length > 0 && textBox.Text != "Filter Terms";
            Console.WriteLine("TextboxTextChanged: " + textBox.Text);
            if (textBox.IsLoaded)
            {
                if (searchActive || (searchText != null && searchText.Length > 0))
                {
                    if (searchActive)
                        searchText = textBox.Text.Split(' ');
                    else
                        searchText = null;
                    ReapplyFilters();
                }
            }
        }

        private void SortBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            // 0 - Name
            // 1 - Average intact plat
            // 2 - Average radiant plat
            // 3 - Difference (radiant-intact)

            Console.WriteLine(SortBox.SelectedIndex);
        }

        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (!searchActive)
                textBox.Clear();
        }

        private void ToggleShowAllRelics(object sender, RoutedEventArgs e)
        {
            showAllRelics = !showAllRelics;
            RelicTree.Items.Clear();
            if (showAllRelics)
            {
                relicComboButton.Content = "All Relics";
                foreach (RelicTreeNode era in RelicNodes)
                {
                    foreach (RelicTreeNode relic in era.ChildrenList)
                        relic.topLevel = true;

                    foreach (RelicTreeNode relic in era.Children)
                        RelicTree.Items.Add(relic);
                }
            } else
            {
                relicComboButton.Content = "Relic Eras";
                foreach (RelicTreeNode era in RelicNodes)
                {
                    RelicTree.Items.Add(era);
                    foreach (RelicTreeNode relic in era.ChildrenList)
                        relic.topLevel = false;
                }
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        { // triggers when the window is first loaded, populates all the listviews once.

            #region Populate grouped collection
            RelicNodes = new List<RelicTreeNode>();

            RelicTreeNode lith = new RelicTreeNode("Lith", "");
            RelicTreeNode meso = new RelicTreeNode("Meso", "");
            RelicTreeNode neo = new RelicTreeNode("Neo", "");
            RelicTreeNode axi = new RelicTreeNode("Axi", "");
            RelicNodes.AddRange(new[] { lith, meso, neo, axi });
            foreach (RelicTreeNode head in RelicNodes)
            {
                head.SetSilent();
                foreach (JProperty prop in Main.dataBase.relicData[head.Name])
                {
                    JObject primeItems = (JObject)Main.dataBase.relicData[head.Name][prop.Name];
                    string vaulted = primeItems["vaulted"].ToObject<bool>() ? "vaulted" : "";
                    RelicTreeNode relic = new RelicTreeNode(prop.Name, vaulted);
                    relic.Era = head.Name;
                    foreach (KeyValuePair<string, JToken> kvp in primeItems)
                    {
                        if (kvp.Key != "vaulted" && Main.dataBase.marketData.TryGetValue(kvp.Value.ToString(), out JToken marketValues))
                        {
                            RelicTreeNode part = new RelicTreeNode(kvp.Value.ToString(), "");
                            part.SetPartText(marketValues["plat"].ToObject<double>(), marketValues["ducats"].ToObject<int>(), kvp.Key);
                            relic.AddChild(part);
                        }
                    }
                    relic.SetRelicText();
                    head.AddChild(relic);
                    //groupedByAll.Items.Add(relic);
                    //Search.Items.Add(relic);
                }
                head.ResetFilter();
                head.FilterOutVaulted();
                RelicTree.Items.Add(head);
            }
            #endregion
        }
    }
}

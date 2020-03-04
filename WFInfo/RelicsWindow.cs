using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for RelicsWindow.xaml
    /// </summary>
    public partial class RelicsWindow : System.Windows.Window
    {
        private bool searchActive = false;
        private bool showAllRelics = false;
        public static List<TreeNode> RelicNodes { get; set; }
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
                List<TreeNode> activeNodes = new List<TreeNode>();
                foreach (TreeNode era in RelicNodes)
                    foreach (TreeNode relic in era.ChildrenFiltered)
                        activeNodes.Add(relic);


                for (index = 0; index < RelicTree.Items.Count;)
                {
                    TreeNode relic = (TreeNode)RelicTree.Items.GetItemAt(index);
                    if (!activeNodes.Contains(relic))
                        RelicTree.Items.RemoveAt(index);
                    else
                    {
                        activeNodes.Remove(relic);
                        index++;
                    }
                }

                foreach (TreeNode relic in activeNodes)
                    RelicTree.Items.Add(relic);

                SortBoxChanged(null, null);
            }
            else
            {
                foreach (TreeNode era in RelicNodes)
                {
                    int curr = RelicTree.Items.IndexOf(era);
                    if (era.ChildrenFiltered.Count == 0)
                    {
                        if (curr != -1)
                            RelicTree.Items.RemoveAt(curr);
                    }
                    else
                    {
                        if (curr == -1)
                            RelicTree.Items.Insert(index, era);

                        index++;
                    }
                    era.RecolorChildren();
                }
            }
            RelicTree.Items.Refresh();
        }

        private void ReapplyFilters()
        {

            foreach (TreeNode era in RelicNodes)
                era.ResetFilter();

            if ((bool)vaulted.IsChecked)
                foreach (TreeNode era in RelicNodes)
                    era.FilterOutVaulted(true);

            if (searchText != null && searchText.Length != 0)
                foreach (TreeNode era in RelicNodes)
                    era.FilterSearchText(searchText, false, true);

            RefreshVisibleRelics();
        }

        private void VaultedClick(object sender, RoutedEventArgs e)
        {
            if ((bool)vaulted.IsChecked)
            {
                foreach (TreeNode era in RelicNodes)
                    era.FilterOutVaulted(true);

                RefreshVisibleRelics();
            }
            else
                ReapplyFilters();
        }

        private void TextboxTextChanged(object sender, TextChangedEventArgs e)
        {
            searchActive = textBox.Text.Length > 0 && textBox.Text != "Filter Terms";
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

            if (IsLoaded)
            {
                foreach (TreeNode era in RelicNodes)
                {
                    era.Sort(SortBox.SelectedIndex);
                    era.RecolorChildren();
                }
                if (showAllRelics)
                {
                    RelicTree.Items.SortDescriptions.Clear();
                    RelicTree.Items.IsLiveSorting = true;
                    switch (SortBox.SelectedIndex)
                    {
                        case 1:
                            RelicTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Intact_Val", System.ComponentModel.ListSortDirection.Descending));
                            break;
                        case 2:
                            RelicTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Radiant_Val", System.ComponentModel.ListSortDirection.Descending));
                            break;
                        case 3:
                            RelicTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Bonus_Val", System.ComponentModel.ListSortDirection.Descending));
                            break;
                        default:
                            RelicTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name_Sort", System.ComponentModel.ListSortDirection.Ascending));
                            break;
                    }
                    bool i = false;
                    foreach (TreeNode relic in RelicTree.Items)
                    {
                        i = !i;
                        if (i)
                            relic.Background_Color = TreeNode.BACK_D_BRUSH;
                        else
                            relic.Background_Color = TreeNode.BACK_U_BRUSH;
                    }
                }
            }
        }

        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (!searchActive)
                textBox.Clear();
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (!searchActive)
                textBox.Text = "Filter Terms";
        }

        private void ToggleShowAllRelics(object sender, RoutedEventArgs e)
        {
            showAllRelics = !showAllRelics;
            foreach (TreeNode era in RelicNodes)
                foreach (TreeNode relic in era.Children)
                    relic.topLevel = showAllRelics;

            if (showAllRelics)
                relicComboButton.Content = "All Relics";
            else
                relicComboButton.Content = "Relic Eras";


            RelicTree.Items.Clear();
            RefreshVisibleRelics();
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            foreach (TreeNode era in RelicNodes)
                era.ChangeExpandedTo(true);
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            foreach (TreeNode era in RelicNodes)
                era.ChangeExpandedTo(false);
        }

        private void SingleClickExpand(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;

            if (tvi == null || e.Handled) return;

            tvi.IsExpanded = !tvi.IsExpanded;
            tvi.IsSelected = false;
            e.Handled = true;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        { // triggers when the window is first loaded, populates all the listviews once.

            #region Populate grouped collection
            RelicNodes = new List<TreeNode>();

            TreeNode lith = new TreeNode("Lith", "");
            TreeNode meso = new TreeNode("Meso", "");
            TreeNode neo = new TreeNode("Neo", "");
            TreeNode axi = new TreeNode("Axi", "");
            RelicNodes.AddRange(new[] { lith, meso, neo, axi });
            int eraNum = 0;
            foreach (TreeNode head in RelicNodes)
            {
                head.SortNum = eraNum++;
                foreach (JProperty prop in Main.dataBase.relicData[head.Name])
                {
                    JObject primeItems = (JObject)Main.dataBase.relicData[head.Name][prop.Name];
                    string vaulted = primeItems["vaulted"].ToObject<bool>() ? "vaulted" : "";
                    TreeNode relic = new TreeNode(prop.Name, vaulted);
                    relic.Era = head.Name;
                    foreach (KeyValuePair<string, JToken> kvp in primeItems)
                    {
                        if (kvp.Key != "vaulted" && Main.dataBase.marketData.TryGetValue(kvp.Value.ToString(), out JToken marketValues))
                        {
                            TreeNode part = new TreeNode(kvp.Value.ToString(), "");
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
                head.RecolorChildren();
                RelicTree.Items.Add(head);
            }
            #endregion
        }
    }
}

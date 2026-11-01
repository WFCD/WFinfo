using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System;
using System.Windows.Threading;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for RelicsWindow.xaml
    /// </summary>
    public partial class EquipmentWindow : Window
    {
        private List<string> types = new List<string>() { "Warframes", "Primary", "Secondary", "Melee", "Archwing", "Companion" };
        private Dictionary<string, TreeNode> primeTypes;
        private bool searchActive = false;
        private bool showAllEqmt = false;
        private int searchTimerDurationMS = 500;
        public DispatcherTimer searchTimer;
        public static string[] searchText;
        public static EquipmentWindow INSTANCE;

        public EquipmentWindow()
        {
            InitializeComponent();
            INSTANCE = this;

            searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(searchTimerDurationMS)
            };
            searchTimer.Tick += (s, e) =>
            {
                searchTimer.Stop();
                ReapplyFilters();
            };
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void reloadItems()
        {
            if (primeTypes != null)
            {
                foreach (TreeNode category in primeTypes.Values)
                {
                    foreach (TreeNode prime in category.Children)
                    {
                        foreach (TreeNode part in prime.Children)
                        {
                            part.ReloadPartOwned(prime);
                        }
                        prime.GetSetInfo();
                    }
                }
                EqmtTree.Items.Refresh();
            }
        }

        public void populate()
        {

            primeTypes = new Dictionary<string, TreeNode>();
            foreach (KeyValuePair<string, JToken> prime in Main.dataBase.equipmentData)
            {
                if (prime.Key.Contains("Prime"))
                {
                    string primeName = prime.Key.Substring(0, prime.Key.IndexOf("Prime") + 5);
                    string primeType = prime.Value["type"].ToObject<string>();
                    bool mastered = prime.Value["mastered"].ToObject<bool>();
                    if (primeType.Contains("Sentinel") || primeType.Contains("Skin"))
                        primeType = "Companion";
                    else if (primeType.Contains("Arch")) //Future proofing for Arch-Guns and Arch-Melee
                        primeType = "Archwing";

                    if (!primeTypes.ContainsKey(primeType))
                    {
                        TreeNode newType = new TreeNode(primeType, "", false, 0);
                        if (!types.Contains(primeType))
                            types.Add(primeType);
                        newType.SortNum = types.IndexOf(primeType);
                        primeTypes[primeType] = newType;
                    }
                    TreeNode type = primeTypes[primeType];
                    TreeNode primeNode = new TreeNode(primeName, prime.Value["vaulted"].ToObject<bool>() ? "Vaulted" : "", mastered, 1);
                    primeNode.MakeClickable(prime.Key);
                    foreach (KeyValuePair<string, JToken> primePart in prime.Value["parts"].ToObject<JObject>())
                    {
                        string partName = primePart.Key;
                        if (primePart.Key.IndexOf("Prime") + 6 < primePart.Key.Length)
                            partName = partName.Substring(primePart.Key.IndexOf("Prime") + 6);

                        if (partName.Contains("Kubrow"))
                            partName = partName.Substring(partName.IndexOf(" Blueprint") + 1);
                        TreeNode partNode = new TreeNode(partName, primePart.Value["vaulted"].ToObject<bool>() ? "Vaulted" : "", false, 0);
                        partNode.MakeClickable(primePart.Key);
                        if (Main.dataBase.marketData.TryGetValue(primePart.Key.ToString(), out JToken marketValues))
                            partNode.SetPrimePart(marketValues["plat"].ToObject<double>(), marketValues["ducats"].ToObject<int>(), primePart.Value["owned"].ToObject<int>(), primePart.Value["count"].ToObject<int>());
                        else if (Main.dataBase.equipmentData.TryGetValue(primePart.Key, out JToken job))
                        {
                            double plat = 0.0;
                            double ducats = 0.0;
                            foreach (KeyValuePair<string, JToken> subPartPart in job["parts"].ToObject<JObject>())
                            {
                                if (Main.dataBase.marketData.TryGetValue(subPartPart.Key.ToString(), out JToken subMarketValues))
                                {
                                    int temp = subPartPart.Value["count"].ToObject<int>();
                                    plat += temp * subMarketValues["plat"].ToObject<double>();
                                    ducats += temp * subMarketValues["ducats"].ToObject<double>();
                                }
                            }
                            partNode.SetPrimeEqmt(plat, ducats, primePart.Value["owned"].ToObject<int>(), primePart.Value["count"].ToObject<int>());
                        }
                        else
                        {
                            Main.AddLog("COULDN'T FIND MARKET VALUES FOR: " + primePart.Key);
                            continue;
                        }

                        primeNode.AddChild(partNode);
                    }
                    if (primeNode.Children.Count() > 0)
                    {
                        primeNode.GetSetInfo();
                        type.AddChild(primeNode);
                    }
                    else
                    {
                        Main.AddLog("EQUIPMENT: Skipping " + primeName + " - no children added (all parts missing from marketData)");
                    }
                }
            }

            foreach (string typeName in types)
            {
                if (!primeTypes.ContainsKey(typeName))
                {
                    Main.AddLog("EQUIPMENT: No items found for type: " + typeName + ", skipping");
                    continue;
                }
                TreeNode primeType = primeTypes[typeName];
                primeType.ResetFilter();
                ApplyFilters(primeType);
                EqmtTree.Items.Add(primeType);
            }
            SortBoxChanged(null, null);
            RefreshVisibleRelics();
            Show();
            Focus();

        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        public void SortBoxChanged(object sender, SelectionChangedEventArgs e)
        {

            if (IsLoaded)
            {
                EqmtTree.Items.SortDescriptions.Clear();
                foreach (KeyValuePair<string, TreeNode> primeType in primeTypes)
                {
                    primeType.Value.Sort(SortBox.SelectedIndex, false);
                    primeType.Value.RecolorChildren();
                }
                if (showAllEqmt)
                {
                    EqmtTree.Items.IsLiveSorting = true;
                    var (propertyName, direction) = SortBox.SelectedIndex switch
                    {
                        1 => ("Plat_Val", System.ComponentModel.ListSortDirection.Descending),
                        2 => ("Diff_Val", System.ComponentModel.ListSortDirection.Ascending),
                        3 => ("Owned_Val", System.ComponentModel.ListSortDirection.Descending),
                        4 => ("Owned_Plat_Val", System.ComponentModel.ListSortDirection.Descending),
                        5 => ("Owned_Ducat_Val", System.ComponentModel.ListSortDirection.Descending),
                        _ => ("EqmtName_Sort", System.ComponentModel.ListSortDirection.Ascending)
                    };
                    EqmtTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(propertyName, direction));

                    bool i = false;
                    foreach (TreeNode prime in EqmtTree.Items)
                    {
                        i = !i;
                        if (i)
                            prime.Background_Color = TreeNode.BACK_D_BRUSH;
                        else
                            prime.Background_Color = TreeNode.BACK_U_BRUSH;
                    }
                }
                EqmtTree.Items.Refresh();
            }
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (sender == completed) incomplete.IsChecked = true;
            if (sender == incomplete) completed.IsChecked = true;

            if (sender == mastered) unmastered.IsChecked = true;
            if (sender == unmastered) mastered.IsChecked = true;

            StartSearchReapplyTimer();
        }

        /// <summary>
        /// Starts a timer to wait to apply changes to filters on search bar
        /// </summary>
        private void StartSearchReapplyTimer()
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void TextboxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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
                    StartSearchReapplyTimer();
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

        private void ToggleShowAllEqmt(object sender, RoutedEventArgs e)
        {
            showAllEqmt = !showAllEqmt;
            foreach (KeyValuePair<string, TreeNode> primeType in primeTypes)
                foreach (TreeNode kid in primeType.Value.Children)
                    kid.topLevel = showAllEqmt;

            if (showAllEqmt)
                eqmtComboButton.Content = "All Equipment";
            else
                eqmtComboButton.Content = "Equipment Types";

            EqmtTree.Items.Clear();
            RefreshVisibleRelics();
        }

        private void SingleClickExpand(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;

            if (tvi == null || e.Handled) return;

            tvi.IsExpanded = !tvi.IsExpanded;
            tvi.IsSelected = false;
            e.Handled = true;
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<string, TreeNode> primeType in primeTypes)
                primeType.Value.ChangeExpandedTo(true);
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<string, TreeNode> primeType in primeTypes)
                primeType.Value.ChangeExpandedTo(false);
        }

        private void CategoriesExpand(object sender, RoutedEventArgs e) =>
            CategoriesPopup.IsOpen = !CategoriesPopup.IsOpen;

        private void CategoriesCollapse(object sender, System.Windows.Input.MouseEventArgs e) =>
            CategoriesPopup.IsOpen = false;

        private void RefreshVisibleRelics()
        {
            int index = 0;
            if (showAllEqmt)
            {
                List<TreeNode> activeNodes = new List<TreeNode>();
                foreach (string typeName in types)
                {
                    if (!primeTypes.ContainsKey(typeName)) continue;
                    TreeNode primeType = primeTypes[typeName];
                    foreach (TreeNode eqmt in primeType.ChildrenFiltered)
                        activeNodes.Add(eqmt);
                }


                for (index = 0; index < EqmtTree.Items.Count;)
                {
                    TreeNode eqmt = (TreeNode)EqmtTree.Items.GetItemAt(index);
                    if (!activeNodes.Contains(eqmt))
                        EqmtTree.Items.RemoveAt(index);
                    else
                    {
                        activeNodes.Remove(eqmt);
                        index++;
                    }
                }

                foreach (TreeNode eqmt in activeNodes)
                    EqmtTree.Items.Add(eqmt);

                SortBoxChanged(null, null);
            }
            else
            {
                foreach (string typeName in types)
                {
                    if (!primeTypes.ContainsKey(typeName)) continue;
                    TreeNode primeType = primeTypes[typeName];
                    int curr = EqmtTree.Items.IndexOf(primeType);
                    if (primeType.ChildrenFiltered.Count == 0)
                    {
                        if (curr != -1)
                            EqmtTree.Items.RemoveAt(curr);
                    }
                    else
                    {
                        if (curr == -1)
                            EqmtTree.Items.Insert(index, primeType);

                        index++;
                    }
                    primeType.RecolorChildren();
                }
            }
            EqmtTree.Items.Refresh();
        }

        private void ApplyFilters(TreeNode type)
        {
            if (vaulted.IsChecked == false) type.FilterOutVaulted(true);
            if (unowned.IsChecked == false) type.FilterOutUnowned();
            if (completed.IsChecked == false) type.FilterOutCompleted();
            if (incomplete.IsChecked == false) type.FilterOutIncomplete();
            if (mastered.IsChecked == false) type.FilterOutMastered();
            if (unmastered.IsChecked == false) type.FilterOutUnmastered();

            if (warframes.IsChecked == false) type.FilterOutEquipment("Warframes");
            if (primary.IsChecked == false) type.FilterOutEquipment("Primary");
            if (secondary.IsChecked == false) type.FilterOutEquipment("Secondary");
            if (melee.IsChecked == false) type.FilterOutEquipment("Melee");
            if (archwing.IsChecked == false) type.FilterOutEquipment("Archwing");
            if (companion.IsChecked == false) type.FilterOutEquipment("Companion");
        }

        private void ReapplyFilters()
        {
            foreach (TreeNode primeType in primeTypes.Values)
            {
                primeType.ResetFilter();
                ApplyFilters(primeType);
            }

            if (searchText != null && searchText.Length != 0)
                foreach (TreeNode primeType in primeTypes.Values)
                    primeType.FilterSearchText(searchText, false, true);

            RefreshVisibleRelics();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            populate();
        }

    }
}
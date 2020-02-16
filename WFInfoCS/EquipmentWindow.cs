using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for RelicsWindow.xaml
    /// </summary>
    public partial class EquipmentWindow : System.Windows.Window
    {
        private List<string> types = new List<string>() { "Warframes", "Primary", "Secondary", "Melee", "Archwing", "Companion" };
        private Dictionary<string, RelicTreeNode> primeTypes;
        private bool searchActive = false;
        private bool showAllEqmt = false;
        public static string[] searchText;

        public EquipmentWindow()
        {
            InitializeComponent();
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void populate()
        { //todo implement populating the listview

            primeTypes = new Dictionary<string, RelicTreeNode>();
            foreach (KeyValuePair<string, JToken> prime in Main.dataBase.equipmentData)
            {
                if (prime.Key.Contains("Prime"))
                {
                    string primeName = prime.Key.Substring(0, prime.Key.IndexOf("Prime") + 5);
                    string primeType = prime.Value["type"].ToObject<string>();
                    if (primeType.Contains("Sentinel") || primeType.Contains("Skin"))
                        primeType = "Companion";
                    else if (primeType.Contains("Arch")) //Future proofing for Arch-Guns and Arch-Melee
                        primeType = "Archwing";

                    if (!primeTypes.ContainsKey(primeType))
                    {
                        RelicTreeNode newType = new RelicTreeNode(primeType, "");
                        if (!types.Contains(primeType))
                            types.Add(primeType);
                        newType.SortNum = types.IndexOf(primeType);
                        primeTypes[primeType] = newType;
                    }
                    RelicTreeNode type = primeTypes[primeType];
                    RelicTreeNode primeNode = new RelicTreeNode(primeName, prime.Value["vaulted"].ToObject<bool>() ? "Vaulted" : "");
                    foreach (KeyValuePair<string, JToken> primePart in prime.Value["parts"].ToObject<JObject>())
                    {
                        string partName = primePart.Key;
                        if (primePart.Key.IndexOf("Prime") + 6 < primePart.Key.Length)
                            partName = partName.Substring(primePart.Key.IndexOf("Prime") + 6);

                        if (partName.Contains("Kubrow"))
                            partName = partName.Substring(partName.IndexOf(" Blueprint") + 1);
                        RelicTreeNode partNode = new RelicTreeNode(partName, primePart.Value["vaulted"].ToObject<bool>() ? "Vaulted" : "");
                        if (Main.dataBase.marketData.TryGetValue(primePart.Key.ToString(), out JToken marketValues))
                            partNode.SetPrimePart(marketValues["plat"].ToObject<double>(), marketValues["ducats"].ToObject<int>(), primePart.Value["owned"].ToObject<int>(), primePart.Value["count"].ToObject<int>());
                        else if (Main.dataBase.equipmentData.TryGetValue(primePart.Key, out JToken job))
                        {
                            double plat = 0.0;
                            foreach (KeyValuePair<string, JToken> subPartPart in job["parts"].ToObject<JObject>())
                            {
                                if (Main.dataBase.marketData.TryGetValue(subPartPart.Key.ToString(), out JToken subMarketValues))
                                {
                                    int temp = subPartPart.Value["count"].ToObject<int>();
                                    plat += temp * subMarketValues["plat"].ToObject<double>();
                                }
                            }



                            partNode.SetPrimeEqmt(plat, primePart.Value["owned"].ToObject<int>(), primePart.Value["count"].ToObject<int>());
                        } else
                            Console.WriteLine(primePart.Key + " has no marketValues?");

                        primeNode.AddChild(partNode);
                    }
                    primeNode.GetSetInfo();
                    type.AddChild(primeNode);
                }
            }

            foreach (string typeName in types)
            {
                RelicTreeNode primeType = primeTypes[typeName];
                primeType.ResetFilter();
                primeType.FilterOutVaulted();
                EqmtTree.Items.Add(primeType);
            }
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

        private void SortBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            /*   < ComboBoxItem Content = "Name" />
                 < ComboBoxItem Content = "Cost" />
                 < ComboBoxItem Content = "Unowned" />
                 < ComboBoxItem Content = "Set cost" />*/

            if (IsLoaded)
            {
                EqmtTree.Items.SortDescriptions.Clear();
                foreach (KeyValuePair<string, RelicTreeNode> primeType in primeTypes)
                {
                    primeType.Value.Sort(SortBox.SelectedIndex, false);
                    primeType.Value.RecolorChildren();
                }
                if (showAllEqmt)
                {
                    EqmtTree.Items.IsLiveSorting = true;
                    switch (SortBox.SelectedIndex)
                    {
                        case 1:
                            EqmtTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Plat_Val", System.ComponentModel.ListSortDirection.Descending));
                            break;
                        case 2:
                            EqmtTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Diff_Val", System.ComponentModel.ListSortDirection.Descending));
                            break;
                        //case 3:
                        //    EqmtTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Bonus_Val", System.ComponentModel.ListSortDirection.Descending));
                        //    break;
                        default:
                            EqmtTree.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("EqmtName_Sort", System.ComponentModel.ListSortDirection.Ascending));
                            break;
                    }
                    bool i = false;
                    foreach (RelicTreeNode prime in EqmtTree.Items)
                    {
                        i = !i;
                        if (i)
                            prime.Background_Color = RelicTreeNode.BACK_D_BRUSH;
                        else
                            prime.Background_Color = RelicTreeNode.BACK_U_BRUSH;
                    }
                }
            }
        }

        private void VaultedClick(object sender, RoutedEventArgs e)
        {
            if ((bool)vaulted.IsChecked)
            {
                foreach (KeyValuePair<string, RelicTreeNode> primeType in primeTypes)
                    primeType.Value.FilterOutVaulted(true);

                RefreshVisibleRelics();
            } else
                ReapplyFilters();
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
                    ReapplyFilters();
                }
            }
        }

        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (!searchActive)
                textBox.Clear();
        }

        private void ToggleShowAllEqmt(object sender, RoutedEventArgs e)
        {
            showAllEqmt = !showAllEqmt;
            EqmtTree.Items.Clear();
            RefreshVisibleRelics();
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Add was clicked");

            //todo add 1 owned to current selected treeview item
        }

        private void Subtract(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Subtract was clicked");

            //todo remove 1 owned of the current selected tree view item
        }

        private void RefreshVisibleRelics()
        {
            int index = 0;
            if (showAllEqmt)
            {
                List<RelicTreeNode> activeNodes = new List<RelicTreeNode>();
                foreach (string typeName in types)
                {
                    RelicTreeNode primeType = primeTypes[typeName];
                    foreach (RelicTreeNode eqmt in primeType.ChildrenFiltered)
                        activeNodes.Add(eqmt);
                }


                for (index = 0; index < EqmtTree.Items.Count;)
                {
                    RelicTreeNode eqmt = (RelicTreeNode)EqmtTree.Items.GetItemAt(index);
                    if (!activeNodes.Contains(eqmt))
                        EqmtTree.Items.RemoveAt(index);
                    else
                    {
                        activeNodes.Remove(eqmt);
                        index++;
                    }
                }

                foreach (RelicTreeNode eqmt in activeNodes)
                    EqmtTree.Items.Add(eqmt);

                SortBoxChanged(null, null);
            } else
            {
                foreach (string typeName in types)
                {
                    RelicTreeNode primeType = primeTypes[typeName];
                    int curr = EqmtTree.Items.IndexOf(primeType);
                    if (primeType.ChildrenFiltered.Count == 0)
                    {
                        if (curr != -1)
                            EqmtTree.Items.RemoveAt(curr);
                    } else
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

        private void ReapplyFilters()
        {
            foreach (KeyValuePair<string, RelicTreeNode> primeType in primeTypes)
                primeType.Value.ResetFilter();

            if ((bool)vaulted.IsChecked)
                foreach (KeyValuePair<string, RelicTreeNode> primeType in primeTypes)
                    primeType.Value.FilterOutVaulted(true);

            if (searchText != null && searchText.Length != 0)
                foreach (KeyValuePair<string, RelicTreeNode> primeType in primeTypes)
                    primeType.Value.FilterSearchText(searchText, false, true);

            RefreshVisibleRelics();
        }
    }
}

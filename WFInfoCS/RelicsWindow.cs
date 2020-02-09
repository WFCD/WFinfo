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

        private bool showAllRelics = false;
        public static List<RelicTreeNode> RelicNodes { get; set; }


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

        private void VaultedClick(object sender, RoutedEventArgs e)
        {
            if ((bool)vaulted.IsChecked)
            {
                for (int i = 0; i < RelicTree.Items.Count; i++)
                {
                    RelicTreeNode relic = (RelicTreeNode)RelicTree.Items.GetItemAt(i);
                    if (!RelicTreeNode.FilterOutVaulted(relic))
                    {
                        RelicTree.Items.Remove(relic);
                        i--;
                    } else
                        relic.Filter(RelicTreeNode.FilterOutVaulted);
                }
            } else if (showAllRelics)
            {
                int prev = 0;
                foreach (RelicTreeNode era in RelicNodes)
                {
                    foreach (RelicTreeNode relic in era.ChildrenList)
                    {
                        if (RelicTree.Items.IndexOf(relic) == -1)
                            RelicTree.Items.Insert(prev, relic);

                        prev = RelicTree.Items.IndexOf(relic) + 1;
                    }
                }
            } else
            {
                foreach (RelicTreeNode era in RelicNodes)
                    era.ResetFilter();
            }

        }

        private void TextboxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBox.IsLoaded)
            {
                Console.WriteLine(textBox.Text);
                //Search.Visibility = Visibility.Visible;
                //groupedByAll.Visibility = Visibility.Hidden;
                //groupedByCollection.Visibility = Visibility.Hidden;
                /*foreach (RelicTreeNode item in Search.Items)
                {
                    item.HideItem();
                    foreach (var child in item.Children)
                    {
                        if (child.Name.Contains(textBox.Text))
                        { // if there was text found show item.
                            item.ShowItem();
                        }
                    }
                    if (item.Name.Contains(textBox.Text))
                    { // if there was text found show item.
                        item.ShowItem();
                    }
                }*/
            }
        }

        private void ComboboxMouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine(comboBox.Text); // compare this to the known results
        }

        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            textBox.Clear();
        }

        private void ComboButton(object sender, RoutedEventArgs e)
        {
            showAllRelics = !showAllRelics;
            if (showAllRelics)
            {
                relicComboButton.Content = "All Relics";
                RelicTree.Items.Clear();
                foreach (RelicTreeNode era in RelicNodes)
                    foreach (RelicTreeNode relic in era.ChildrenList)
                    {
                        relic.topLevel = true;
                        RelicTree.Items.Add(relic);
                    }
            } else
            {
                relicComboButton.Content = "Relic Eras";
                RelicTree.Items.Clear();
                foreach (RelicTreeNode era in RelicNodes)
                {
                    RelicTree.Items.Add(era);
                    foreach (RelicTreeNode relic in era.ChildrenList)
                        relic.topLevel = false;
                }
            }
        }

        public static void LoadNodesOnThread()
        {
            RelicNodes = new List<RelicTreeNode>();

            RelicNodes.Add(Main.CreateOnUIThread(() => { return new RelicTreeNode("Lith", ""); }));
            RelicNodes.Add(Main.CreateOnUIThread(() => { return new RelicTreeNode("Meso", ""); }));
            RelicNodes.Add(Main.CreateOnUIThread(() => { return new RelicTreeNode("Neo", ""); }));
            RelicNodes.Add(Main.CreateOnUIThread(() => { return new RelicTreeNode("Axi", ""); }));
            foreach (RelicTreeNode head in RelicNodes)
            {
                head.SetSilent();
                foreach (JProperty prop in Main.dataBase.relicData[head.Name])
                {
                    JObject primeItems = (JObject)Main.dataBase.relicData[head.Name][prop.Name];
                    string vaulted = primeItems["vaulted"].ToObject<bool>() ? "vaulted" : "";

                    RelicTreeNode relic = Main.CreateOnUIThread(() => { return new RelicTreeNode(prop.Name, vaulted); });
                    head.ChildrenList.Add(relic);
                    foreach (KeyValuePair<string, JToken> kvp in primeItems)
                    {
                        if (kvp.Key != "vaulted" && Main.dataBase.marketData.TryGetValue(kvp.Value.ToString(), out JToken marketValues))
                        {
                            RelicTreeNode part = Main.CreateOnUIThread(() => { return new RelicTreeNode(kvp.Value.ToString(), ""); });
                            part.SetPartText(marketValues["plat"].ToObject<double>(), marketValues["ducats"].ToObject<int>(), kvp.Key);
                            relic.ChildrenList.Add(part);
                        }
                    }
                    relic.SetRelicText();
                    head.ChildrenList.Add(relic);
                    //Main.RunOnUIThread(() => { Main.relicWindow.groupedByAll.Items.Add(relic); });
                    //Main.RunOnUIThread(() => { Main.relicWindow.Search.Items.Add(relic); });

                }
                head.ResetFilter();
                Main.RunOnUIThread(() => { Main.relicWindow.RelicTree.Items.Add(head); });
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
                            relic.ChildrenList.Add(part);
                        }
                    }
                    relic.SetRelicText();
                    head.ChildrenList.Add(relic);
                    //groupedByAll.Items.Add(relic);
                    //Search.Items.Add(relic);
                }
                head.ResetFilter();
                RelicTree.Items.Add(head);
            }
            #endregion
        }

    }
}

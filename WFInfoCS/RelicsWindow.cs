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
                foreach (RelicTreeNode era in RelicNodes)
                    era.Filter(RelicTreeNode.FilterOutVaulted);

                if (showAllRelics)
                {
                    for (int i = 0; i < RelicTree.Items.Count;)
                    {
                        RelicTreeNode relic = (RelicTreeNode)RelicTree.Items.GetItemAt(i);
                        if (!RelicTreeNode.FilterOutVaulted(relic))
                            RelicTree.Items.Remove(relic);
                        else
                            i++;
                    }
                }
            } else
            {
                foreach (RelicTreeNode era in RelicNodes)
                    era.ResetFilter();

                if (showAllRelics)
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
                }
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
                            relic.ChildrenList.Add(part);
                        }
                    }
                    relic.SetRelicText();
                    head.ChildrenList.Add(relic);
                    //groupedByAll.Items.Add(relic);
                    //Search.Items.Add(relic);
                }
                head.ResetFilter();
                head.Filter(RelicTreeNode.FilterOutVaulted);
                RelicTree.Items.Add(head);
            }
            #endregion
        }

    }
}

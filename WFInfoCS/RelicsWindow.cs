using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        public List<RelicsTreeNode> RelicNodes { get; set; }


        public RelicsWindow()
        {
            InitializeComponent();
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void populate()
        { //todo implement populating the listview
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
                Console.WriteLine("Vaulted is checked");
                //todo logic if it is checked
            }
        }

        private void TextboxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //todo update seach from what the user types
            Console.WriteLine(textBox.Text);

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
            Console.WriteLine("Combo button was clicked");
            //todo toggles between showing all relics in a single treeview vs showing them nested per age 
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

        private void WindowLoaded(object sender, RoutedEventArgs e)
        { // triggers when the window is first loaded, populates all the listviews once.

            #region Populate grouped collection

            RelicNodes = new List<RelicsTreeNode>();

            RelicsTreeNode lith = new RelicsTreeNode("Lith", "");
            RelicsTreeNode meso = new RelicsTreeNode("Meso", "");
            RelicsTreeNode neo = new RelicsTreeNode("Neo", "");
            RelicsTreeNode axi = new RelicsTreeNode("Axi", "");
            RelicNodes.AddRange(new[] { lith, meso, neo, axi });

            foreach (RelicsTreeNode head in RelicNodes)
            {
                head.SetSilent();
                foreach (JProperty prop in Main.dataBase.relicData[head.Name])
                {
                    JObject primeItems = (JObject)Main.dataBase.relicData[head.Name][prop.Name];
                    string vaulted = primeItems["vaulted"].ToObject<bool>() ? "vaulted" : "";
                    RelicsTreeNode relic = new RelicsTreeNode(prop.Name, vaulted);
                    foreach (KeyValuePair<string, JToken> kvp in primeItems)
                    {
                        if (kvp.Key != "vaulted" && Main.dataBase.marketData.TryGetValue(kvp.Value.ToString(), out JToken marketValues))
                        {
                            RelicsTreeNode part = new RelicsTreeNode(kvp.Value.ToString(), "");
                            part.SetPartText(marketValues["plat"].ToObject<double>(), marketValues["ducats"].ToObject<int>(), kvp.Key);
                            relic.Children.Add(part);
                        }
                    }
                    relic.SetRelicText();
                    head.Children.Add(relic);
                }
                groupedByCollection.Items.Add(head);
            }
        }




        #endregion
    }
}

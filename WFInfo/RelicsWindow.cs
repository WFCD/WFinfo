using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using WebSocketSharp;

namespace WFInfo
{
    public class RelicsViewModel : ObservableObject
    {
        private string _textBoxText = "";
        private bool _showAllRelics;

        public string TextBoxText
        {
            get => _textBoxText;
            set
            {
                SetProperty(ref _textBoxText, value); 
                OnPropertyChanged(nameof(IsTextboxEmpty));
            }
        }

        public bool IsTextboxEmpty => TextBoxText.IsNullOrEmpty();
        public List<TreeNode> RelicNodes { get; } = new List<TreeNode>();

        public bool ShowAllRelics
        {
            get
            {
                
                return _showAllRelics;
            }
            set
            {
                foreach (TreeNode era in RelicNodes)
                    foreach (TreeNode relic in era.Children)
                        relic.topLevel = value;
                SetProperty(ref _showAllRelics, value);
                OnPropertyChanged(nameof(ShowAllRelicsText));
            }
        }

        public string ShowAllRelicsText => ShowAllRelics ? "All Relics" : "Relic Eras";
    }
    /// <summary>
    /// Interaction logic for RelicsWindow.xaml
    /// </summary>
    public partial class RelicsWindow : Window
    {
        private bool showAllRelics = false;

        private string[] SearchText => SearchActive ? _relicsViewModel.TextBoxText.Split(' ') : null;

        private bool SearchActive => !_relicsViewModel.TextBoxText.IsNullOrEmpty();

        private RelicsViewModel _relicsViewModel = new RelicsViewModel();

        public RelicsWindow()
        {
            InitializeComponent();
            DataContext = this._relicsViewModel;
            this._relicsViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(RelicsViewModel.TextBoxText))
                {
                    ReapplyFilters();
                }
                else if (args.PropertyName == nameof(RelicsViewModel.ShowAllRelics))
                {
                    RelicTree.Items.Clear();
                    RefreshVisibleRelics();
                }
                
            };
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
                foreach (TreeNode era in _relicsViewModel.RelicNodes)
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
                foreach (TreeNode era in _relicsViewModel.RelicNodes)
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

            foreach (TreeNode era in _relicsViewModel.RelicNodes)
                era.ResetFilter();

            if ((bool)vaulted.IsChecked)
                foreach (TreeNode era in _relicsViewModel.RelicNodes)
                    era.FilterOutVaulted(true);

            if (SearchActive)
                foreach (TreeNode era in _relicsViewModel.RelicNodes)
                    era.FilterSearchText(SearchText, false, true);

            RefreshVisibleRelics();
        }

        private void VaultedClick(object sender, RoutedEventArgs e)
        {
            if ((bool)vaulted.IsChecked)
            {
                foreach (TreeNode era in _relicsViewModel.RelicNodes)
                    era.FilterOutVaulted(true);

                RefreshVisibleRelics();
            }
            else
                ReapplyFilters();
            
        }

        private void SortBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            // 0 - Name
            // 1 - Average intact plat
            // 2 - Average radiant plat
            // 3 - Difference (radiant-intact)

            if (IsLoaded)
            {
                foreach (TreeNode era in _relicsViewModel.RelicNodes)
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

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            foreach (TreeNode era in _relicsViewModel.RelicNodes)
                era.ChangeExpandedTo(true);
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            foreach (TreeNode era in _relicsViewModel.RelicNodes)
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

            TreeNode lith = new TreeNode("Lith", "", false, 0);
            TreeNode meso = new TreeNode("Meso", "", false, 0);
            TreeNode neo = new TreeNode("Neo", "", false, 0);
            TreeNode axi = new TreeNode("Axi", "", false, 0);
            _relicsViewModel.RelicNodes.AddRange(new[] { lith, meso, neo, axi });
            int eraNum = 0;
            foreach (TreeNode head in _relicsViewModel.RelicNodes)
            {
                double sumIntact = 0;
                double sumRad = 0;

                head.SortNum = eraNum++;
                foreach (JProperty prop in Main.dataBase.relicData[head.Name])
                {
                    JObject primeItems = (JObject)Main.dataBase.relicData[head.Name][prop.Name];
                    string vaulted = primeItems["vaulted"].ToObject<bool>() ? "vaulted" : "";
                    TreeNode relic = new TreeNode(prop.Name, vaulted, false, 0);
                    relic.Era = head.Name;
                    foreach (KeyValuePair<string, JToken> kvp in primeItems)
                    {
                        if (kvp.Key != "vaulted" && Main.dataBase.marketData.TryGetValue(kvp.Value.ToString(), out JToken marketValues))
                        {
                            TreeNode part = new TreeNode(kvp.Value.ToString(), "", false, 0);
                            part.SetPartText(marketValues["plat"].ToObject<double>(), marketValues["ducats"].ToObject<int>(), kvp.Key);                           
                            relic.AddChild(part);
                        }
                    }
                    
                    relic.SetRelicText();
                    head.AddChild(relic);

                    //groupedByAll.Items.Add(relic);
                    //Search.Items.Add(relic);
                }

                head.SetEraText();   
                head.ResetFilter();
                head.FilterOutVaulted();
                head.RecolorChildren();
                RelicTree.Items.Add(head);
            }
            SortBoxChanged(null, null);
            #endregion
        }
    }
}
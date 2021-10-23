using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WebSocketSharp;

namespace WFInfo
{
    public class RelicsViewModel : INPC
    {
        public RelicsViewModel()
        {
            _relicTreeItems = new List<TreeNode>();
            RelicsItemsView = new ListCollectionView(_relicTreeItems);
            ExpandAllCommand = new SimpleCommand(() => ExpandOrCollapseAll(true));
            CollapseAllCommand = new SimpleCommand(() => ExpandOrCollapseAll(false));
        }


        private string _filterText = "";
        private bool _showAllRelics;
        private readonly List<TreeNode> _relicTreeItems;
        private int _sortBoxSelectedIndex;
        private bool _hideVaulted = true;
        private readonly List<TreeNode> _rawRelicNodes = new List<TreeNode>();

        public string FilterText
        {
            get => _filterText;
            set
            {
                this.SetField(ref _filterText, value);
                ReapplyFilters();
                RaisePropertyChanged(nameof(IsFilterEmpty));
            }
        }
        public bool IsFilterEmpty => FilterText.IsNullOrEmpty();

        public SimpleCommand ExpandAllCommand { get; }
        public SimpleCommand CollapseAllCommand { get; }

        private void ExpandOrCollapseAll(bool expand)
        {
            foreach (TreeNode era in _rawRelicNodes)
                era.ChangeExpandedTo(expand);
        }
        
        public bool ShowAllRelics
        {
            get => _showAllRelics;
            set
            {
                foreach (TreeNode era in _rawRelicNodes)
                foreach (TreeNode relic in era.Children)
                    relic.topLevel = value;
                SetField(ref _showAllRelics, value);
                
                RefreshVisibleRelics();
                RaisePropertyChanged(nameof(ShowAllRelicsText));
            }
        }
        public string ShowAllRelicsText => ShowAllRelics ? "All Relics" : "Relic Eras";

        public bool HideVaulted
        {
            get => _hideVaulted;
            set
            { 
                SetField(ref _hideVaulted, value);
                ReapplyFilters();
            }
        }

        public ICollectionView RelicsItemsView { get; }

        public int SortBoxSelectedIndex
        {
            get => _sortBoxSelectedIndex;
            set
            {
                SetField(ref _sortBoxSelectedIndex, value);
                SortBoxChanged();
            }
        }
        public void SortBoxChanged()
        {
            // 0 - Name
            // 1 - Average intact plat
            // 2 - Average radiant plat
            // 3 - Difference (radiant-intact)
        
            foreach (TreeNode era in _rawRelicNodes)
            {
                era.Sort(SortBoxSelectedIndex);
                era.RecolorChildren();
            }
            if (ShowAllRelics)
            {
                RelicsItemsView.SortDescriptions.Clear();
                //TODO:
                //_relicTreeItems.IsLiveSorting = true;
                switch (SortBoxSelectedIndex)
                {
                    case 1:
                        RelicsItemsView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Intact_Val", System.ComponentModel.ListSortDirection.Descending));
                        break;
                    case 2:
                        RelicsItemsView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Radiant_Val", System.ComponentModel.ListSortDirection.Descending));
                        break;
                    case 3:
                        RelicsItemsView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Bonus_Val", System.ComponentModel.ListSortDirection.Descending));
                        break;
                    default:
                        RelicsItemsView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name_Sort", System.ComponentModel.ListSortDirection.Ascending));
                        break;
                }

                bool i = false;
                foreach (TreeNode relic in _relicTreeItems)
                {
                    i = !i;
                    if (i)
                        relic.Background_Color = TreeNode.BACK_D_BRUSH;
                    else
                        relic.Background_Color = TreeNode.BACK_U_BRUSH;
                }
            }
        }

        public void RefreshVisibleRelics()
        {
            int index = 0;
            if (ShowAllRelics)
            {
                List<TreeNode> activeNodes = new List<TreeNode>();
                foreach (TreeNode era in _rawRelicNodes)
                foreach (TreeNode relic in era.ChildrenFiltered)
                    activeNodes.Add(relic);


                for (index = 0; index < _relicTreeItems.Count;)
                {
                    TreeNode relic = (TreeNode)_relicTreeItems.ElementAt(index);
                    if (!activeNodes.Contains(relic))
                        _relicTreeItems.RemoveAt(index);
                    else
                    {
                        activeNodes.Remove(relic);
                        index++;
                    }
                }

                foreach (TreeNode relic in activeNodes)
                    _relicTreeItems.Add(relic);

                SortBoxChanged();
            }
            else
            {
                foreach (TreeNode era in _rawRelicNodes)
                {
                    int curr = _relicTreeItems.IndexOf(era);
                    if (era.ChildrenFiltered.Count == 0)
                    {
                        if (curr != -1)
                            _relicTreeItems.RemoveAt(curr);
                    }
                    else
                    {
                        if (curr == -1)
                            _relicTreeItems.Insert(index, era);

                        index++;
                    }
                    era.RecolorChildren();
                }
            }
            RelicsItemsView.Refresh();
        }

        public void ReapplyFilters()
        {
        
            foreach (TreeNode era in _rawRelicNodes)
            {
                era.ResetFilter();
                if(HideVaulted)
                    era.FilterOutVaulted(true);
                if(!FilterText.IsNullOrEmpty())
                {
                    var searchText = FilterText.Split(' ');
                    era.FilterSearchText(searchText, false, true);
                }
            }
            RefreshVisibleRelics();
        }

        public void InitializeTree()
        {
            TreeNode lith = new TreeNode("Lith", "", false, 0);
            TreeNode meso = new TreeNode("Meso", "", false, 0);
            TreeNode neo = new TreeNode("Neo", "", false, 0);
            TreeNode axi = new TreeNode("Axi", "", false, 0);
            _rawRelicNodes.AddRange(new[] { lith, meso, neo, axi });
            int eraNum = 0;
            foreach (TreeNode head in _rawRelicNodes)
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
            }
            RefreshVisibleRelics();
            SortBoxChanged();
        }
    }
    /// <summary>
    /// Interaction logic for RelicsWindow.xaml
    /// </summary>
    public partial class RelicsWindow : Window
    {
        private readonly RelicsViewModel _relicsViewModel = new RelicsViewModel();

        public RelicsWindow()
        {
            InitializeComponent();
            DataContext = this._relicsViewModel;
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
            _relicsViewModel.InitializeTree();
        }
    }
}
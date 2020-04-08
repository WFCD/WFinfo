using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WFInfo
{
    public class INPC : INotifyPropertyChanged
    {

        protected bool SetField<T>(ref T backingField, T value, [CallerMemberName] string propName = null)
        {
            bool valueChanged = false;

            // Can't use equality operator on generic types
            if (!EqualityComparer<T>.Default.Equals(backingField, value))
            {
                backingField = value;
                RaisePropertyChanged(propName);
                valueChanged = true;
            }

            return valueChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(string propName)
        {
            if (!string.IsNullOrWhiteSpace(propName) && (PropertyChanged != null))
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }

    public class SimpleCommand : ICommand
    {
        public SimpleCommand(Action action)
        {
            Action = action;
        }

        public Action Action { get; set; }

        public bool CanExecute(object parameter)
        {
            return (Action != null);
        }

        public event EventHandler CanExecuteChanged
        { // This will never be used, this is just to ignore the warning prompt in VS
            add { }    // doesn't matter
            remove { } // doesn't matter
        }

        public void Execute(object parameter)
        {
            Action?.Invoke();
        }
    }

    public class TreeNode : INPC
    {
        private const double INTACT_CHANCE_RARE = 0.02;
        private const double RADIANT_CHANCE_RARE = 0.1;
        private const double INTACT_CHANCE_UNCOMMON = 0.11;
        private const double RADIANT_CHANCE_UNCOMMON = 0.2;
        private const double INTACT_CHANCE_COMMON = 0.2533;
        private const double RADIANT_CHANCE_COMMON = 0.1667;

        private static ImageSource PLAT_SRC = (ImageSource)new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/plat.gif");
        private static ImageSource DUCAT_SRC = (ImageSource)new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/ducat_w.gif");
        private static Color RARE_COLOR = Color.FromRgb(255, 215, 0);
        private static Color UNCOMMON_COLOR = Color.FromRgb(192, 192, 192);
        private static Color COMMON_COLOR = Color.FromRgb(205, 127, 50);
        private static Brush RARE_BRUSH = new SolidColorBrush(RARE_COLOR);
        private static Brush UNCOMMON_BRUSH = new SolidColorBrush(UNCOMMON_COLOR);
        private static Brush COMMON_BRUSH = new SolidColorBrush(COMMON_COLOR);

        private static Color BACK_D_COLOR = Color.FromRgb(22, 22, 22);
        private static Color BACK_COLOR = Color.FromRgb(27, 27, 27);
        private static Color BACK_U_COLOR = Color.FromRgb(32, 32, 32);
        public static Brush BACK_D_BRUSH = new SolidColorBrush(BACK_D_COLOR);
        public static Brush BACK_BRUSH = new SolidColorBrush(BACK_COLOR);
        public static Brush BACK_U_BRUSH = new SolidColorBrush(BACK_U_COLOR);

        public TreeNode(string name, string vaulted)
        {
            Name = name;
            Vaulted = vaulted;

            ChildrenFiltered = new List<TreeNode>();
            Children = new List<TreeNode>();
            SetSilent();
        }

        public bool topLevel = false;

        private string _era;
        public string Era
        {
            get { return _era; }
            set { SetField(ref _era, value); }
        }

        private int _sortNum = -1;
        public int SortNum
        {
            get { return _sortNum; }
            set { SetField(ref _sortNum, value); }
        }

        private string _name;
        public string Name
        {
            get { return topLevel ? _era + " " + _name : _name; }
            set { SetField(ref _name, value); }
        }
        public string Name_Sort
        {
            get { return Parent != null ? Parent.SortNum + _era + " " + _name : SortNum + _name; }
            set { SetField(ref _name, value); }
        }
        public string EqmtName_Sort
        {
            get { return SortNum + _name; }
            set { SetField(ref _name, value); }
        }

        private Brush _colorBrush = new SolidColorBrush(Color.FromRgb(177, 208, 217));
        public Brush NameBrush
        {
            get { return _colorBrush; }
            set { SetField(ref _colorBrush, value); }
        }

        private Color _color = Color.FromRgb(177, 208, 217);
        public Color NameColor
        {
            get { return _color; }
            set { SetField(ref _color, value); }
        }

        private Brush _backcolor = BACK_BRUSH;
        public Brush Background_Color
        {
            get { return _backcolor; }
            set { SetField(ref _backcolor, value); }
        }

        private Thickness _col1margin = new Thickness(0, 0, 18, 0);
        public Thickness Col1_Margin1
        {
            get { return _col1margin; }
            set { SetField(ref _col1margin, value); }
        }

        private Thickness _col1margin2 = new Thickness(0, 0, 0, 0);
        public Thickness Col1_Margin2
        {
            get { return _col1margin2; }
            set { SetField(ref _col1margin2, value); }
        }

        private Thickness _col2margin = new Thickness(0, 0, 18, 0);
        public Thickness Col2_Margin1
        {
            get { return _col2margin; }
            set { SetField(ref _col2margin, value); }
        }

        private Thickness _col2margin2 = new Thickness(0, 0, 0, 0);
        public Thickness Col2_Margin2
        {
            get { return _col2margin2; }
            set { SetField(ref _col2margin2, value); }
        }

        private string _vaulted;
        public string Vaulted
        {
            get { return _vaulted; }
            set { SetField(ref _vaulted, value); }
        }

        public bool IsVaulted()
        {
            return Vaulted.Length == 0;
        }

        public void SetSilent()
        {
            Grid_Shown = "Visible";

            Col1_Text1 = "";
            Col1_Text2 = "";
            Col1_Img1 = null;
            Col1_Img1_Shown = "Hidden";

            Col2_Text1 = "";
            Col2_Text2 = "";
            Col2_Text3 = "";
            Col2_Img1 = null;
            Col2_Img1_Shown = "Hidden";
        }

        public void SetRelicText()
        {
            _intact = 0;
            _radiant = 0;

            foreach (TreeNode node in Children)
            {
                if (node.NameColor == RARE_COLOR)
                {
                    _intact += INTACT_CHANCE_RARE * node._plat;
                    _radiant += RADIANT_CHANCE_RARE * node._plat;
                }
                else if (node.NameColor == UNCOMMON_COLOR)
                {
                    _intact += INTACT_CHANCE_UNCOMMON * node._plat;
                    _radiant += RADIANT_CHANCE_UNCOMMON * node._plat;
                }
                else
                {
                    _intact += INTACT_CHANCE_COMMON * node._plat;
                    _radiant += RADIANT_CHANCE_COMMON * node._plat;
                }
            }

            _bonus = _radiant - _intact;
            Grid_Shown = "Visible";

            Col1_Text1 = "INT:";
            Col1_Text2 = _intact.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";

            Col2_Text1 = "RAD:";
            Col2_Text2 = _radiant.ToString("F1");
            int tempBonus = (int)(_bonus * 10);
            Col2_Text3 = "(";
            if (tempBonus >= 0)
                Col2_Text3 += "+";
            Col2_Text3 += (tempBonus / 10.0).ToString("F1") + ")";

            Col2_Img1 = PLAT_SRC;
            Col2_Img1_Shown = "Visible";
        }

        public void GetSetInfo()
        {
            Grid_Shown = "Visible";
            foreach (TreeNode kid in Children)
            {
                Plat_Val += kid.Plat_Val * kid.Count_Val;
                Owned_Val += kid.Owned_Val;
                Count_Val += kid.Count_Val;
            }
            Diff_Val = Owned_Val / Count_Val - 0.01 * Count_Val;

            Col1_Text1 = _owned + "/" + _count;
            Col1_Text2 = _plat.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";
        }

        internal void SetPrimeEqmt(double plat, int owned, int count)
        {
            Plat_Val = plat;
            Owned_Val = owned;
            Count_Val = count;
            Diff_Val = Owned_Val / Count_Val - 0.01 * Count_Val;

            Col1_Text1 = owned + "/" + count;
            Col1_Text2 = _plat.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";

            Col2_Text1 = "";
            Col2_Text2 = "";
            Col2_Text3 = "";
            Col2_Img1 = null;
            Col2_Img1_Shown = "Hidden";
        }

        internal void ChangeExpandedTo(bool expand)
        {
            IsExpanded = expand;
            foreach (TreeNode kid in Children)
                kid.ChangeExpandedTo(expand);
        }

        public void SetPrimePart(double plat, int ducat, int owned, int count)
        {
            SetPrimeEqmt(plat, owned, count);
            Col2_Text3 = ducat.ToString();
            Col2_Img1 = DUCAT_SRC;
            Col2_Img1_Shown = "Visible";
            Col2_Margin1 = new Thickness(0, 0, 28, 0);
            Col2_Margin2 = new Thickness(0, 0, 10, 0);
        }

        public void SetPartText(double plat, int ducat, string rarity)
        {
            if (rarity.Contains("rare"))
            {
                NameColor = RARE_COLOR;
                NameBrush = RARE_BRUSH;
            }
            else if (rarity.Contains("uncomm"))
            {
                NameColor = UNCOMMON_COLOR;
                NameBrush = UNCOMMON_BRUSH;
            }
            else if (rarity.Contains("comm"))
            {
                NameColor = COMMON_COLOR;
                NameBrush = COMMON_BRUSH;
            }

            if (Name != "Forma Blueprint")
            {
                _plat = plat;
                _ducat = ducat;

                Col1_Text1 = "";
                Col1_Text2 = _plat.ToString("F1");

                Col1_Img1 = PLAT_SRC;
                Col1_Img1_Shown = "Visible";
                Col1_Margin1 = new Thickness(0, 0, 38, 0);
                Col1_Margin2 = new Thickness(0, 0, 20, 0);

                Col2_Text1 = "";
                Col2_Text2 = "";
                Col2_Text3 = ducat.ToString();
                Col2_Img1 = DUCAT_SRC;
                Col2_Img1_Shown = "Visible";
                Col2_Margin1 = new Thickness(0, 0, 78, 0);
                Col2_Margin2 = new Thickness(0, 0, 60, 0);
            }
            else
            {
                Col1_Img1 = null;
                Col1_Text1 = "";
                Col1_Text2 = "";

                Col2_Img1 = null;
                Col2_Text1 = "";
                Col2_Text2 = "";
            }
        }

        public void ResetFilter()
        {
            foreach (TreeNode node in Children)
                node.ResetFilter();

            // This doesn't work, maybe i made mistake
            //Children.AsParallel().ForAll(node => node.ResetFilter());

            ForceVisibility = false;
            ChildrenFiltered = Children;
        }

        public void FilterOutVaulted(bool additionalFilter = false)
        {
            List<TreeNode> filterList = additionalFilter ? ChildrenFiltered : Children;
            ChildrenFiltered = filterList.AsParallel().Where(node => node.IsVaulted()).ToList();
        }

        public void RecolorChildren()
        {
            bool i = false;
            foreach (TreeNode child in ChildrenFiltered)
            {
                i = !i;
                if (i)
                    child.Background_Color = BACK_D_BRUSH;
                else
                    child.Background_Color = BACK_U_BRUSH;
            }
        }

        public string GetFullName()
        {
            string prnt = Name;
            TreeNode temp = Parent;
            while (temp != null)
            {
                prnt = temp.Name + "/" + prnt;
                temp = temp.Parent;
            }
            return prnt;
        }

        private void ConsolePrintBullshit(Dictionary<string, bool> matchedText)
        {
            string prnt = Name + ": ";
            TreeNode temp = Parent;
            while (temp != null)
            {
                prnt = temp.Name + "/" + prnt;
                temp = temp.Parent;
            }
            foreach (KeyValuePair<string, bool> kvp in matchedText)
            {
                prnt += kvp.Key + "(" + kvp.Value + ") ";
            }
            Console.WriteLine(prnt);
        }

        public bool FilterSearchText(string[] searchText, bool removeLeaves, bool additionalFilter = false, Dictionary<string, bool> matchedText = null)
        {
            Dictionary<string, bool> matchedTextCopy = new Dictionary<string, bool>();

            bool done = true;
            foreach (string text in searchText)
            {
                bool tempVal = (matchedText != null && matchedText[text]) || Name.ToLower().Contains(text.ToLower());
                matchedTextCopy[text] = tempVal;
                done = done && tempVal;
            }

            List<TreeNode> filterList = additionalFilter ? ChildrenFiltered : Children;
            if (done)
            {
                if (ChildrenFiltered.Count > 0)
                    ChildrenFiltered = filterList;
                else
                    ForceVisibility = true;

                return true;
            }

            List<TreeNode> temp = new List<TreeNode>();
            foreach (TreeNode node in filterList)
                if (node.FilterSearchText(searchText, removeLeaves, additionalFilter, matchedTextCopy))
                    temp.Add(node);

            if (temp.Count == Children.Count)
                foreach (TreeNode node in filterList)
                    node.ForceVisibility = false;

            ChildrenFiltered = (filterList.Count > 0 && filterList[0].ChildrenFiltered.Count > 0) || removeLeaves ? temp : filterList;
            return temp.Count > 0;
        }

        internal void Sort(int index, bool isRelics = true, int depth = 0)
        {
            foreach (TreeNode node in Children)
                node.Sort(index, isRelics, depth + 1);
            if (Children.Count > 0)
            {
                if (isRelics)
                {
                    if (depth == 0)   // Relics
                    {
                        switch (index)
                        {
                            // 0 - Name
                            // 1 - Average intact plat
                            // 2 - Average radiant plat
                            // 3 - Difference (radiant-intact)
                            case 1:
                                Children = Children.AsParallel().OrderByDescending(p => p._intact).ToList();
                                ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p._intact).ToList();
                                break;
                            case 2:
                                Children = Children.AsParallel().OrderByDescending(p => p._radiant).ToList();
                                ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p._radiant).ToList();
                                break;
                            case 3:
                                Children = Children.AsParallel().OrderByDescending(p => p._bonus).ToList();
                                ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p._bonus).ToList();
                                break;
                            default:
                                Children = Children.AsParallel().OrderBy(p => p.Name).ToList();
                                ChildrenFiltered = ChildrenFiltered.AsParallel().OrderBy(p => p.Name).ToList();
                                break;
                        }
                    }
                    else            // Parts
                    {
                        Children = Children.AsParallel().OrderByDescending(p => p.NameColor.G).ToList();
                        ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p.NameColor.G).ToList();
                    }
                }
                else
                {
                    switch (index)
                    {
                        // 0 - Name
                        // 1 - Plat
                        // 2 - Unowned
                        // 3 - N/A

                        case 1:
                            Children = Children.AsParallel().OrderByDescending(p => p.Plat_Val).ToList();
                            ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p.Plat_Val).ToList();
                            break;
                        case 2:
                            Children = Children.AsParallel().OrderBy(p => p.Owned_Val).OrderBy(p => p.Diff_Val).ToList();
                            ChildrenFiltered = ChildrenFiltered.AsParallel().OrderBy(p => p.Owned_Val).OrderBy(p => p.Diff_Val).ToList();
                            break;
                        //case 3:
                        //    Children = Children.AsParallel().OrderByDescending(p => p._bonus).ToList();
                        //    ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p._bonus).ToList();
                        //    break;
                        default:
                            Children = Children.AsParallel().OrderBy(p => p.Name).ToList();
                            ChildrenFiltered = ChildrenFiltered.AsParallel().OrderBy(p => p.Name).ToList();
                            break;
                    }
                }
            }
        }

        private string _col1_text1 = "INT:";
        public string Col1_Text1
        {
            get { return _col1_text1; }
            private set { SetField(ref _col1_text1, value); }
        }

        private string _col1_text2 = "4.4";
        public string Col1_Text2
        {
            get { return _col1_text2; }
            private set { SetField(ref _col1_text2, value); }
        }

        private ImageSource _col1_img1 = null;
        public ImageSource Col1_Img1
        {
            get { return _col1_img1; }
            private set { SetField(ref _col1_img1, value); }
        }

        private string _grid_shown = "Visible";
        public string Grid_Shown
        {
            get { return _grid_shown; }
            private set { SetField(ref _grid_shown, value); }
        }

        private string _col1_img1_shown = "Visible";
        public string Col1_Img1_Shown
        {
            get { return _col1_img1_shown; }
            private set { SetField(ref _col1_img1_shown, value); }
        }

        private string _col2_text1 = "RAD:";
        public string Col2_Text1
        {
            get { return _col2_text1; }
            private set { SetField(ref _col2_text1, value); }
        }

        private string _col2_text2 = "9.9";
        public string Col2_Text2
        {
            get { return _col2_text2; }
            private set { SetField(ref _col2_text2, value); }
        }

        private string _col2_text3 = "(+5.5)";
        public string Col2_Text3
        {
            get { return _col2_text3; }
            private set { SetField(ref _col2_text3, value); }
        }

        private ImageSource _col2_img1 = null;
        public ImageSource Col2_Img1
        {
            get { return _col2_img1; }
            private set { SetField(ref _col2_img1, value); }
        }

        private string _col2_img1_shown = "Visible";
        public string Col2_Img1_Shown
        {
            get { return _col2_img1_shown; }
            private set { SetField(ref _col2_img1_shown, value); }
        }

        public double _plat = 0;
        public double Plat_Val
        {
            get { return _plat; }
            set { SetField(ref _plat, value); }
        }

        public int _ducat = 0;
        public int Ducat_Val
        {
            get { return _ducat; }
            set { SetField(ref _ducat, value); }
        }

        public int _owned = 0;
        public int Owned_Val
        {
            get { return _owned; }
            set { SetField(ref _owned, value); }
        }

        public double _count = 0;
        public double Count_Val
        {
            get { return _count; }
            set { SetField(ref _count, value); }
        }
        public double _diff = 0;
        public double Diff_Val
        {
            get { return _diff; }
            set { SetField(ref _diff, value); }
        }

        public double _intact = 0;
        public double Intact_Val
        {
            get { return _intact; }
            set { SetField(ref _intact, value); }
        }

        public double _radiant = 0;
        public double Radiant_Val
        {
            get { return _radiant; }
            set { SetField(ref _radiant, value); }
        }

        public double _bonus = 0;
        public double Bonus_Val
        {
            get { return _bonus; }
            set { SetField(ref _bonus, value); }
        }

        public Visibility IsVisible
        {
            get { return (_forceVisibility || Parent == null || Parent.IsExpanded || topLevel) ? Visibility.Visible : Visibility.Collapsed; }
        }

        private bool _forceVisibility = false;
        public bool ForceVisibility
        {
            get { return _forceVisibility; }
            set
            {
                SetField(ref _forceVisibility, value);
                RaisePropertyChanged("IsVisible");
            }
        }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                SetField(ref _isExpanded, value);
                foreach (TreeNode kid in Children)
                    kid.RaisePropertyChanged("IsVisible");
            }
        }

        private List<TreeNode> _childrenFiltered;
        public List<TreeNode> ChildrenFiltered
        {
            get { return _childrenFiltered; }
            private set { SetField(ref _childrenFiltered, value); }
        }

        private List<TreeNode> _children;
        public List<TreeNode> Children
        {
            get { return _children; }
            private set { SetField(ref _children, value); }
        }

        public TreeNode Parent;
        public void AddChild(TreeNode kid)
        {
            kid.Parent = this;
            Children.Add(kid);
        }

        public override string ToString()
        {
            return Era + " " + Name;
        }

        private ICommand _decrement;
        public ICommand DecrementPart
        {
            get { return _decrement; }
            private set { SetField(ref _decrement, value); }
        }

        private ICommand _increment;
        public ICommand IncrementPart
        {
            get { return _increment; }
            private set { SetField(ref _increment, value); }
        }

        private string dataRef;

        public void MakeClickable(string eqmtRef)
        {
            dataRef = eqmtRef;
            DecrementPart = new SimpleCommand(DecrementPartFunc);
            IncrementPart = new SimpleCommand(IncrementPartFunc);
        }

        public async void DecrementPartFunc()
        {
            if (Parent.dataRef != null)
            {
                await System.Threading.Tasks.Task.Run(() => DecrementPartThreaded(Parent));


            }
        }

        public async void IncrementPartFunc()
        {
            if (Parent.dataRef != null)
            {
                await System.Threading.Tasks.Task.Run(() => IncrementPartThreaded(Parent));
            }
        }

        private void DecrementPartThreaded(TreeNode Parent)
        {
            JObject job = Main.dataBase.equipmentData[Parent.dataRef]["parts"][dataRef] as JObject;
            int owned = job["owned"].ToObject<int>();
            if (owned > 0)
            {
                job["owned"] = owned - 1;
                Main.dataBase.SaveAllJSONs();
                Owned_Val--;
                Parent.Owned_Val--;
                Diff_Val = Owned_Val / Count_Val - 0.01 * Count_Val;
                Parent.Diff_Val = Parent.Owned_Val / Parent.Count_Val - 0.01 * Parent.Count_Val;
                Col1_Text1 = Owned_Val + "/" + Count_Val;
                Parent.Col1_Text1 = Parent.Owned_Val + "/" + Parent.Count_Val;
                Main.RunOnUIThread(() =>
                {
                    EquipmentWindow.INSTANCE.SortBoxChanged(null, null);
                });

            }
        }

        private void IncrementPartThreaded(TreeNode Parent)
        {
            JObject job = Main.dataBase.equipmentData[Parent.dataRef]["parts"][dataRef] as JObject;
            int count = job["count"].ToObject<int>();
            int owned = job["owned"].ToObject<int>();
            if (owned < count)
            {
                job["owned"] = owned + 1;
                Main.dataBase.SaveAllJSONs();
                Owned_Val++;
                Diff_Val = Owned_Val / Count_Val - 0.01 * Count_Val;
                Col1_Text1 = Owned_Val + "/" + Count_Val;
                Parent.Owned_Val++;
                Parent.Diff_Val = Parent.Owned_Val / Parent.Count_Val - 0.01 * Parent.Count_Val;
                Parent.Col1_Text1 = Parent.Owned_Val + "/" + Parent.Count_Val;
                Main.RunOnUIThread(() =>
                {
                    EquipmentWindow.INSTANCE.SortBoxChanged(null, null);
                });
            }
        }
    }
}

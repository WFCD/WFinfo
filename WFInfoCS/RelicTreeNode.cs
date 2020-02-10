using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WFInfoCS
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

    public class RelicTreeNode : INPC
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

        public RelicTreeNode(string name, string vaulted)
        {
            Name = name;
            Vaulted = vaulted;

            ChildrenFiltered = new List<RelicTreeNode>();
            Children = new List<RelicTreeNode>();
        }

        public bool topLevel = false;

        private string _era;
        public string Era
        {
            get { return _era; }
            set { SetField(ref _era, value); }
        }

        private int _eraNum = -1;
        public int EraNum
        {
            get { return _eraNum; }
            set { SetField(ref _eraNum, value); }
        }

        private string _name;
        public string Name
        {
            get { return topLevel ? _era + " " + _name : _name; }
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
            Col2_Img1 = null;
            Col2_Img1_Shown = "Hidden";
        }

        public void SetRelicText()
        {
            _intact = 0;
            _radiant = 0;

            foreach (RelicTreeNode node in Children)
            {
                if (node.NameColor == RARE_COLOR)
                {
                    _intact += INTACT_CHANCE_RARE * node._plat;
                    _radiant += RADIANT_CHANCE_RARE * node._plat;
                } else if (node.NameColor == UNCOMMON_COLOR)
                {
                    _intact += INTACT_CHANCE_UNCOMMON * node._plat;
                    _radiant += RADIANT_CHANCE_UNCOMMON * node._plat;
                } else
                {
                    _intact += INTACT_CHANCE_COMMON * node._plat;
                    _radiant += RADIANT_CHANCE_COMMON * node._plat;
                }
            }

            _bonus = _radiant - _intact;
            Grid_Shown = "Visible";

            Col1_Text1 = "INT ";
            Col1_Text2 = " " + _intact.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";

            Col2_Text1 = "RAD ";
            Col2_Text2 = " " + _radiant.ToString("F1") + " (";
            if (_bonus >= 0)
                Col2_Text2 += "+";
            Col2_Text2 += _bonus.ToString("F1") + ")";

            Col2_Img1 = PLAT_SRC;
            Col2_Img1_Shown = "Visible";
        }

        public void SetPartText(double plat, int ducat, string rarity)
        {
            if (rarity.Contains("rare"))
            {
                NameColor = RARE_COLOR;
                NameBrush = RARE_BRUSH;
            } else if (rarity.Contains("uncomm"))
            {
                NameColor = UNCOMMON_COLOR;
                NameBrush = UNCOMMON_BRUSH;
            } else
            {
                NameColor = COMMON_COLOR;
                NameBrush = COMMON_BRUSH;
            }

            if (Name != "Forma Blueprint")
            {
                _plat = plat;
                _ducat = ducat;

                Col1_Text1 = "       ";
                if (plat < 100)
                    Col1_Text2 = " " + plat.ToString("F1");
                else
                    Col1_Text2 = " " + plat.ToString("F0");

                Col1_Img1 = PLAT_SRC;
                Col1_Img1_Shown = "Visible";

                Col2_Text1 = "         ";
                Col2_Text2 = " " + ducat.ToString();
                Col2_Img1 = DUCAT_SRC;
                Col2_Img1_Shown = "Visible";
            } else
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
            foreach (RelicTreeNode node in Children)
                node.ResetFilter();

            // This doesn't work, maybe i made mistake
            //Children.AsParallel().ForAll(node => node.ResetFilter());

            ChildrenFiltered = Children;
        }

        public void FilterOutVaulted(bool additionalFilter = false)
        {
            List<RelicTreeNode> filterList = additionalFilter ? ChildrenFiltered : Children;
            ChildrenFiltered = filterList.AsParallel().Where(node => node.IsVaulted()).ToList();
        }

        public string GetFullName()
        {
            string prnt = Name;
            RelicTreeNode temp = Parent;
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
            RelicTreeNode temp = Parent;
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

        public bool FilterSearchText(bool removeLeaves, bool additionalFilter = false, Dictionary<string, bool> matchedText = null)
        {
            Dictionary<string, bool> matchedTextCopy = new Dictionary<string, bool>();

            bool done = true;
            foreach (string text in RelicsWindow.searchText)
            {
                bool tempVal = (matchedText != null && matchedText[text]) || Name.ToLower().Contains(text.ToLower());
                matchedTextCopy[text] = tempVal;
                done = done && tempVal;
            }

            List<RelicTreeNode> filterList = additionalFilter ? ChildrenFiltered : Children;
            if (done)
            {
                ChildrenFiltered = filterList;
                return true;
            }

            List<RelicTreeNode> temp = new List<RelicTreeNode>();
            foreach (RelicTreeNode node in filterList)
            {
                if (node.FilterSearchText(removeLeaves, additionalFilter, matchedTextCopy))
                {
                    temp.Add(node);
                }
            }

            ChildrenFiltered = (filterList.Count > 0 && filterList[0].ChildrenFiltered.Count > 0) || removeLeaves ? temp : filterList;
            return temp.Count > 0;
        }

        internal void Sort(int index, int depth = 0)
        {
            foreach (RelicTreeNode node in Children)
                node.Sort(index, depth + 1);
            if (Children.Count > 0)
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
                            ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p.Name).ToList();
                            break;
                    }
                } else            // Parts
                {
                    Children = Children.AsParallel().OrderByDescending(p => p.NameColor.G).ToList();
                    ChildrenFiltered = ChildrenFiltered.AsParallel().OrderByDescending(p => p.NameColor.G).ToList();
                }
            }
        }

        private string _col1_text1 = "INT";
        public string Col1_Text1
        {
            get { return _col1_text1; }
            private set { SetField(ref _col1_text1, value); }
        }

        private string _col1_text2 = ": 4.4";
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

        private string _col2_text1 = "RAD";
        public string Col2_Text1
        {
            get { return _col2_text1; }
            private set { SetField(ref _col2_text1, value); }
        }

        private string _col2_text2 = ": 9.9 (+5.5)";
        public string Col2_Text2
        {
            get { return _col2_text2; }
            private set { SetField(ref _col2_text2, value); }
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
        public int _ducat = 0;
        public double _intact = 0;
        public double _radiant = 0;
        public double _bonus = 0;

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetField(ref _isExpanded, value); }
        }

        private List<RelicTreeNode> _childrenFiltered;
        public List<RelicTreeNode> ChildrenFiltered
        {
            get { return _childrenFiltered; }
            private set { SetField(ref _childrenFiltered, value); }
        }

        private List<RelicTreeNode> _children;
        public List<RelicTreeNode> Children
        {
            get { return _children; }
            private set { SetField(ref _children, value); }
        }

        public RelicTreeNode Parent;
        public void AddChild(RelicTreeNode kid)
        {
            kid.Parent = this;
            Children.Add(kid);
        }

        public override string ToString()
        {
            return Era + " " + Name;
        }

    }
}

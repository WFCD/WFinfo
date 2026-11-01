using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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

        protected void RaisePropertyChanged([CallerMemberName] string propName = null)
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

        public TreeNode(string name, string vaulted, bool mastered, byte showAll)
        {
            Name = name;
            Vaulted = vaulted;
            Mastered = mastered;
            ShowAll = showAll;
            ChildrenFiltered = new List<TreeNode>();
            Children = new List<TreeNode>();
            SetSilent();
        }

        public object ShowAll { get; set; }

        public bool topLevel = false;

        public string Era { get; set { SetField(ref field, value); } }

        public int SortNum { get; set { SetField(ref field, value); } } = -1;

        public string Name
        {
            get { return topLevel ? Era + " " + field : field; }
            set { SetField(ref field, value); }
        }
        public string Name_Sort
        {
            get { return current != null ? current.SortNum + Era + " " + Name : SortNum + Name; }
            set { Name = value; }
        }
        public string EqmtName_Sort
        {
            get { return SortNum + Name; }
            set { Name = value; }
        }

        public Brush NameBrush { get; set { SetField(ref field, value); } } = new SolidColorBrush(Color.FromRgb(177, 208, 217));

        public Color NameColor { get; set { SetField(ref field, value); } } = Color.FromRgb(177, 208, 217);

        public Brush Background_Color { get; set { SetField(ref field, value); } } = BACK_BRUSH;

        public Thickness Col1_Margin1 { get; set { SetField(ref field, value); } } = new Thickness(0, 0, 18, 0);

        public Thickness Col1_Margin2 { get; set { SetField(ref field, value); } } = new Thickness(0, 0, 0, 0);

        public Thickness Col2_Margin1 { get; set { SetField(ref field, value); } } = new Thickness(0, 0, 18, 0);

        public Thickness Col2_Margin2 { get; set { SetField(ref field, value); } } = new Thickness(0, 0, 0, 0);

        public string Vaulted { get; set { SetField(ref field, value); } }

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

        public void SetEraText()
        {
            Intact_Val = 0;
            Radiant_Val = 0;

            foreach (TreeNode node in Children)
            {

                if (node.IsVaulted()) // IsVaulted is true if its not vaulted
                {
                    Intact_Val += node.Intact_Val;
                    Radiant_Val += node.Radiant_Val;

                }
            }

            Bonus_Val = Radiant_Val - Intact_Val;

            Col1_Text1 = "INT:";
            Col1_Text2 = Intact_Val.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";

            Col2_Text1 = "RAD:";
            Col2_Text2 = Radiant_Val.ToString("F1");
            int tempBonus = (int)(Bonus_Val * 10);
            Col2_Text3 = "(";
            if (tempBonus >= 0)
                Col2_Text3 += "+";
            Col2_Text3 += (tempBonus / 10.0).ToString("F1") + ")";

            Col2_Img1 = PLAT_SRC;
            Col2_Img1_Shown = "Visible";

        }
        public void SetRelicText()
        {
            Intact_Val = 0;
            Radiant_Val = 0;

            foreach (TreeNode node in Children)
            {
                var (intactChance, radiantChance) = node.NameColor switch
                {
                    var c when c == RARE_COLOR => (INTACT_CHANCE_RARE, RADIANT_CHANCE_RARE),
                    var c when c == UNCOMMON_COLOR => (INTACT_CHANCE_UNCOMMON, RADIANT_CHANCE_UNCOMMON),
                    _ => (INTACT_CHANCE_COMMON, RADIANT_CHANCE_COMMON)
                };

                Intact_Val += intactChance * node.Plat_Val;
                Radiant_Val += radiantChance * node.Plat_Val;
            }

            Bonus_Val = Radiant_Val - Intact_Val;
            Grid_Shown = "Visible";

            Col1_Text1 = "INT:";
            Col1_Text2 = Intact_Val.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";

            Col2_Text1 = "RAD:";
            Col2_Text2 = Radiant_Val.ToString("F1");
            int tempBonus = (int)(Bonus_Val * 10);
            Col2_Text3 = "(";
            if (tempBonus >= 0)
                Col2_Text3 += "+";
            Col2_Text3 += (tempBonus / 10.0).ToString("F1") + ")";

            Col2_Img1 = PLAT_SRC;
            Col2_Img1_Shown = "Visible";
        }

        public bool GetSetInfo()
        {
            string primeSetName = Data.GetSetName(Name);
            if (!Main.dataBase.marketData.TryGetValue(primeSetName, out JToken primeSetJToken))
            {
                return false; // This is not a set
            }
            JObject primeSet = (JObject)primeSetJToken;

            string primeSetPlat = primeSet["plat"].ToObject<string>();

            Grid_Shown = "Visible";
            Plat_Val = double.Parse(primeSetPlat, Main.culture);
            Owned_Capped_Val = 0;
            Owned_Plat_Val = 0;
            Owned_Ducat_Val = 0;
            Owned_Val = 0;
            Count_Val = 0;
            Mastered = Main.dataBase.equipmentData[this.dataRef]["mastered"].ToObject<bool>();
            foreach (TreeNode kid in Children)
            {
                Owned_Capped_Val += kid.Owned_Capped_Val;
                Owned_Plat_Val += kid.Owned_Plat_Val;
                Owned_Ducat_Val += kid.Owned_Ducat_Val;
                Owned_Val += kid.Owned_Val;
                Count_Val += kid.Count_Val;
            }

            PrimeUpdateDiff(true);
            Col1_Text2 = Plat_Val.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";
            return true;
        }

        internal void SetPrimeEqmt(double plat, double ducat, int owned, int count)
        {
            Plat_Val = plat;
            Owned_Capped_Val = Math.Min(owned, count);
            Owned_Plat_Val = owned * plat;
            Owned_Ducat_Val = owned * ducat;
            Owned_Val = owned;
            Count_Val = count;

            PrimeUpdateDiff(false);
            Col1_Text2 = Plat_Val.ToString("F1");

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
            SetPrimeEqmt(plat, ducat, owned, count);
            Col2_Text3 = ducat.ToString();
            Col2_Img1 = DUCAT_SRC;
            Col2_Img1_Shown = "Visible";
            Col2_Margin1 = new Thickness(0, 0, 28, 0);
            Col2_Margin2 = new Thickness(0, 0, 10, 0);
        }

        public void SetPartText(double plat, int ducat, string rarity)
        {
            (NameColor, NameBrush) = rarity switch
            {
                var s when s.Contains("rare") => (RARE_COLOR, RARE_BRUSH),
                var s when s.Contains("uncomm") => (UNCOMMON_COLOR, UNCOMMON_BRUSH),
                var s when s.Contains("comm") => (COMMON_COLOR, COMMON_BRUSH),
                _ => (NameColor, NameBrush)
            };

            if (Name != "Forma Blueprint")
            {
                Plat_Val = plat;
                Ducat_Val = ducat;

                Col1_Text1 = "";
                Col1_Text2 = Plat_Val.ToString("F1");

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
            ChildrenFiltered = [.. Children];
        }

        public void FilterOutVaulted(bool additionalFilter = false)
        {
            if (!additionalFilter) ChildrenFiltered = [.. Children];
            ApplyFilter(node => string.IsNullOrEmpty(node.Vaulted));
        }

        private void ApplyFilter(Func<TreeNode, bool> predicate) =>
            ChildrenFiltered = ChildrenFiltered.Where(predicate).ToList();
        public void FilterOutUnowned() => ApplyFilter(node => node.Owned_Capped_Val > 0);
        public void FilterOutCompleted() => ApplyFilter(node => node.Owned_Capped_Val != node.Count_Val);
        public void FilterOutIncomplete() => ApplyFilter(node => node.Owned_Capped_Val == node.Count_Val);
        public void FilterOutMastered() => ApplyFilter(node => !node.Mastered);
        public void FilterOutUnmastered() => ApplyFilter(node => node.Mastered);

        public void FilterOutEquipment(string name)
        {
            if (EqmtName_Sort.Contains(name))
                ChildrenFiltered = [];
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
            TreeNode temp = current;
            while (temp != null)
            {
                prnt = temp.Name + "/" + prnt;
                temp = temp.current;
            }
            return prnt;
        }

        private void PrintItemToConsole(Dictionary<string, bool> matchedText)
        {
            string prnt = Name + ": ";
            TreeNode temp = current;
            while (temp != null)
            {
                prnt = temp.Name + "/" + prnt;
                temp = temp.current;
            }
            foreach (KeyValuePair<string, bool> kvp in matchedText)
            {
                prnt += kvp.Key + "(" + kvp.Value + ") ";
            }
            Main.AddLog(prnt);
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
                                Children = Children.OrderByDescending(p => p.Intact_Val).ToList();
                                ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.Intact_Val).ToList();
                                break;
                            case 2:
                                Children = Children.OrderByDescending(p => p.Radiant_Val).ToList();
                                ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.Radiant_Val).ToList();
                                break;
                            case 3:
                                Children = Children.OrderByDescending(p => p.Bonus_Val).ToList();
                                ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.Bonus_Val).ToList();
                                break;
                            default:
                                Children = Children.OrderBy(p => PadNumbers(p.Name)).ToList();
                                ChildrenFiltered = ChildrenFiltered.OrderBy(p => PadNumbers(p.Name)).ToList();
                                break;
                        }
                    }
                    else            // Parts
                    {
                        Children = Children.OrderByDescending(p => p.NameColor.G).ToList();
                        ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.NameColor.G).ToList();
                    }
                }
                else
                {
                    switch (index)
                    {
                        // 0 - Name
                        // 1 - Plat
                        // 2 - Unowned (Capped)
                        // 3 - Owned (Uncapped)
                        // 4 - Owned Plat Value

                        case 1:
                            Children = Children.OrderByDescending(p => p.Plat_Val).ToList();
                            ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.Plat_Val).ToList();
                            break;
                        case 2:
                            Children = Children.OrderBy(p => p.Owned_Capped_Val).OrderBy(p => p.Diff_Val).ToList();
                            ChildrenFiltered = ChildrenFiltered.OrderBy(p => p.Owned_Capped_Val).OrderBy(p => p.Diff_Val).ToList();
                            break;
                        case 3:
                            Children = Children.OrderByDescending(p => p.Owned_Val).ToList();
                            ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.Owned_Val).ToList();
                            break;
                        case 4:
                            Children = Children.OrderByDescending(p => p.Owned_Plat_Val).ToList();
                            ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.Owned_Plat_Val).ToList();
                            break;
                        case 5:
                            Children = Children.OrderByDescending(p => p.Owned_Ducat_Val).ToList();
                            ChildrenFiltered = ChildrenFiltered.OrderByDescending(p => p.Owned_Ducat_Val).ToList();
                            break;
                        default:
                            Children = Children.OrderBy(p => PadNumbers(p.Name)).ToList();
                            ChildrenFiltered = ChildrenFiltered.OrderBy(p => PadNumbers(p.Name)).ToList();
                            break;
                    }
                }
            }
        }

        public static string PadNumbers(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "[0-9]+", match => match.Value.PadLeft(5, '0'));
        }


        public string Col1_Text1 { get; private set { SetField(ref field, value); } } = "INT:";

        public string Col1_Text2 { get; private set { SetField(ref field, value); } } = "4.4";

        public ImageSource Col1_Img1 { get; private set { SetField(ref field, value); } } = null;

        public string Grid_Shown { get; private set { SetField(ref field, value); } } = "Visible";

        public string Col1_Img1_Shown { get; private set { SetField(ref field, value); } } = "Visible";

        public string Col2_Text1 { get; private set { SetField(ref field, value); } } = "RAD:";

        public string Col2_Text2 { get; private set { SetField(ref field, value); } } = "9.9";

        public string Col2_Text3 { get; private set { SetField(ref field, value); } } = "(+5.5)";

        public ImageSource Col2_Img1 { get; private set { SetField(ref field, value); } } = null;

        public string Col2_Img1_Shown { get; private set { SetField(ref field, value); } } = "Visible";

        public double Plat_Val { get; set { SetField(ref field, value); } } = 0;

        public int Ducat_Val { get; set { SetField(ref field, value); } } = 0;

        public int Owned_Val { get; set { SetField(ref field, value); } } = 0;

        public int Owned_Capped_Val { get; set { SetField(ref field, value); } } = 0;

        public double Owned_Plat_Val { get; set { SetField(ref field, value); } } = 0;

        public double Owned_Ducat_Val { get; set { SetField(ref field, value); } } = 0;

        public int Count_Val { get; set { SetField(ref field, value); } } = 0;
        public double Diff_Val { get; set { SetField(ref field, value); } } = 0;

        public double Intact_Val { get; set { SetField(ref field, value); } } = 0;

        public double Radiant_Val { get; set { SetField(ref field, value); } } = 0;

        public double Bonus_Val { get; set { SetField(ref field, value); } } = 0;

        public Visibility IsVisible
        {
            get { return (ForceVisibility || current == null || current.IsExpanded || topLevel) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool ForceVisibility
        {
            get;
            set
            {
                SetField(ref field, value);
                RaisePropertyChanged("IsVisible");
            }
        } = false;

        public bool IsExpanded
        {
            get;
            set
            {
                SetField(ref field, value);
                foreach (TreeNode kid in Children)
                    kid.RaisePropertyChanged("IsVisible");
            }
        } = false;

        public List<TreeNode> ChildrenFiltered { get; private set { SetField(ref field, value); } }

        public List<TreeNode> Children { get; private set { SetField(ref field, value); } }

        public bool Mastered { get; set { SetField(ref field, value); } } = false;

        public TreeNode current;
        public void AddChild(TreeNode kid)
        {
            kid.current = this;
            Children.Add(kid);
        }

        public override string ToString()
        {
            return Era + " " + Name;
        }

        public ICommand DecrementPart { get; private set { SetField(ref field, value); } }

        public ICommand IncrementPart { get; private set { SetField(ref field, value); } }

        public ICommand MarkComplete { get; private set { SetField(ref field, value); } }

        private string dataRef;

        public void MakeClickable(string eqmtRef)
        {
            dataRef = eqmtRef;
            DecrementPart = new SimpleCommand(DecrementPartFunc);
            IncrementPart = new SimpleCommand(IncrementPartFunc);
            MarkComplete = new SimpleCommand(MarkCompleteFunc);
        }

        public async void DecrementPartFunc()
        {
            if (current.dataRef != null)
            {
                await System.Threading.Tasks.Task.Run(() => DecrementPartThreaded(current));
            }
        }

        public async void IncrementPartFunc()
        {
            if (current.dataRef != null)
            {
                await System.Threading.Tasks.Task.Run(() => IncrementPartThreaded(current));
            }
        }

        public async void MarkCompleteFunc()
        {
            await System.Threading.Tasks.Task.Run(() => MarkSetAsComplete());

            /*Main.AddLog("test");
            Main.AddLog(current.dataRef);
            if (current.dataRef != null)
            {
                Main.AddLog("test");
            }*/
        }

        public void ReloadPartOwned(TreeNode Parent)
        {
            //DOES NOT UPDATE PARENT
            JObject job = Main.dataBase.equipmentData[Parent.dataRef]["parts"][dataRef] as JObject;
            Owned_Val = job["owned"].ToObject<int>();
            Owned_Capped_Val = Math.Min(Owned_Val, Count_Val);
            Owned_Plat_Val = Owned_Val * Plat_Val;
            Owned_Ducat_Val = Owned_Val * Ducat_Val;
            PrimeUpdateDiff(false);
        }

        private void DecrementPartThreaded(TreeNode Parent)
        {
            JObject job = Main.dataBase.equipmentData[Parent.dataRef]["parts"][dataRef] as JObject;
            int owned = Owned_Val;
            if (owned > 0)
            {
                job["owned"] = owned - 1;
                Main.dataBase.SaveAllJSONs();
                Owned_Val--;
                Owned_Capped_Val = Math.Min(Owned_Val, Count_Val);
                Owned_Plat_Val = Owned_Val * Plat_Val;
                Owned_Ducat_Val = Owned_Val * Ducat_Val;
                PrimeUpdateDiff(false);
                int count = Count_Val;
                Parent.Owned_Val--;
                Parent.Owned_Plat_Val -= Plat_Val;
                Parent.Owned_Ducat_Val -= Ducat_Val;
                if (owned <= count)
                {
                    Parent.Owned_Capped_Val--;
                    Parent.PrimeUpdateDiff(true);
                }
            }
        }

        private void IncrementPartThreaded(TreeNode Parent)
        {
            JObject job = Main.dataBase.equipmentData[Parent.dataRef]["parts"][dataRef] as JObject;
            int count = Count_Val;
            int owned = Owned_Val;
            job["owned"] = owned + 1;
            Main.dataBase.SaveAllJSONs();
            Owned_Val++;
            Owned_Capped_Val = Math.Min(Owned_Val, Count_Val);
            Owned_Plat_Val = Owned_Val * Plat_Val;
            Owned_Ducat_Val = Owned_Val * Ducat_Val;
            PrimeUpdateDiff(false);
            Parent.Owned_Val++;
            Parent.Owned_Plat_Val += Plat_Val;
            Parent.Owned_Ducat_Val += Ducat_Val;
            if (owned < count)
            {
                Parent.Owned_Capped_Val++;
                Parent.PrimeUpdateDiff(true);
            }
        }

        private void MarkSetAsComplete()
        {
            Main.dataBase.equipmentData[this.dataRef]["mastered"] = !Mastered;
            Mastered = !Mastered;
            Main.dataBase.SaveAllJSONs();
        }

        private void PrimeUpdateDiff(bool UseCappedOwned)
        {
            int owned = Owned_Val;
            if (UseCappedOwned)
            {
                owned = Owned_Capped_Val;
            }
            Diff_Val = owned / (double)(Count_Val) - 0.01 * Count_Val;
            Col1_Text1 = owned + "/" + Count_Val;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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



    public class RelicsTreeNode : INPC
    {
        public static ImageSource PLAT_SRC = (ImageSource)new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/plat.gif");
        public static ImageSource DUCAT_SRC = (ImageSource)new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/ducat_w.gif");
        public static Brush RARE_COLOR = new SolidColorBrush(Color.FromRgb(255,215,0));
        public static Brush UNCOMMON_COLOR = new SolidColorBrush(Color.FromRgb(192,192,192));
        public static Brush COMMON_COLOR = new SolidColorBrush(Color.FromRgb(205,127,50));


        public RelicsTreeNode(string name, string vaulted)
        {
            Name = name;
            Vaulted = vaulted;

            Children = new List<RelicsTreeNode>();
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }

        private Brush _color = new SolidColorBrush(Color.FromRgb(177,208,217));
        public Brush Name_Color
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

        public void HideItem() {
            Grid_Shown = "Collapsed";
        }

        public void ShowItem() {
            Grid_Shown = "Visible";
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
            double intact = 0;
            double radiant = 0;
            double bonus = 0;

            foreach (RelicsTreeNode node in Children)
            {
                if (node.Name_Color == RARE_COLOR)
                {
                    intact += 0.02 * node._plat;
                    radiant += 0.1 * node._plat;
                } else if (node.Name_Color == UNCOMMON_COLOR)
                {
                    intact += 0.11 * node._plat;
                    radiant += 0.2 * node._plat;
                } else
                {
                    intact += 0.2533 * node._plat;
                    radiant += 0.1667 * node._plat;
                }
            }
            bonus = radiant - intact;
            Grid_Shown = "Visible";

            Col1_Text1 = "INT";
            Col1_Text2 = ": " + intact.ToString("F1");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";

            Col2_Text1 = "RAD";
            Col2_Text2 = ": " + radiant.ToString("F1") + "(";
            if (bonus >= 0)
                Col2_Text2 += "+";
            Col2_Text2 += bonus.ToString("F1") + ")";

            Col2_Img1 = PLAT_SRC;
            Col2_Img1_Shown = "Visible";
        }

        public void SetPartText(double plat, int ducat, string rarity)
        {
            _plat = plat;
            _ducat = ducat;

            if (rarity.Contains("rare"))
                Name_Color = RARE_COLOR;
            else if (rarity.Contains("uncomm"))
                Name_Color = UNCOMMON_COLOR;
            else
                Name_Color = COMMON_COLOR;

            Col1_Text1 = "  PLAT";
            if (plat < 100)
                Col1_Text2 = ": " + plat.ToString("F1");
            else
                Col1_Text2 = ": " + plat.ToString("F0");

            Col1_Img1 = PLAT_SRC;
            Col1_Img1_Shown = "Visible";

            Col2_Text1 = "  DUCAT";
            Col2_Text2 = ": " + ducat.ToString();
            Col2_Img1 = DUCAT_SRC;
            Col2_Img1_Shown = "Visible";
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

        private string _grid_shown = "Visable";

        public string Grid_Shown {
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

        private List<RelicsTreeNode> _children;
        public List<RelicsTreeNode> Children
        {
            get { return _children; }
            private set { SetField(ref _children, value); }
        }

        public override string ToString()
        {
            return Name;
        }

    }
}

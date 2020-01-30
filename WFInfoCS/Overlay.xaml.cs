using System.Windows;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        public static double window_wid = 243.0;
        public static double window_hei = 141.0;
        public static double part_line_hei = 20.0; // TBD
        public static double part_margin_top = 39.0;
        public static double part_margin_bot = 82.0;
        public static double vol_margin_top = 104.0;
        public static double vol_margin_bot = 17.0;
        public static double plat_margin_left = 76.0;
        public static double plat_margin_top = 77.0;
        public static double plat_margin_bot = 43.0;
        public static double ducat_margin_left = 164.0;
        public static double ducat_margin_top = 77.0;
        public static double ducat_margin_bot = 43.0;
        public static double corner_margin_side = 23.0;
        public static double corner_margin_top = 15.0;
        public static double corner_margin_bot = 110.0;

        public static double plat_img_margin_left = 50.0;
        public static double plat_img_margin_bot = 44.0;
        public static double plat_img_hei_wid = 20.0;
        public static double ducat_img_margin_left = 138.0;
        public static double ducat_img_margin_bot = 44.0;
        public static double ducat_img_hei_wid = 20.0;

        public static double large_font = 18.0;
        public static double mid_font = 17.0;
        public static double small_font = 14.0;

        public Overlay()
        {
            InitializeComponent();
        }

        public void LoadTextData(string name, string plat, string ducats, string volume)
        {
            Part_Text.Text = name;
            Plat_Text.Text = plat;
            Ducat_Text.Text = ducats;
            Volume_Text.Text = volume + " sold last 48hrs";
        }

        public void Resize(int wid)
        {
            double scale = wid / window_wid;
            this.Width = wid;
            this.Height = scale * window_hei;

            Thickness margin;

            // Part_Text
            margin = Part_Margin.Margin;
            margin.Top = part_margin_top * scale;
            margin.Bottom = part_margin_bot * scale;
            Part_Margin.Margin = margin;

            // Vaulted_Text
            margin = Vaulted_Margin.Margin;
            margin.Top = corner_margin_top * scale;
            margin.Bottom = corner_margin_bot * scale;
            margin.Right = corner_margin_side * scale;
            Vaulted_Margin.Margin = margin;
            Vaulted_Text.FontSize = small_font *  scale;

            // Owned_Text
            margin = Owned_Margin.Margin;
            margin.Top = corner_margin_top * scale;
            margin.Bottom = corner_margin_bot * scale;
            margin.Left = corner_margin_side * scale;
            Owned_Margin.Margin = margin;
            Owned_Text.FontSize = small_font * scale;

            // Volume_Text
            margin = Volume_Margin.Margin;
            margin.Top = vol_margin_top * scale;
            margin.Bottom = vol_margin_bot * scale;
            Volume_Margin.Margin = margin;
            Volume_Text.FontSize = mid_font * scale;

            // Plat_Text
            margin = Plat_Margin.Margin;
            margin.Top = plat_margin_top * scale;
            margin.Bottom = plat_margin_bot * scale;
            margin.Left = plat_margin_left * scale;
            Plat_Margin.Margin = margin;
            Plat_Text.FontSize = mid_font * scale;


            // Ducat_Text
            margin = Ducat_Margin.Margin;
            margin.Top = ducat_margin_top * scale;
            margin.Bottom = ducat_margin_bot * scale;
            margin.Left = ducat_margin_left * scale;
            Ducat_Margin.Margin = margin;
            Ducat_Text.FontSize = mid_font * scale;

            // Plat_IMG
            margin = Plat_IMG.Margin;
            margin.Bottom = plat_img_margin_bot * scale;
            margin.Left = plat_img_margin_left * scale;
            Plat_IMG.Margin = margin;
            Plat_IMG.Height = plat_img_hei_wid * scale;
            Plat_IMG.Width = Plat_IMG.Height;

            // Ducat_IMG
            margin = Ducat_IMG.Margin;
            margin.Bottom = ducat_img_margin_bot * scale;
            margin.Left = ducat_img_margin_left * scale;
            Ducat_IMG.Margin = margin;
            Ducat_IMG.Height = ducat_img_hei_wid * scale;
            Ducat_IMG.Width = Ducat_IMG.Height;
        }

        public void Display(int x, int y)
        {
            this.Left = x;
            this.Top = y;
            this.Show();
        }
    }
}

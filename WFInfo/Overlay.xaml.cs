using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        public static double window_wid = 243.0;
        public static double window_hei = 141.0;
        public static double part_line_hei = 20.0; // TBD
        public static double partMarginTop = 39.0;
        public static double partMarginBottom = 80.0;
        public static double volumeMarginTop = 104.0;
        public static double volumeMarginBottom = 17.0;
        //public static double platMarginLeft = 76.0;
        public static double platMarginRight = 163.0;
        public static double platMarginTop = 77.0;
        public static double platMarginBottom = 43.0;
        //public static double ducatMarginLeft = 162.0;
        public static double ducatMarginRight = 78.0;
        public static double ducatMarginTop = 77.0;
        public static double ducatMarginbottom = 43.0;
        public static double cornerMarginSide = 23.0;
        public static double cornerMarginTop = 15.0;
        public static double cornerMarginBottom = 110.0;

        public static double platImageMarginLeft = 88.0;
        public static double platImageMarginBottom = 44.0;
        public static double platImageHeightWidth = 20.0;
        public static double ducatImageMarginLeft = 172.0;
        public static double ducatImageMarginBottom = 44.0;
        public static double ducatImageHeightWidth = 20.0;
        
        public static double large_font = 18.0;
        public static double mediumFont = 17.0;
        public static double smallFont = 14.0;

        private static Color blu = Color.FromRgb(177, 208, 217);
        private static SolidColorBrush bluBrush = new SolidColorBrush(blu);

        private DispatcherTimer hider = new DispatcherTimer();

        public Overlay()
        {
            hider.Interval = TimeSpan.FromSeconds(10);
            hider.Tick += HideOverlay;
            InitializeComponent();
        }

        public void bestPlatChoice()
        {
            platText.FontWeight = FontWeights.Bold;
            partText.FontWeight = FontWeights.Bold;
            platText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            partText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        }

        public void bestDucatChoice()
        {
            ducatText.FontWeight = FontWeights.Bold;
            partText.FontWeight = FontWeights.Bold;
            ducatText.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
            partText.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        }

        public void bestOwnedChoice()
        {
            ownedText.FontWeight = FontWeights.Bold;
            partText.FontWeight = FontWeights.Bold;
            ownedText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 215));
            partText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 215));
        }

        public void LoadTextData(string name, string plat, string ducats, string volume, bool vaulted, string owned, bool hideRewardInfo)
        {
            ducatText.Foreground = bluBrush;
            ducatText.FontWeight = FontWeights.Normal;
            platText.Foreground = bluBrush;
            platText.FontWeight = FontWeights.Normal;
            ownedText.Foreground = bluBrush;
            ownedText.FontWeight = FontWeights.Normal;
            partText.Foreground = bluBrush;
            partText.FontWeight = FontWeights.Normal;

            if (Settings.highContrast)
            {
                Console.WriteLine("Turning high contrast on");
                BackgroundGrid.Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
            else
            {
                new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }

            partText.Text = name;
            if (hideRewardInfo)
            {
                platText.Visibility = Visibility.Hidden;
                ducatText.Visibility = Visibility.Hidden;
                volumeText.Visibility = Visibility.Hidden;
                vaultedMargin.Visibility = Visibility.Hidden;
                platImage.Visibility = Visibility.Hidden;
                ducatImage.Visibility = Visibility.Hidden;
                ownedText.Text = "";
            }
            else
            {
                platText.Visibility = Visibility.Visible;
                ducatText.Visibility = Visibility.Visible;
                volumeText.Visibility = Visibility.Visible;
                vaultedMargin.Visibility = Visibility.Visible;
                platImage.Visibility = Visibility.Visible;
                ducatImage.Visibility = Visibility.Visible;
                platText.Text = plat;
                ducatText.Text = ducats;
                volumeText.Text = volume + " sold last 48hrs";
                if (vaulted)
                    vaultedMargin.Visibility = Visibility.Visible;
                else
                    vaultedMargin.Visibility = Visibility.Hidden;
                if (owned.Length > 0)
                    ownedText.Text = owned + " OWNED";
                else
                    ownedText.Text = "";
            }
        }

        public void clear()
        {
            ducatText.Foreground = bluBrush;
            ducatText.FontWeight = FontWeights.Normal;
            platText.Foreground = bluBrush;
            platText.FontWeight = FontWeights.Normal;
            ownedText.Foreground = bluBrush;
            ownedText.FontWeight = FontWeights.Normal;
            partText.Foreground = bluBrush;
            partText.FontWeight = FontWeights.Normal;
        }

        public void Resize(int wid)
        {
            double scale = wid / window_wid;
            Width = wid;
            Height = scale * window_hei;

            Thickness margin;

            // Part_Text
            margin = partMargin.Margin;
            margin.Top = partMarginTop * scale;
            margin.Bottom = partMarginBottom * scale;
            partMargin.Margin = margin;

            // Vaulted_Text
            margin = vaultedMargin.Margin;
            margin.Top = cornerMarginTop * scale;
            margin.Bottom = cornerMarginBottom * scale;
            margin.Right = cornerMarginSide * scale;
            vaultedMargin.Margin = margin;
            vaultedText.FontSize = smallFont * scale;

            // Owned_Text
            margin = ownedMargin.Margin;
            margin.Top = cornerMarginTop * scale;
            margin.Bottom = cornerMarginBottom * scale;
            margin.Left = cornerMarginSide * scale;
            ownedMargin.Margin = margin;
            ownedText.FontSize = smallFont * scale;

            // Volume_Text
            margin = volumeMargin.Margin;
            margin.Top = volumeMarginTop * scale;
            margin.Bottom = volumeMarginBottom * scale;
            volumeMargin.Margin = margin;
            volumeText.FontSize = mediumFont * scale;

            // Plat_Text
            margin = platMargin.Margin;
            margin.Top = platMarginTop * scale;
            margin.Bottom = platMarginBottom * scale;
            margin.Right = platMarginRight * scale;
            platMargin.Margin = margin;
            platText.FontSize = mediumFont * scale;


            // Ducat_Text
            margin = ducatMargin.Margin;
            margin.Top = ducatMarginTop * scale;
            margin.Bottom = ducatMarginbottom * scale;
            margin.Right = ducatMarginRight * scale;
            ducatMargin.Margin = margin;
            ducatText.FontSize = mediumFont * scale;

            // Plat_IMG
            margin = platImage.Margin;
            margin.Bottom = platImageMarginBottom * scale;
            margin.Left = platImageMarginLeft * scale;
            platImage.Margin = margin;
            platImage.Height = platImageHeightWidth * scale;
            platImage.Width = platImage.Height;

            // Ducat_IMG
            margin = ducatImage.Margin;
            margin.Bottom = ducatImageMarginBottom * scale;
            margin.Left = ducatImageMarginLeft * scale;
            ducatImage.Margin = margin;
            ducatImage.Height = ducatImageHeightWidth * scale;
            ducatImage.Width = ducatImage.Height;
        }

        public void Display(int x, int y, int wait = 10000)
        {
            hider.Stop();
            hider.Interval = TimeSpan.FromMilliseconds(wait);
            Left = x;
            Top = y;
            Show();
            hider.Start();
        }

        public void HideOverlay(object sender, EventArgs e)
        {
            hider.Stop();
            Hide();
            Main.StatusUpdate("WFinfo is ready",0);
        }
    }
}

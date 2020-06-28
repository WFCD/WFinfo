using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        private static double window_wid = 243.0;
        private static double window_hei = 141.0;
        private static double part_line_hei = 20.0; // TBD
        private static double partMarginTop = 39.0;
        private static double partMarginBottom = 80.0;
        private static double volumeMarginTop = 104.0;
        private static double volumeMarginBottom = 17.0;
        private static double platMarginRight = 163.0;
        private static double platMarginTop = 77.0;
        private static double platMarginBottom = 43.0;
        private static double ducatMarginRight = 67.0;
        private static double ducatMarginTop = 77.0;
        private static double ducatMarginbottom = 43.0;
        private static double cornerMarginSide = 23.0;
        private static double cornerMarginTop = 15.0;
        private static double cornerMarginBottom = 110.0;

        private static double platImageMarginLeft = 88.0;
        private static double platImageMarginBottom = 44.0;
        private static double platImageHeightWidth = 20.0;
        private static double ducatImageMarginLeft = 172.0;
        private static double ducatImageMarginBottom = 44.0;
        private static double ducatImageHeightWidth = 20.0;

        private static double platMarginRightSanpit = 187;
        private static double ducatMargineRightSanpit = 119;
        private static double EfficencyMarginRight = 51;

        private static double platImageMarginLeftSanpit = 61;
        private static double ducatImageMarginLeftSanpit = 130;
        private static double EfficencyplatImageMarginLeft = 206.0;
        private static double EfficencyplatImageMarginBottom = 44.0;
        private static double EfficencyplatImageHeightWidth = 12.0;
        private static double EfficencyducatImageMarginLeft = 197.0;
        private static double EfficencyducatImageMarginBottom = 52.0;
        private static double EfficencyducatImageHeightWidth = 12.0;

        private static double largefont = 18.0;
        private static double mediumFont = 17.0;
        private static double smallFont = 14.0;

        private static readonly Color blu = Color.FromRgb(177, 208, 217);
        private static readonly SolidColorBrush bluBrush = new SolidColorBrush(blu);

        private readonly DispatcherTimer hider = new DispatcherTimer();

        public static bool rewardsDisplaying;

        
        public Overlay()
        {
            hider.Interval = TimeSpan.FromSeconds(10);
            hider.Tick += HideOverlay;
            InitializeComponent();
        }
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            Win32.SetWindowExTransparent(hwnd);
        }

        public void BestPlatChoice()
        {
            platText.FontWeight = FontWeights.Bold;
            partText.FontWeight = FontWeights.Bold;
            platText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            partText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        }

        public void BestDucatChoice()
        {
            ducatText.FontWeight = FontWeights.Bold;
            partText.FontWeight = FontWeights.Bold;
            ducatText.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
            partText.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        }

        public void BestOwnedChoice()
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
                Debug.WriteLine("Turning high contrast on");
                BackgroundGrid.Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
            else
            {
                BackgroundGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
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
                if (owned == null)
                {
                    throw new ArgumentNullException(nameof(owned));
                }
                if (owned.Length > 0)
                    ownedText.Text = owned + " OWNED";
                else
                    ownedText.Text = "";
            }
        }

        public void Clear()
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
            
            //snapit plat text
            margin = PlatMargineSnap.Margin;
            margin.Top = platMarginTop * scale;
            margin.Bottom = platMarginBottom * scale;
            margin.Right = platMarginRightSanpit * scale;
            PlatMargineSnap.Margin = margin;
            PlatTextSnap.FontSize = mediumFont * scale;
            
            //snapit ducat text
            margin = DucatMargineSnap.Margin;
            margin.Top = platMarginTop * scale;
            margin.Bottom = platMarginBottom * scale;
            margin.Right = ducatMargineRightSanpit * scale;
            DucatMargineSnap.Margin = margin;
            DucatTextSnap.FontSize = mediumFont * scale;
            
            //snapit efficency text
            margin = EfficencyMargin.Margin;
            margin.Top = platMarginTop * scale;
            margin.Bottom = platMarginBottom * scale;
            margin.Right = EfficencyMarginRight * scale;
            EfficencyMargin.Margin = margin;
            EfficencyText.FontSize = mediumFont * scale;

            //snapit ducat image
            margin = DucatImageSnap.Margin;
            margin.Top = platMarginTop * scale;
            margin.Bottom = ducatImageMarginBottom * scale;
            margin.Left = ducatImageMarginLeftSanpit * scale;
            DucatImageSnap.Margin = margin;
            DucatImageSnap.Height = platImageHeightWidth * scale;
            DucatImageSnap.Width = ducatImage.Height;
            
            //snapit plat image
            margin = platImage.Margin;
            margin.Bottom = platImageMarginBottom * scale;
            margin.Left = 61 * scale;
            PlatImageSnap.Margin = margin;
            PlatImageSnap.Height = platImageHeightWidth * scale;
            PlatImageSnap.Width = platImage.Height;
            
            //snapit plat efficency image
            margin = EfficencyPlatinumImage.Margin;
            margin.Bottom = EfficencyplatImageMarginBottom * scale;
            margin.Left = EfficencyplatImageMarginLeft * scale;
            EfficencyPlatinumImage.Margin = margin;
            EfficencyPlatinumImage.Height = EfficencyplatImageHeightWidth * scale;
            EfficencyPlatinumImage.Width = ducatImage.Height;
                
            //snapit ducat efficency image
            margin = EfficencyDucatImage.Margin;
            margin.Bottom = EfficencyducatImageMarginBottom * scale;
            margin.Left = EfficencyducatImageMarginLeft * scale;
            EfficencyDucatImage.Margin = margin;
            EfficencyDucatImage.Height = EfficencyducatImageHeightWidth * scale;
            EfficencyDucatImage.Width = ducatImage.Height;
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

        private void HideOverlay(object sender, EventArgs e)
        {
            hider.Stop();
            Hide();
            Main.StatusUpdate("WFinfo is ready",0);
            rewardsDisplaying = false;
        }

        public void toSnapit(string efficiency, SolidColorBrush color)
        {
            platImage.Visibility = Visibility.Collapsed;
            platMargin.Visibility = Visibility.Collapsed;
            
            ducatImage.Visibility = Visibility.Collapsed;
            ducatMargin.Visibility = Visibility.Collapsed;

            DucatTextSnap.Visibility = Visibility.Visible;
            DucatImageSnap.Visibility = Visibility.Visible;
            
            PlatTextSnap.Visibility = Visibility.Visible;
            PlatImageSnap.Visibility = Visibility.Visible;
            
            EfficencyMargin.Visibility = Visibility.Visible;
            EfficencyDucatImage.Visibility = Visibility.Visible;
            EfficencyPlatinumImage.Visibility = Visibility.Visible;
            EfficencyText.Text = efficiency;
            DucatTextSnap.Text = ducatText.Text;
            PlatTextSnap.Text = platText.Text;
            EfficencyText.Foreground = color;
        }
    }
}

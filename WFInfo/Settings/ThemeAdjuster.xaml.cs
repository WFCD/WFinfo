using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for verifyCount.xaml
    /// </summary>
    public partial class ThemeAdjuster : Window
    {
        private readonly Settings.SettingsViewModel _viewModel;
        public Settings.SettingsViewModel SettingsViewModel => _viewModel;

        public static ThemeAdjuster INSTANCE;
        private Bitmap unfiltered;
        public BitmapImage displayImage;

        public ThemeAdjuster()
        {
            InitializeComponent();
            DataContext = this;
            INSTANCE = this;
            _viewModel = Settings.SettingsViewModel.Instance;
        }
        public static void ShowThemeAdjuster()
        {
            if (INSTANCE != null)
            {
                INSTANCE.Show();
                INSTANCE.Focus();
            }
        }

        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            //from https://stackoverflow.com/questions/22499407/how-to-display-a-bitmap-in-a-wpf-image
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void ApplyFilter(object sender, RoutedEventArgs e)
        {
            if (unfiltered != null)
            {
                Bitmap filtered = OCR.ScaleUpAndFilter(unfiltered, WFtheme.CUSTOM, out int[] rowHits, out int[] colHits);
                displayImage = BitmapToImageSource(filtered);
                previewImage.Source = displayImage;
                filtered.Dispose();
            }
        }

        private void ShowUnfiltered(object sender, RoutedEventArgs e)
        {
            if (unfiltered != null)
            {
                displayImage = BitmapToImageSource(unfiltered);
                previewImage.Source = displayImage;
            }
        }

        private void LoadLatest(object sender, RoutedEventArgs e)
        {
            List<FileInfo> files = (new DirectoryInfo(Main.AppPath + @"\Debug\")).GetFiles()
                .Where(f => f.Name.Contains("FullScreenShot"))
                .ToList();
            files = files.OrderBy(f => f.CreationTimeUtc).ToList();


            Bitmap image = null;
            try
            {
                foreach (FileInfo file in files)
                {
                    Main.AddLog("Loading filter testing with file: " + file.Name);

                    //Get the path of specified file
                    image = new Bitmap(file.FullName);
                    break;
                }
            }
            catch (Exception exc)
            {
                Main.AddLog(exc.Message);
                Main.AddLog(exc.StackTrace);
                Main.StatusUpdate("Failed to load image", 1);
            }
            if (image != null)
            {
                unfiltered = image;
            }

        }
        private void LoadFromFile(object sender, RoutedEventArgs e)
        {
            // Using WinForms for the openFileDialog because it's simpler and much easier
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Task.Factory.StartNew(
                        () =>
                        {

                            Bitmap image = null;
                            try
                            {
                                foreach (string file in openFileDialog.FileNames)
                                {
                                        Main.AddLog("Loading filter testing with file: " + file);

                                        //Get the path of specified file
                                        image = new Bitmap(file);
                                    break;
                                }
                            }
                            catch (Exception exc)
                            {
                                Main.AddLog(exc.Message);
                                Main.AddLog(exc.StackTrace);
                                Main.StatusUpdate("Failed to load image", 1);
                            }
                            if (image != null)
                            {
                                unfiltered = image;
                            }
                        });
                }
                else
                {
                    Main.StatusUpdate("Failed to load image", 1);
                }
            }
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
    }
}

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
            files.Reverse();

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
                Main.RunOnUIThread(() => { ShowUnfiltered(null, null); });
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
                                Main.RunOnUIThread(() => { ShowUnfiltered(null, null); });
                            }
                        });
                }
                else
                {
                    Main.StatusUpdate("Failed to load image", 1);
                }
            }
        }

        private void ExportFilterJson(object sender, RoutedEventArgs e)
        {
            JObject exp = new JObject
                {
                    { "CF_usePrimaryHSL", _viewModel.CF_usePrimaryHSL },
                    { "CF_pHueMax", _viewModel.CF_pHueMax },
                    { "CF_pHueMin", _viewModel.CF_pHueMin },
                    { "CF_pSatMax", _viewModel.CF_pSatMax },
                    { "CF_pSatMin", _viewModel.CF_pSatMin },
                    { "CF_pBrightMax", _viewModel.CF_pBrightMax },
                    { "CF_pBrightMin", _viewModel.CF_pBrightMin },

                    { "CF_usePrimaryRGB", _viewModel.CF_usePrimaryRGB },
                    { "CF_pRMax", _viewModel.CF_pRMax },
                    { "CF_pRMin", _viewModel.CF_pRMin },
                    { "CF_pGMax", _viewModel.CF_pGMax },
                    { "CF_pGMin", _viewModel.CF_pGMin },
                    { "CF_pBMax", _viewModel.CF_pBMax },
                    { "CF_pBMin", _viewModel.CF_pBMin },

                    { "CF_useSecondaryHSL", _viewModel.CF_useSecondaryHSL },
                    { "CF_sHueMax", _viewModel.CF_sHueMax },
                    { "CF_sHueMin", _viewModel.CF_sHueMin },
                    { "CF_sSatMax", _viewModel.CF_sSatMax },
                    { "CF_sSatMin", _viewModel.CF_sSatMin },
                    { "CF_sBrightMax", _viewModel.CF_sBrightMax },
                    { "CF_sBrightMin", _viewModel.CF_sBrightMin },

                    { "CF_useSecondaryRGB", _viewModel.CF_useSecondaryRGB },
                    { "CF_sRMax", _viewModel.CF_sRMax },
                    { "CF_sRMin", _viewModel.CF_sRMin },
                    { "CF_sGMax", _viewModel.CF_sGMax },
                    { "CF_sGMin", _viewModel.CF_sGMin },
                    { "CF_sBMax", _viewModel.CF_sBMax },
                    { "CF_sBMin", _viewModel.CF_sBMin }
                };
            filterTextBox.Text = JsonConvert.SerializeObject(exp, Formatting.None);
        }

        private void ImportFilterJson(object sender, RoutedEventArgs e)
        {
            string input = filterTextBox.Text;
            try
            {
                //try to read all parameters to temporary variables
                JObject json = JsonConvert.DeserializeObject<JObject>(input);
                bool CF_usePrimaryHSL = json["CF_usePrimaryHSL"].ToObject<bool>();
                float CF_pHueMax = json["CF_pHueMax"].ToObject<float>();
                float CF_pHueMin = json["CF_pHueMin"].ToObject<float>();
                float CF_pSatMax = json["CF_pSatMax"].ToObject<float>();
                float CF_pSatMin = json["CF_pSatMin"].ToObject<float>();
                float CF_pBrightMax = json["CF_pBrightMax"].ToObject<float>();
                float CF_pBrightMin = json["CF_pBrightMin"].ToObject<float>();

                bool CF_usePrimaryRGB = json["CF_usePrimaryRGB"].ToObject<bool>();
                int CF_pRMax = json["CF_pRMax"].ToObject<int>();
                int CF_pRMin = json["CF_pRMin"].ToObject<int>();
                int CF_pGMax = json["CF_pGMax"].ToObject<int>();
                int CF_pGMin = json["CF_pGMin"].ToObject<int>();
                int CF_pBMax = json["CF_pBMax"].ToObject<int>();
                int CF_pBMin = json["CF_pBMin"].ToObject<int>();

                bool CF_useSecondaryHSL = json["CF_useSecondaryHSL"].ToObject<bool>();
                float CF_sHueMax = json["CF_sHueMax"].ToObject<float>();
                float CF_sHueMin = json["CF_sHueMin"].ToObject<float>();
                float CF_sSatMax = json["CF_sSatMax"].ToObject<float>();
                float CF_sSatMin = json["CF_sSatMin"].ToObject<float>();
                float CF_sBrightMax = json["CF_sBrightMax"].ToObject<float>();
                float CF_sBrightMin = json["CF_sBrightMin"].ToObject<float>();

                bool CF_useSecondaryRGB = json["CF_useSecondaryRGB"].ToObject<bool>();
                int CF_sRMax = json["CF_sRMax"].ToObject<int>();
                int CF_sRMin = json["CF_sRMin"].ToObject<int>();
                int CF_sGMax = json["CF_sGMax"].ToObject<int>();
                int CF_sGMin = json["CF_sGMin"].ToObject<int>();
                int CF_sBMax = json["CF_sBMax"].ToObject<int>();
                int CF_sBMin = json["CF_sBMin"].ToObject<int>();


                //all parameters read successfully, apply to actual settings
                _viewModel.CF_usePrimaryHSL = CF_usePrimaryHSL;
                _viewModel.CF_pHueMax = CF_pHueMax;
                _viewModel.CF_pHueMin = CF_pHueMin;
                _viewModel.CF_pSatMax = CF_pSatMax;
                _viewModel.CF_pSatMin = CF_pSatMin;
                _viewModel.CF_pBrightMax = CF_pBrightMax;
                _viewModel.CF_pBrightMin = CF_pBrightMin;

                _viewModel.CF_usePrimaryRGB = CF_usePrimaryRGB;
                _viewModel.CF_pRMax = CF_pRMax;
                _viewModel.CF_pRMin = CF_pRMin;
                _viewModel.CF_pGMax = CF_pGMax;
                _viewModel.CF_pGMin = CF_pGMin;
                _viewModel.CF_pBMax = CF_pBMax;
                _viewModel.CF_pBMin = CF_pBMin;

                _viewModel.CF_useSecondaryHSL = CF_useSecondaryHSL;
                _viewModel.CF_sHueMax = CF_sHueMax;
                _viewModel.CF_sHueMin = CF_sHueMin;
                _viewModel.CF_sSatMax = CF_sSatMax;
                _viewModel.CF_sSatMin = CF_sSatMin;
                _viewModel.CF_sBrightMax = CF_sBrightMax;
                _viewModel.CF_sBrightMin = CF_sBrightMin;

                _viewModel.CF_useSecondaryRGB = CF_useSecondaryRGB;
                _viewModel.CF_sRMax = CF_sRMax;
                _viewModel.CF_sRMin = CF_sRMin;
                _viewModel.CF_sGMax = CF_sGMax;
                _viewModel.CF_sGMin = CF_sGMin;
                _viewModel.CF_sBMax = CF_sBMax;
                _viewModel.CF_sBMin = CF_sBMin;
            }
            catch (Exception exc)
            {
                Main.AddLog("Custom Filter Import failed. Input: " + Environment.NewLine + input + Environment.NewLine + "Custom filter import error message: " + exc.Message);
                Main.SpawnErrorPopup(DateTime.UtcNow);
            }
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            if (unfiltered != null)
            {
                unfiltered.Dispose();
            }
            unfiltered = null;
            displayImage = null;
            previewImage.Source = null;
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

using AutoUpdaterDotNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WFInfo.Settings;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for errorDialogue.xaml
    /// </summary>
    public partial class UpdateDialogue : Window
    {
        UpdateInfoEventArgs updateInfo;
        readonly WebClient WebClient;
        private readonly SettingsViewModel settings = SettingsViewModel.Instance;

        public UpdateDialogue(UpdateInfoEventArgs args)
        {
            InitializeComponent();
            updateInfo = args;

            string version = args.CurrentVersion.ToString();
            if (!args.IsUpdateAvailable || (settings.Ignored == version))
                return;
            version = version.Substring(0, version.LastIndexOf("."));

            NewVersionText.Text = "WFInfo version " + version + " has been released!";
            OldVersionText.Text = "You have version " + Main.BuildVersion + " installed.";

            WebClient = new WebClient() { Proxy = new WebProxy(new Uri("http://127.0.0.1:7890")) };
            WebClient.Headers.Add("platform", "pc");
            WebClient.Headers.Add("language", "en");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WebClient.Headers.Add("User-Agent", "WFCD");
            JArray releases = JsonConvert.DeserializeObject<JArray>(WebClient.DownloadString("https://api.github.com/repos/WFCD/WFInfo/releases"));
            foreach (JObject prop in releases)
            {
                if (!prop["prerelease"].ToObject<bool>())
                {
                    string tag_name = prop["tag_name"].ToString();
                    if (tag_name.Substring(1) == Main.BuildVersion)
                        break;
                    TextBlock tag = new TextBlock();
                    tag.Text = tag_name;
                    tag.FontWeight = FontWeights.Bold;
                    ReleaseNotes.Children.Add(tag);
                    TextBlock body = new TextBlock();
                    body.Text = prop["body"].ToString() + "\n";
                    body.Padding = new Thickness(10, 0, 0, 0);
                    body.TextWrapping = TextWrapping.Wrap;
                    ReleaseNotes.Children.Add(body);
                }
            }

            Show();
            Focus();
        }

        public void YesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (AutoUpdater.DownloadUpdate(updateInfo))
                    WFInfo.MainWindow.INSTANCE.Exit(null, null);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Skip(object sender, RoutedEventArgs e)
        {
            settings.Ignored = updateInfo.CurrentVersion.ToString();
            SettingsWindow.Save();
            Close();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

    }
}

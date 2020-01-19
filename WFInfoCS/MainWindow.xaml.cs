using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WFInfoCS {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		Main main = new Main(); //subscriber


		public MainWindow() {
			LowLevelListener listener = new LowLevelListener(); //publisher
			Main.updatedStatus += this.ChangeStatus;
			try {
				if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\settings.json")) {
					Settings.settingsObj = JObject.Parse(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\settings.json"));
				} else {
					Settings.settingsObj = JObject.Parse("{\"Display\":\"Overlay\"," +
						"\"ActivationKey\":\"Snapshot\"," +
						"\"Scaling\":100.0," +
						"\"Auto\":false," +
						"\"Debug\":false}");
				}
				if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\color.json")) {
					Settings.colorArray = JObject.Parse(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\color.json"));
				} else {
					Settings.colorArray = JObject.Parse("{\"rarityColor\":[\"b4876e\",\"c8c8c8\",\"d4c078\"]," +
						"\"Vitruvian\":\"bda865\"," +
						"\"Stalker\":\"961f23\"," +
						"\"Baruk\":\"eec169\"," +
						"\"Corpus\":\"23c8f5\"," +
						"\"Fortuna\":\"3969c0\"," +
						"\"Grineer\":\"ffbd66\"," +
						"\"Lotus\":\"24b8f2\"," +
						"\"Nidus\":\"8c265c\"," +
						"\"Orokin\":\"14291d\"," +
						"\"Tenno\":\"094e6a\"," +
						"\"High contrast\":\"027fd9\"," +
						"\"Legacy\":\"ffffff\"," +
						"\"Equinox\":\"9e9fa7\"}");
					File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\color.json", JsonConvert.SerializeObject(Settings.colorArray, Formatting.Indented));

				}
				Settings.activationKey = (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("ActivationKey").ToString());
				Settings.debug = (bool)Settings.settingsObj.GetValue("Debug");
				Settings.auto = (bool)Settings.settingsObj.GetValue("Auto");
				Settings.Scaling = Convert.ToInt32(Settings.settingsObj.GetValue("Scaling"));

				String thisprocessname = Process.GetCurrentProcess().ProcessName;
				if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1) {
					Main.AddLog("Duplicate process found");
					this.Close();
				}

				LowLevelListener.KeyAction += main.OnKeyAction;
				listener.Hook();
				InitializeComponent();
				Version.Content = main.BuildVersion;
				ChangeStatus("loaded", 0);

				Main.AddLog("Sucsesfully launched");
			}
			catch (Exception e) {
				Main.AddLog("An error occured while loading the main window: " + e.Message);
			}
		}

		public void ChangeStatus(string status, int serverity) {
			Status.Content = "Status: " + status;
			switch (serverity) {
				case 0: //default, no problem
				Status.Foreground = main.LightBlue;
				break;
				case 1: //severe, red text
				Status.Foreground = Brushes.Red;
				break;
				case 2: //warning, orange text
				Status.Foreground = Brushes.Orange;
				break;
				default: //Uncaught, big problem
				Status.Foreground = Brushes.Yellow;
				break;
			}
		}

		private void Exit(object sender, RoutedEventArgs e) {
			App.Current.Shutdown();
		}

		private void Minimise(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}

		private void Website_click(object sender, RoutedEventArgs e) {
			ChangeStatus("Go go website", 0);
			Process.Start("https://wfinfo.warframestat.us/");
		}

		private void Relics_click(object sender, RoutedEventArgs e) {
			//todo, open new window, showing all relics
			ChangeStatus("Relics not implemented", 2);
		}

		private void Gear_click(object sender, RoutedEventArgs e) {
			//todo, opens new window, shows all prime items
			ChangeStatus("This should work, big oopsie, scaling teeeeeeeeeeeeeeeeeeeest", 1);

		}
		private void Settings_click(object sender, RoutedEventArgs e) {
			Settings settingsWindow = new Settings();
			settingsWindow.Show();
			//ChangeStatus("Something uncaught", -1);
		}

		private void ReloadWikiClick(object sender, RoutedEventArgs e) {
			//todo reloads wiki data
		}

		private void ReloadDropClick(object sender, RoutedEventArgs e) {
			//todo reloads de's data
		}

		private void ReloadMarketClick(object sender, RoutedEventArgs e) {
			//todo reloads warframe.market data
		}

		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
using System.Windows;
using System.Windows.Input;

namespace WFInfoCS {
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window : System.Windows.Window {
		public Window() {
			InitializeComponent();
		}
		public void loadTextData(string name, string plat, string ducats, string volume, bool vaulted, string owned, int partNumber) {
			Show();
			switch (partNumber) {
				case 0:
				firstPartText.Text = name;
				firstPlatText.Text = plat;
				firstDucatText.Text = ducats;
				firstVolumeText.Text = volume + " sold last 48hrs";
				if (vaulted) { firstVaultedMargin.Visibility = Visibility.Visible; }
				firstOwnedText.Text = owned + " owned";
				Width = 250;
				break;

				case 1:
				secondPartText.Text = name;
				secondPlatText.Text = plat;
				secondDucatText.Text = ducats;
				secondVolumeText.Text = volume + " sold last 48hrs";
				if (vaulted) { secondVaultedMargin.Visibility = Visibility.Visible; }
				firstOwnedText.Text = owned + " owned";
				Width = 500;
				break;

				case 2:
				thirdPartText.Text = name;
				thirdPlatText.Text = plat;
				thirdDucatText.Text = ducats;
				thirdVolumeText.Text = volume + " sold last 48hrs";
				if (vaulted) { thirdVaultedMargin.Visibility = Visibility.Visible; }
				thirdOwnedText.Text = owned + " owned";
				Width = 750;
				break;

				case 3:
				fourthPartText.Text = name;
				fourthPlatText.Text = plat;
				fourthDucatText.Text = ducats;
				fourthVolumeText.Text = volume + " sold last 48hrs";
				if (vaulted) { fourthVaultedMargin.Visibility = Visibility.Visible; }
				fourthOwnedText.Text = owned + " owned";
				Width = 1000;
				break;

				default:
				Main.AddLog("something went wrong while displaying: " + name);
				Main.StatusUpdate("something went wrong while displaying: " + name + " in window", 1);
				break;
			}
		}
		private void Exit(object sender, RoutedEventArgs e) {
			Close();
		}
		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}

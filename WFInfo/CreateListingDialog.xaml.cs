using System.Windows;
using System.Windows.Input;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for CreateListing.xaml
	/// </summary>
	public partial class CreateListing : Window {

		#region default methods
		public CreateListing() {
			InitializeComponent();
		}

		private void Hide(object sender, RoutedEventArgs e) {
			Hide();
		}

		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
		#endregion

	}
}

using System.Windows;
using System.Windows.Input;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for Inventory.xaml
	/// </summary>
	public partial class Inventory : Window {
        private void WindowLoaded(object sender, RoutedEventArgs e) { // triggers when the window is first loaded, populates all the listviews once.

        }


		#region Boring basic window stuff
		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
		private void Hide(object sender, RoutedEventArgs e) {
			Hide();
		}
		public Inventory() {
			InitializeComponent();
		}
		#endregion
	}
}

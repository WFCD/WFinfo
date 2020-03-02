using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for Inventory.xaml
	/// </summary>
	public partial class Inventory : Window {
		public Inventory() {
			InitializeComponent();
		}

		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
		private void Hide(object sender, RoutedEventArgs e) {
			Hide();
		}
	}
}

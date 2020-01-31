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

namespace WFInfoCS {
	/// <summary>
	/// Interaction logic for RelicsWindow.xaml
	/// </summary>
	public partial class RelicsWindow : System.Windows.Window {
		public RelicsWindow() {
			InitializeComponent();
		}

		private void Exit(object sender, RoutedEventArgs e) {
			Hide();
		}

		public void populate() { //todo implement populating the listview
			Show();
		}

		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
	}
}

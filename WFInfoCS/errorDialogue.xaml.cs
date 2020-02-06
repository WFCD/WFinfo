using System;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;

namespace WFInfoCS {
	/// <summary>
	/// Interaction logic for errorDialogue.xaml
	/// </summary>
	public partial class ErrorDialogue : System.Windows.Window {

		string startPath = Main.appPath + @"\debug";
		string zipPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\WFInfoError.zip";
		public ErrorDialogue() {
			InitializeComponent();
			Show();
			Focus();
		}

		private void YesClick(object sender, RoutedEventArgs e) {
			ZipFile.CreateFromDirectory(startPath, zipPath);
			OCR.errorDetected = false;
			Close();
		}

		private void NoClick(object sender, RoutedEventArgs e) {
			OCR.errorDetected = false;
			Close();
		}

		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for WebBrowser.xaml
	/// </summary>
	public partial class Login : Window {

		public Login() {
			InitializeComponent();
		}
		private void Hide(object sender, MouseButtonEventArgs e) {
			Hide();
		}

		// Allows the draging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		private void Click(object sender, MouseButtonEventArgs e) {
			Console.WriteLine("Email: " + Email.Text + " Password: " + Password.Password.ToString());
			Main.dataBase.GetUserLogin(Email.Text, Password.Password.ToString());
		}


	}
}

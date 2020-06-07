using System;
using System.Threading.Tasks;
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
			Main.searchBox.Hide();
			Hide();
		}

		// Allows the dragging of the window
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		private async void Click(object sender, MouseButtonEventArgs e) {

			if (!Main.dataBase.IsJwtAvailable())
				await Main.dataBase.GetUserLogin(Email.Text, Password.Password);
			Close(); //dispose of window once done

			Main.searchBox.placeholder.Content = "Logged in";
			Main.searchBox.isInUse = true;
			Main.searchBox.searchField.Focusable = true;
			if(RememberMe.IsChecked.Value)
				Settings.JWT = Main.dataBase.JWT;
			Console.WriteLine(Settings.JWT);
		}

		private void Email_GotFocus(object sender, RoutedEventArgs e) {
			if (Email.Text == "Email")
				Email.Text = "";
		}


		//private void MakeListing_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		//{
		// //{"order_type":"sell","item_id":"54a73e65e779893a797fff58","platinum":30,"quantity":1}
		// Main.dataBase.ListItem("54a73e65e779893a797fff58", 30, 1);
		//}
	}
}

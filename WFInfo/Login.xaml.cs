using System;
using System.Windows;
using System.Windows.Input;
using Google.Apis.Auth;

namespace WFInfo {
	/// <summary>
	/// Interaction logic for Login.xaml
	/// </summary>
	public partial class Login : Window {
		
		#region default methods
		public Login() {
			InitializeComponent();
		}

		/// <summary>
		/// Hides the window 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void HideExternal(object sender, MouseButtonEventArgs e) {
			Main.searchBox.Hide(); //hide search it if the user minimizes the window even though not yet logged in
			Hide();
		}

		/// <summary>
		/// Allows the window to be dragged
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private new void MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}
		#endregion

		/// <summary>
		/// Attempts to log in with the filled in credentials.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void LoginClick(object sender, MouseButtonEventArgs e) {

			if (!Main.dataBase.IsJwtAvailable())
			{
				try 
				{
					await Main.dataBase.GetUserLogin(Email.Text, Password.Password);
				}
				catch (Exception ex)
				{
					Settings.settingsObj["JWT"] = null;
					Settings.Save();
					Main.AddLog("Couldn't login: " + ex);
					if (ex.Message.Contains("email")) {
						if (ex.Message.Contains("app.form.invalid")) {
							Main.StatusUpdate("Invalid email form", 2);
						} else
							Main.StatusUpdate("Unknown email", 1);
					} else if (ex.Message.Contains("password")) {
						Main.StatusUpdate("Wrong password", 1);
					} else {
						Main.StatusUpdate("Too many requests", 1); //default to too many requests
					}
					Main.signOut();
					return;
				}
				Main.loggedIn();
				Email.Text = "Email";
				Password.Password = "";
				if (RememberMe.IsChecked.Value)
				{
					Settings.settingsObj["JWT"] = Main.dataBase.JWT;
					Settings.Save();
				}
				else
				{
					Settings.settingsObj["JWT"] = null;
				}
			}
			Hide(); //dispose of window once done
			if (Main.searchBox.IsActive) {
				Main.searchBox.placeholder.Content = "Logged in";
				Main.searchBox.isInUse = true;
				Main.searchBox.searchField.Focusable = true;
			}
		}

		/// <summary>
		/// Clears the email field if it's not been used yet.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Email_GotFocus(object sender, RoutedEventArgs e) {
			if (Email.Text == "Email")
				Email.Text = "";
		}

		/// <summary>
		/// Allow the window to be spawned in an appropriate place.
		/// </summary>
		/// <param name="x">Left most border of the window</param>
		/// <param name="y">Top most border of the window</param>
		public void MoveLogin(double x, double y) {
			Left = x;
			Top = y;
			Show();
		}
	}
}

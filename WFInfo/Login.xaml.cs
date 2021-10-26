using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Google.Apis.Auth;
using Microsoft.Win32;
using System.Windows.Media;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {

        #region default methods
        public Login()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Hides the window 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideExternal(object sender, MouseButtonEventArgs e)
        {
            Main.searchBox.Hide(); //hide search it if the user minimizes the window even though not yet logged in
            Hide();
        }

        /// <summary>
        /// Allows the window to be dragged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        #endregion

        /// <summary>
        /// Attempts to log in with the filled in credentials.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoginClick(object sender, MouseButtonEventArgs e)
        {

            if (!Main.dataBase.IsJwtAvailable())
            {
                try
                {
                    await Main.dataBase.GetUserLogin(Email.Text, Password.Password);
                }
                catch (Exception ex)
                {
                    Main.dataBase.JWT = null;
                    Settings.Save();
                    Main.AddLog("Couldn't login: " + ex);
                    string StatusMessage; //StatusMessage = text to display on StatusUpdate() AND the error box under login 
                    byte StatusSeverity; //StatusSeverity = Severity for StatusUpdate()
                    if (ex.Message.Contains("email"))
                    {
                        if (ex.Message.Contains("app.form.invalid"))
                        {
                            StatusMessage = "Invalid email form";
                            StatusSeverity = 2;

                        }
                        else
                        {
                            StatusMessage = "Unknown email";
                            StatusSeverity = 1;
                        }
                    }
                    else if (ex.Message.Contains("password"))
                    {
                        StatusMessage = "Wrong password";
                        StatusSeverity = 1;
                    }
                    else if (ex.Message.Contains("could not understand"))
                    {
                        StatusMessage = "Severe issue, server did not understand request";
                        StatusSeverity = 1;
                    }
                    else
                    {
                        StatusMessage = "Too many requests";
                        StatusSeverity = 1; //default to too many requests
                    }
                    WeakReferenceMessenger.Default.Send<SignOutMessage>();
                    Main.StatusUpdate(StatusMessage, StatusSeverity); //Changing WFinfo status

                    switch (StatusSeverity)
                    { // copy/paste from Main.cs (statusChange())
                        case 1: //severe, red text
                            Error.Foreground = Brushes.Red;
                            break;
                        case 2: //warning, orange text
                            Error.Foreground = Brushes.Orange;
                            break;
                        default: //Uncaught, big problem
                            Error.Foreground = Brushes.Yellow;
                            break;
                    }
                    Error.Text = StatusMessage; //Displaying the error under the text fields
                    if(Error.Visibility != Visibility.Visible)
                    {
                        Height += 20;
                    }
                    Error.Visibility = Visibility.Visible;
                    return;
                }
                WeakReferenceMessenger.Default.Send<LoginMessage>();
                Email.Text = "Email";
                Password.Password = "";
                Main.dataBase.rememberMe = RememberMe.IsChecked.Value;
            }
            Hide(); //dispose of window once done
            if (Main.searchBox.IsActive)
            {
                Main.searchBox.placeholder.Content = "Logged in";
                Main.searchBox.IsInUse = true;
                Main.searchBox.searchField.Focusable = true;
            }
        }

        /// <summary>
        /// Clears the email field if it's not been used yet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Email_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Email.Text == "Email")
                Email.Text = "";
        }

        /// <summary>
        /// Allow the window to be spawned in an appropriate place.
        /// </summary>
        /// <param name="x">Left most border of the window</param>
        /// <param name="y">Top most border of the window</param>
        public void MoveLogin(double x, double y)
        {
            Left = x;
            Top = y;
            Show();
        }
    }
}

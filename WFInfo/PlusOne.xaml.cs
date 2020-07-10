using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace WFInfo.Resources
{
    /// <summary>
    /// Interaction logic for PlusOne.xaml
    /// </summary>
    public partial class PlusOne : Window
    {
        int counter;
        public PlusOne()
        {
            InitializeComponent();
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WFinfo");
            if (key.GetValue("review") != null)
                Processed();
            key.Close();
        }
        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Allows the draging of the window
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void TextboxGotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox.Text.Contains("Optional comment field") && TextBox.IsLoaded)
                TextBox.Text = "";
        }

        private void post(object sender, RoutedEventArgs e)
        {
            var message = TextBox.Text == "Optional comment field" ? "" : TextBox.Text;
            try
            {
                var t = Task.Run(async () =>
                {
                    await Main.dataBase.PostReview(message);
                });
                t.Wait();
            }
            catch (System.Exception)
            {

                throw;
            }
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WFinfo");
            key.SetValue("review", true);
            key.Close();
            Processed();
        }

        private void Processed()
        {
            TextBox.Text = "Review submited, thank you";
            TextBox.IsEnabled = false;
            postReview.Content = "Thank you!";
            postReview.IsEnabled = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBox.Text.Contains("Give me sliders") || TextBox.Text.Contains("more sliders"))
            {
                Height += 58;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (counter == 9) return;
            Height += 22;
            counter ++;
        }
    }
}

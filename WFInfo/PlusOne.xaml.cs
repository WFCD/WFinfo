using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WFInfo.Resources
{
    /// <summary>
    /// Interaction logic for PlusOne.xaml
    /// </summary>
    public partial class PlusOne : Window
    {
        public PlusOne()
        {
            InitializeComponent();
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBox.Text.Contains("Optional comment field"))
                TextBox.Text = "";
        }

        private void PreviousScreen(object sender, RoutedEventArgs e)
        {
            var message = TextBox.Text == "Optional comment field" ? "" : TextBox.Text;
            var developers = new List<string> { "dimon222", "Dapal003", "Kekasi" };
            Main.dataBase.postReview(developers, message);
        }
    }
}

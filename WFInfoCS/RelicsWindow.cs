using System;
using System.Windows;
using System.Windows.Input;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for RelicsWindow.xaml
    /// </summary>
    public partial class RelicsWindow : System.Windows.Window
    {
        public RelicsWindow()
        {
            InitializeComponent();
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void populate()
        { //todo implement populating the listview
            Show();
            Focus();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void VaultedClick(object sender, RoutedEventArgs e)
        {
            if ((bool)vaulted.IsChecked)
            {
                Console.WriteLine("Vaulted is checked");
                //todo logic if it is checked
            }
        }

        private void TextboxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //todo update seach from what the user types
            Console.WriteLine(textBox.Text);

        }

        private void ComboboxMouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine(comboBox.Text); // compare this to the known results
        }

        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            textBox.Clear();
        }

        private void ComboButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Combo button was clicked");
            //todo toggles between showing all relics in a single treeview vs showing them nested per age 
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Add was clicked");

            //todo add 1 owned to current selected treeview item
        }

        private void Subtract(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Subtract was clicked");

            //todo remove 1 owned of the current selected tree view item
        }

    }
}

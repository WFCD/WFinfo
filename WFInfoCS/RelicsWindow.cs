using System.Windows;
using System.Windows.Input;

namespace WFInfoCS {
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

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {

        }
    }
}

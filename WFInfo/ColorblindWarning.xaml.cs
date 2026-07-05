using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for ColorblindWarning.xaml
    /// </summary>
    public partial class ColorblindWarning : Window
    {
        public ColorblindWarning()
        {
            InitializeComponent();
            Show();
            Focus();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Process.Start(e.Uri.ToString());
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Dismiss(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Allows the dragging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}

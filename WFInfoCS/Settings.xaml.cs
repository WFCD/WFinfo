using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WFInfoCS
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            Activation_key_box.Text = "Print screen"; //toodo, set to current saved setting
            Scaling_box.Text = "100%"; //todo, set to current saved setting
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // todo implement saving to settings file
            // 1 = overlay
            // 0 = window

            Console.WriteLine(e.NewValue);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //todo allow only a single input to register as new activation key
        }



        private void Window_checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window checked");            //todo changes to window setting and save to file. Dummy option for aplha
        }

        private void Overlay_checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Overlay checked");            //todo changes to overlay setting and save to file. Dummy option for aplha
        }

        private void Debug_Clicked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Debug clicked" + e.GetType());            //todo toggle debug and save to file. Dummy option for aplha
        }
        private void Auto_Clicked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Debug clicked" + e.ToString());            //todo toggle debug and save to file. Dummy option for aplha
        }
    }
}

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for errorDialogue.xaml
    /// </summary>
    public partial class InitialDialogue : System.Windows.Window
    {
        public InitialDialogue()
        {
            InitializeComponent();
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}

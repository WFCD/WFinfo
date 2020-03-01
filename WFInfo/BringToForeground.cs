using System;
using System.Windows;
using System.Windows.Input;

namespace WFInfo
{
    /// <summary>
    /// A simple command that displays the command parameter as
    /// a dialog message.
    /// </summary>

    public class BringToForeground : ICommand
    {
        public void Execute(object parameter)
        {
            Console.WriteLine("Test");
            MainWindow.INSTANCE.Visibility = Visibility.Visible;
            MainWindow.INSTANCE.Activate();
            MainWindow.INSTANCE.Topmost = true;  // important
            MainWindow.INSTANCE.Topmost = false; // important
            MainWindow.INSTANCE.Focus();         // important
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}

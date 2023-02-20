using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for AutoCount.xaml
    /// </summary>
    public partial class AutoCount : Window
    {

        //private readonly Settings.SettingsViewModel _viewModel;
        //public Settings.SettingsViewModel SettingsViewModel => _viewModel;

        public static AutoCount INSTANCE;
        public AutoAddViewModel viewModel { get; }
        public SimpleCommand IncrementAll { get; }
        public SimpleCommand RemoveAll { get; }

        public AutoCount()
        {
            INSTANCE = this;
            viewModel = new AutoAddViewModel();

            RemoveAll = new SimpleCommand(() => RemoveFromParentAll());
            IncrementAll = new SimpleCommand(() => AddCountAll());

            for (int i = 0; i < 30; i++)
            {
                List<string> tmp = new List<string>();
                tmp.Add("Ivara Prime Blueprint");
                tmp.Add("Braton Prime Blueprint");
                tmp.Add("Paris Prime Upper Limb");
                AutoAddSingleItem tmpItem = new AutoAddSingleItem(tmp, i % 5, viewModel);
                viewModel.addItem(tmpItem);
            }
            InitializeComponent();
        }
        public static void ShowAutoCount()
        {
            if (INSTANCE != null)
            {
                INSTANCE.Show();
                INSTANCE.Focus();
            }
        }

        private void Hide(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void AddCountAll()
        {
            foreach (AutoAddSingleItem item in viewModel.ItemList)
            {
                if (item._parent != viewModel)
                {
                    item._parent = viewModel;
                }
            }

            while (viewModel.ItemList.Count > 0)
            {
                viewModel.ItemList.FirstOrDefault().AddCount(false);
            }
            Main.dataBase.SaveAllJSONs();
            EquipmentWindow.INSTANCE.reloadItems();
        }

        private void RemoveFromParentAll()
        {
            foreach (AutoAddSingleItem item in viewModel.ItemList)
            {
                if (item._parent != viewModel)
                {
                    item._parent = viewModel;
                }
            }

            while (viewModel.ItemList.Count > 0)
            {
                viewModel.ItemList.FirstOrDefault().Remove.Execute(null);
            }
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void RedirectScrollToParent(object sender, MouseWheelEventArgs e)
        {
            object tmp = VisualTreeHelper.GetParent(sender as DependencyObject);
            if (tmp is ScrollContentPresenter)
            {
                ScrollContentPresenter SCP = tmp as ScrollContentPresenter;
                MouseWheelEventArgs eventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArgs.RoutedEvent = MouseWheelEvent;
                eventArgs.Source = sender;
                SCP.RaiseEvent(eventArgs);
            }
        }
    }
}

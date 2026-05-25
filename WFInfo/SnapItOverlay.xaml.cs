using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WFInfo.Services.WindowInfo;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for SnapItOverlay.xaml
    /// Marching ant logic by: https://www.codeproject.com/Articles/27816/Marching-Ants-Selection
    /// </summary>
    public partial class SnapItOverlay : Window
    {
        public bool isEnabled;
        public Bitmap tempImage;
        private System.Windows.Point startDrag;
        private System.Drawing.Point topLeft;

        private readonly IWindowInfoService _window;

        public SnapItOverlay(IWindowInfoService window)
        {
            _window = window;
            WindowStartupLocation = WindowStartupLocation.Manual;

            Left = 0;
            Top = 0;
            InitializeComponent();
            MouseDown += new MouseButtonEventHandler(canvas_MouseDown);
            MouseUp += new MouseButtonEventHandler(canvas_MouseUp);
            MouseMove += new MouseEventHandler(canvas_MouseMove);

        }

        public void Populate(Bitmap screenshot)
        {
            ResetRectangle();
            tempImage = screenshot;
            isEnabled = true;
        }

        private void ResetRectangle()
        {
            rectangleWhite.Width = 0;
            rectangleWhite.Height = 0;
            rectangleWhite.RenderTransform = new TranslateTransform(0, 0);
            rectangleWhite.Visibility = Visibility.Hidden;
            rectangleBlack.Width = 0;
            rectangleBlack.Height = 0;
            rectangleBlack.RenderTransform = new TranslateTransform(0, 0);
            rectangleBlack.Visibility = Visibility.Hidden;
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startDrag = e.GetPosition(canvas);
            rectangleWhite.Visibility = Visibility.Visible;
            rectangleBlack.Visibility = Visibility.Visible;
            Canvas.SetZIndex(rectangleWhite, canvas.Children.Count);
            Canvas.SetZIndex(rectangleBlack, canvas.Children.Count - 1);
            if (!canvas.IsMouseCaptured)
                canvas.CaptureMouse();
            canvas.Cursor = Cursors.Cross;
        }

        public void closeOverlay()
        {
            ResetRectangle();
            Topmost = false;
            isEnabled = false;

            // Force immediate hide without delay to prevent rectangle persistence
            Hide();
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (canvas.IsMouseCaptured)
                canvas.ReleaseMouseCapture();
            canvas.Cursor = Cursors.Arrow;
            Main.AddLog("User drew rectangle: Starting point: " + startDrag.ToString() + " Width: " + rectangleWhite.Width + " Height:" + rectangleWhite.Height);
            if (rectangleWhite.Width < 10 || rectangleWhite.Height < 10)
            {
                Main.AddLog("User selected an area too small");
                Main.StatusUpdate("Please slecet a larger area to scan", 2);
                return;
            }
            Bitmap cutout = tempImage.Clone(new Rectangle((int)(topLeft.X * _window.DpiScaling), (int)(topLeft.Y * _window.DpiScaling), (int)(rectangleWhite.Width * _window.DpiScaling), (int)(rectangleWhite.Height * _window.DpiScaling)), System.Drawing.Imaging.PixelFormat.DontCare);
            Task.Run(() => OCR.ProcessSnapIt(cutout, tempImage, topLeft));

            closeOverlay();
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                System.Windows.Point currentPoint = e.GetPosition(canvas);

                double x = startDrag.X < currentPoint.X ? startDrag.X : currentPoint.X;
                double y = startDrag.Y < currentPoint.Y ? startDrag.Y : currentPoint.Y;

                if (rectangleWhite.Visibility == Visibility.Hidden)
                {
                    rectangleWhite.Visibility = Visibility.Visible;
                    rectangleBlack.Visibility = Visibility.Visible;
                }

                topLeft = new System.Drawing.Point((int)x, (int)y);
                rectangleWhite.RenderTransform = new TranslateTransform(x, y);
                rectangleBlack.RenderTransform = new TranslateTransform(x, y);
                rectangleWhite.Width = Math.Abs(e.GetPosition(canvas).X - startDrag.X);
                rectangleWhite.Height = Math.Abs(e.GetPosition(canvas).Y - startDrag.Y);
                rectangleBlack.Width = Math.Abs(e.GetPosition(canvas).X - startDrag.X);
                rectangleBlack.Height = Math.Abs(e.GetPosition(canvas).Y - startDrag.Y);
            }
        }
    }
}

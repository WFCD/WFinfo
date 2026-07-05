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
            SetRectangles(0, 0, new TranslateTransform(0, 0), Visibility.Hidden);
        }

        private void SetRectangles(double width, double height, TranslateTransform transform, Visibility visibility)
        {
            rectangleWhite.Width = width;
            rectangleWhite.Height = height;
            rectangleWhite.RenderTransform = transform;
            rectangleWhite.Visibility = visibility;
            rectangleBlack.Width = width;
            rectangleBlack.Height = height;
            rectangleBlack.RenderTransform = transform;
            rectangleBlack.Visibility = visibility;
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startDrag = e.GetPosition(canvas);
            SetRectangles(0, 0, new TranslateTransform(0, 0), Visibility.Visible);
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
                Main.StatusUpdate("Please select a larger area to scan", 2);
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

                topLeft = new System.Drawing.Point((int)x, (int)y);
                double w = Math.Abs(currentPoint.X - startDrag.X);
                double h = Math.Abs(currentPoint.Y - startDrag.Y);
                SetRectangles(w, h, new TranslateTransform(x, y), Visibility.Visible);
            }
        }
    }
}

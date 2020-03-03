using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WFInfo {
    /// <summary>
    /// Interaction logic for SnapItOverlay.xaml
    /// Marching ant logic by: https://www.codeproject.com/Articles/27816/Marching-Ants-Selection
    /// </summary>
    public partial class SnapItOverlay : Window {

        private Bitmap tempImage;
        private System.Windows.Point startDrag;
        public SnapItOverlay() {
			InitializeComponent();
			canvas.MouseDown += new MouseButtonEventHandler(canvas_MouseDown);
			canvas.MouseUp += new MouseButtonEventHandler(canvas_MouseUp);
			canvas.MouseMove += new MouseEventHandler(canvas_MouseMove);
		}

		public SnapItOverlay(Bitmap screenshot) {
			InitializeComponent();
            tempImage = screenshot;
			Image.Source = Win32.ImageSourceFromBitmap(screenshot);
			canvas.MouseDown += new MouseButtonEventHandler(canvas_MouseDown);
			canvas.MouseUp += new MouseButtonEventHandler(canvas_MouseUp);
			canvas.MouseMove += new MouseEventHandler(canvas_MouseMove);
		}
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e) {
            //Set the start point
            startDrag = e.GetPosition(canvas);
            //Move the selection marquee on top of all other objects in canvas
            Canvas.SetZIndex(rectangle, canvas.Children.Count);
            //Capture the mouse
            if (!canvas.IsMouseCaptured)
                canvas.CaptureMouse();
            canvas.Cursor = Cursors.Cross;
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e) {
            //Release the mouse
            if (canvas.IsMouseCaptured)
                canvas.ReleaseMouseCapture();
            canvas.Cursor = Cursors.Arrow;
            Main.AddLog("User drew rectangle: Starting point: " + startDrag.ToString() + " Width: " + rectangle.Width + " Height:" + rectangle.Height );
            tempImage = tempImage.Clone(new Rectangle((int) startDrag.X, (int)startDrag.Y, (int)rectangle.Width, (int)rectangle.Height), System.Drawing.Imaging.PixelFormat.DontCare);
            OCR.ProcessSnapIt(tempImage);
            Close();
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e) {
            if (canvas.IsMouseCaptured) {
                System.Windows.Point currentPoint = e.GetPosition(canvas);

                //Calculate the top left corner of the rectangle 
                //regardless of drag direction
                double x = startDrag.X < currentPoint.X ? startDrag.X : currentPoint.X;
                double y = startDrag.Y < currentPoint.Y ? startDrag.Y : currentPoint.Y;

                if (rectangle.Visibility == Visibility.Hidden)
                    rectangle.Visibility = Visibility.Visible;

                //Move the rectangle to proper place
                rectangle.RenderTransform = new TranslateTransform(x, y);
                //Set its size
                rectangle.Width = Math.Abs(e.GetPosition(canvas).X - startDrag.X);
                rectangle.Height = Math.Abs(e.GetPosition(canvas).Y - startDrag.Y);
            }
        }
    }
}

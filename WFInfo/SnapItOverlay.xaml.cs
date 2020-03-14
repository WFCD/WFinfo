using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        public SnapItOverlay()
        {
            InitializeComponent();
            MouseDown += new MouseButtonEventHandler(canvas_MouseDown);
            MouseUp += new MouseButtonEventHandler(canvas_MouseUp);
            MouseMove += new MouseEventHandler(canvas_MouseMove);
        }

        public void Populate(Bitmap screenshot)
        {
            tempImage = screenshot;
            isEnabled = true;
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Set the start point
            startDrag = e.GetPosition(canvas);
            //Move the selection marquee on top of all other objects in canvas
            Canvas.SetZIndex(rectangle, canvas.Children.Count);
            //Capture the mouse
            if (!canvas.IsMouseCaptured)
                canvas.CaptureMouse();
            canvas.Cursor = Cursors.Cross;
        }

        public void closeOverlay()
        {
            rectangle.Width = 0;
            rectangle.Height = 0;
            rectangle.RenderTransform = new TranslateTransform(0, 0);
            Topmost = false;
            isEnabled = false;

            // THIS FUCKING RECTANGLE WOULDN'T GO AWAY 
            //    AND IT WOULD STAY FOR 1 FRAME WHEN RE-OPENNING THIS WINDOW
            //    SO I FORCED THAT FRAME TO HAPPEN BEFORE CLOSING
            //       AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHHHHHHHHHHH
            //
            //  fucking hate rectangles
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(100);
                Dispatcher.Invoke(Hide);
            });
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //Release the mouse
            if (canvas.IsMouseCaptured)
                canvas.ReleaseMouseCapture();
            canvas.Cursor = Cursors.Arrow;
            Main.AddLog("User drew rectangle: Starting point: " + startDrag.ToString() + " Width: " + rectangle.Width + " Height:" + rectangle.Height);
            if (rectangle.Width < 10 || rectangle.Height < 10)
            { // box is smaller than 10x10 and thus will never be able to have any text. Also used as a failsave to prevent the program from crashing if the user makes a 0x0 sleection
                Main.AddLog("User selected an area too small");
                Main.StatusUpdate("Please slecet a larger area to scan", 2);
                return;
            }

            Bitmap cutout = tempImage.Clone(new Rectangle((int)(topLeft.X * OCR.dpiScaling), (int)(topLeft.Y * OCR.dpiScaling), (int)(rectangle.Width * OCR.dpiScaling), (int)(rectangle.Height * OCR.dpiScaling)), System.Drawing.Imaging.PixelFormat.DontCare);
            int xPos = topLeft.X + (int)rectangle.Width / 2 * (int)(OCR.dpiScaling);
            int yPos = topLeft.Y + 10 * (int)(OCR.dpiScaling);
            Task.Factory.StartNew(() => OCR.ProcessSnapIt(cutout,tempImage,xPos,yPos));

            closeOverlay();
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                System.Windows.Point currentPoint = e.GetPosition(canvas);

                //Calculate the top left corner of the rectangle 
                //regardless of drag direction
                double x = startDrag.X < currentPoint.X ? startDrag.X : currentPoint.X;
                double y = startDrag.Y < currentPoint.Y ? startDrag.Y : currentPoint.Y;

                if (rectangle.Visibility == Visibility.Hidden)
                    rectangle.Visibility = Visibility.Visible;

                //Move the rectangle to proper place
                topLeft = new System.Drawing.Point((int)x, (int)y);
                rectangle.RenderTransform = new TranslateTransform(x, y);
                //Set its size
                rectangle.Width = Math.Abs(e.GetPosition(canvas).X - startDrag.X);
                rectangle.Height = Math.Abs(e.GetPosition(canvas).Y - startDrag.Y);
            }
        }
    }
}

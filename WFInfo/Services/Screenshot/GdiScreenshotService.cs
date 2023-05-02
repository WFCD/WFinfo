using SharpDX.DXGI;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WFInfo.Services.WarframeProcess;
using WFInfo.Services.WindowInfo;
using WFInfo.Settings;

namespace WFInfo.Services.Screenshot
{
    public class GdiScreenshotService : IScreenshotService
    {
        private static IReadOnlyApplicationSettings _settings;
        private static IProcessFinder _process;
        private static IWindowInfoService _window;

        public GdiScreenshotService(IProcessFinder process, IWindowInfoService window, IReadOnlyApplicationSettings settings) 
        {
            _process = process;
            _window = window;
            _settings = settings;
        }

        public Task<List<Bitmap>> CaptureScreenshot()
        {
            var window = _window.Window;
            var center = _window.Center;

            int width = window.Width;
            int height = window.Height;

            if (window == null || window.Width == 0 || window.Height == 0)
            {
                window = _window.Screen.Bounds;
                center = new Point(window.X + window.Width / 2, window.Y + window.Height / 2);

                width *= (int)_window.DpiScaling;
                height *= (int)_window.DpiScaling;
            }

            Bitmap image = new Bitmap(width, height);
            Size FullscreenSize = new Size(image.Width, image.Height);

            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.CopyFromScreen(window.Left, window.Top, 0, 0, FullscreenSize, CopyPixelOperation.SourceCopy);
            }

            var result = new List<Bitmap> { image };
            return Task.FromResult(result);
        }
    }
}

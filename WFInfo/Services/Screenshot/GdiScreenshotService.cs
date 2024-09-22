using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using WFInfo.Services.WindowInfo;
using WFInfo.Settings;

namespace WFInfo.Services.Screenshot
{
    public class GdiScreenshotService : IScreenshotService
    {
        private static IWindowInfoService _window;
        private static IReadOnlyApplicationSettings _settings;

        public GdiScreenshotService(IWindowInfoService window, IReadOnlyApplicationSettings settings) 
        {
            _window = window;
            _settings = settings;
        }

        public bool IsAvailable => true;

        public Task<List<Bitmap>> CaptureScreenshot()
        {
            _window.UpdateWindow();

            var window = _window.Window;
            var center = _window.Center;

            int width = window.Width;
            int height = window.Height;

            if (width == 0 || height == 0)
            {
                if (!_settings.Debug) 
                {
                    return Task.FromResult(new List<Bitmap>());
                }

                window = _window.Screen.Bounds;
                width = window.Width;
                height = window.Height;
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

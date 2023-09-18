using System.Drawing;
using System.Windows.Forms;

namespace WFInfo.Services.WindowInfo
{
    /// <summary>
    /// Provides information about the game window and screen it's located on.
    /// </summary>
    public interface IWindowInfoService
    {
        /// <summary>
        /// Gets the DPI - Only used to display on screen or to get the "actual" screen bounds.
        /// </summary>
        double DpiScaling { get; }

        /// <summary>
        /// Gets the Screen / Resolution Scaling - Used to adjust pixel values to each person's monitor.
        /// </summary>
        double ScreenScaling { get; }

        /// <summary>
        /// Gets the game window rectangle excluding any borders.
        /// </summary>
        Rectangle Window { get; }

        /// <summary>
        /// Gets the center of the game window.
        /// </summary>
        Point Center { get; }

        /// <summary>
        /// Gets the screen the game is currently located on.
        /// </summary>
        Screen Screen { get; }

        /// <summary>
        /// Updates all cached info about the window.
        /// </summary>
        void UpdateWindow();

        /// <summary>
        /// Uses a bitmap to set window info. Any further <see cref="UpdateWindow"/> calls will overwrite this info.
        /// </summary>
        /// <param name="bitmap">Source image</param>
        void UseImage(Bitmap bitmap);
    }
}

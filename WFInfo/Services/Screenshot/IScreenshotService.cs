using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace WFInfo.Services.Screenshot
{
    public enum HdrSupportEnum
    {
        Auto,
        On,
        Off
    }

    /// <summary>
    /// Provides game screenshots.
    /// </summary>
    public interface IScreenshotService
    {
        /// <summary>
        /// Captures one or more screenshots of the game. All screenshots are in SDR.
        /// </summary>
        /// <returns>Captured screenshots</returns>
        Task<List<Bitmap>> CaptureScreenshot();
    }
}

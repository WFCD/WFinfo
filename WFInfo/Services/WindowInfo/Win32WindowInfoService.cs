using System;
using System.Drawing;
using System.Windows.Forms;
using WFInfo.Services.WarframeProcess;
using WFInfo.Settings;

namespace WFInfo.Services.WindowInfo
{
    public class Win32WindowInfoService : IWindowInfoService
    {
        public double DpiScaling { get; private set; }
        public double ScreenScaling { get; private set; }

        public Rectangle Window { get; private set; }
        public Point Center => new Point(Window.X + Window.Width / 2, Window.Y + Window.Height / 2);

        public Screen Screen { get; private set; } = Screen.PrimaryScreen;

        private readonly IProcessFinder _process;
        private readonly IReadOnlyApplicationSettings _settings;

        public Win32WindowInfoService(IProcessFinder process, IReadOnlyApplicationSettings settings)
        {
            _process = process;
            _settings = settings;
        }

        public void UpdateWindow()
        {
            if (!_process.IsRunning && !_settings.Debug)
            {
                Main.AddLog("Failed to find warframe process for window info");
                Main.StatusUpdate("Failed to find warframe process for window info", 1);
                return;
            }

            Screen = Screen.FromHandle(_process.HandleRef.Handle);
            string screenType = Screen.Primary ? "primary" : "secondary";

            if (_process.GameIsStreamed) Main.AddLog($"GFN -- Warframe display: {Screen.DeviceName}, {screenType}");
            else Main.AddLog($"Warframe display: {Screen.DeviceName}, {screenType}");

            RefreshDPIScaling();
            GetWindowRect();
        }

        public void UseImage(Bitmap image)
        {
            int width = image?.Width ?? Screen.Bounds.Width;
            int height = image?.Height ?? Screen.Bounds.Height;
            Window = new Rectangle(0, 0, width, height);
            if (image != null)
                Main.AddLog("DETECTED LOADED IMAGE BOUNDS: " + Window.ToString());
            else
                Main.AddLog("Couldn't Detect Warframe Process. Using Primary Screen Bounds: " + Window.ToString() + " Named: " + Screen.DeviceName);

            RefreshScaling();
        }

        private void GetWindowRect()
        {
            if (!Win32.GetWindowRect(_process.HandleRef, out Win32.R osRect))
            {
                if (_settings.Debug)
                { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
                    GetFullscreenRect();
                    RefreshScaling();
                    return;
                }
                else
                {
                    Main.AddLog("Failed to get window bounds");
                    Main.StatusUpdate("Failed to get window bounds", 1);
                    return;
                }
            }

            if (osRect.Left < -20000 || osRect.Top < -20000)
            { // if the window is in the VOID delete current process and re-set window to nothing
                Window = Rectangle.Empty;
            }
            else if (Window == null || Window.Left != osRect.Left || Window.Right != osRect.Right || Window.Top != osRect.Top || Window.Bottom != osRect.Bottom)
            { // checks if old window size is the right size if not change it
                Window = new Rectangle(osRect.Left, osRect.Top, osRect.Right - osRect.Left, osRect.Bottom - osRect.Top); // get Rectangle out of rect
                                                                                                                         // Rectangle is (x, y, width, height) RECT is (x, y, x+width, y+height) 
                int GWL_style = -16;
                uint WS_BORDER = 0x00800000;
                uint WS_POPUP = 0x80000000;

                uint styles = Win32.GetWindowLongPtr(_process.HandleRef, GWL_style);
                if ((styles & WS_POPUP) != 0)
                {
                    // Borderless, don't do anything
                    Main.AddLog($"Borderless detected (0x{styles.ToString("X8", Main.culture)}, {Window.ToString()}");
                }
                else if ((styles & WS_BORDER) != 0)
                {
                    // Windowed, adjust for thicc border
                    Window = new Rectangle(Window.Left + 8, Window.Top + 30, Window.Width - 16, Window.Height - 38);
                    Main.AddLog($"Windowed detected (0x{styles.ToString("X8", Main.culture)}, adjusting window to: {Window.ToString()}");
                }
                else
                {
                    // Assume Fullscreen, don't do anything
                    Main.AddLog($"Fullscreen detected (0x{styles.ToString("X8", Main.culture)}, {Window.ToString()}");
                    //Show the Fullscreen prompt
                    if (_settings.IsOverlaySelected)
                    {
                        Main.AddLog($"Showing the Fullscreen Reminder");
                        Main.RunOnUIThread(() =>
                        {
                            Main.SpawnFullscreenReminder();
                        });
                    }
                }

                RefreshScaling();
            }
        }

        private void GetFullscreenRect()
        {
            int width = Screen.Bounds.Width;
            int height = Screen.Bounds.Height;
            Window = new Rectangle(0, 0, width, height);
            Main.AddLog("Couldn't Detect Warframe Process. Using Primary Screen Bounds: " + Window.ToString() + " Named: " + Screen.DeviceName);

            RefreshScaling();
        }

        private void RefreshScaling()
        {
            if (Window.Width * 9 > Window.Height * 16)  // image is less than 16:9 aspect
                ScreenScaling = Window.Height / 1080.0;
            else
                ScreenScaling = Window.Width / 1920.0; //image is higher than 16:9 aspect

            // TODO: UI Scaling?
            //Main.AddLog("SCALING VALUES UPDATED: Screen_Scaling = " + (ScreenScaling * 100).ToString("F2", Main.culture) + "%, DPI_Scaling = " + (DpiScaling * 100).ToString("F2", Main.culture) + "%, UI_Scaling = " + (UiScaling * 100).ToString("F0", Main.culture) + "%");
            Main.AddLog("SCALING VALUES UPDATED: Screen_Scaling = " + (ScreenScaling * 100).ToString("F2", Main.culture) + "%, DPI_Scaling = " + (DpiScaling * 100).ToString("F2", Main.culture) + "%");
        }

        private void RefreshDPIScaling()
        {
            try
            {
                var mon = Win32.MonitorFromPoint(new Point(Screen.Bounds.Left + 1, Screen.Bounds.Top + 1), 2);
                Win32.GetDpiForMonitor(mon, Win32.DpiType.Effective, out var dpiXEffective, out _);

                Main.AddLog($"Effective dpi, X:{dpiXEffective}\n Which is %: {dpiXEffective / 96.0}");
                //Main.AddLog($"Raw dpi, X:{dpiXRaw}\n Which is %: {dpiXRaw / 96.0}");
                //Main.AddLog($"Angular dpi, X:{dpiXAngular}\n Which is %: {dpiXAngular / 96.0}");
                DpiScaling = dpiXEffective / 96.0; // assuming that y and x axis dpi scaling will be uniform. So only need to check one value
            }
            catch (Exception e)
            {
                Main.AddLog($"Was unable to set a new dpi scaling, defaulting to 100% zoom, exception: {e}");
                DpiScaling = 1;
            }
        }
    }
}
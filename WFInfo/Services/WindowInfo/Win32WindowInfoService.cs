using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WFInfo.Services.WarframeProcess;
using WFInfo.Settings;

namespace WFInfo.Services.WindowInfo
{
    public class Win32WindowInfoService : IWindowInfoService
    {
        public double DpiScaling { get; private set; } = 1;
        public double ScreenScaling 
        { 
            get
            {
                if (Window.Width * 9 > Window.Height * 16)  // image is less than 16:9 aspect
                    return Window.Height / 1080.0;
                else
                    return Window.Width / 1920.0; //image is higher than 16:9 aspect
            }
        }

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
            else if (_process.IsRunning)
            {
                GetWindowRect();
            }
            else if (_settings.Debug)
            {
                GetFullscreenRect();
            }

            RefreshDPIScaling();
        }

        public void UseImage(Bitmap image)
        {
            int width = image?.Width ?? Screen.Bounds.Width;
            int height = image?.Height ?? Screen.Bounds.Height;

            Window = new Rectangle(0, 0, width, height);
            DpiScaling = 1;

            if (image != null)
                Main.AddLog("DETECTED LOADED IMAGE BOUNDS: " + Window.ToString());
            else
                Main.AddLog("Couldn't Detect Warframe Process. Using Primary Screen Bounds: " + Window.ToString() + " Named: " + Screen.DeviceName);
        }


        private void GetWindowRect()
        {
            if (!Win32.GetWindowRect(_process.HandleRef, out Win32.R windowRect))
            {
                if (_settings.Debug)
                {
                    GetFullscreenRect();
                    return;
                }
                else
                {
                    Main.AddLog("Failed to get window bounds");
                    Main.StatusUpdate("Failed to get window bounds", 1);
                    return;
                }
            }

            if (windowRect.Left < -20000 || windowRect.Top < -20000)
            {
                Window = Rectangle.Empty;
            }
            else if (Window.IsEmpty || Window.Left != windowRect.Left || Window.Right != windowRect.Right || Window.Top != windowRect.Top || Window.Bottom != windowRect.Bottom)
            {
                // Get client rect in client coordinates (0,0 based)
                Win32.R clientRect;
                var gotClient = Win32.GetClientRect(_process.HandleRef, out clientRect);

                // Convert client area top-left to screen coordinates
                Win32.Point clientScreenPos = new Win32.Point { X = 0, Y = 0 };
                var gotScreen = Win32.ClientToScreen(_process.HandleRef, ref clientScreenPos);

                if (!gotClient || !gotScreen)
                {
                    Main.AddLog($"GetClientRect/ClientToScreen failed; using window rect (GetLastError={Marshal.GetLastWin32Error()})");
                    clientRect = new Win32.R { Left = 0, Top = 0, Right = windowRect.Right - windowRect.Left, Bottom = windowRect.Bottom - windowRect.Top };
                    clientScreenPos = new Win32.Point { X = windowRect.Left, Y = windowRect.Top };
                }

                int GWL_style = -16;
                uint WS_BORDER = 0x00800000;
                uint WS_POPUP = 0x80000000;

                uint styles = Win32.GetWindowLongPtr(_process.HandleRef, GWL_style);
                if ((styles & WS_POPUP) != 0)
                {
                    // Borderless - use window rect as is
                    Window = new Rectangle(windowRect.Left, windowRect.Top,
                                          windowRect.Right - windowRect.Left,
                                          windowRect.Bottom - windowRect.Top);
                    Main.AddLog($"Borderless detected (0x{styles.ToString("X8", Main.culture)}, {Window.ToString()}");
                }
                else if ((styles & WS_BORDER) != 0)
                {
                    // Windowed - use actual client area screen coordinates
                    Window = new Rectangle(clientScreenPos.X, clientScreenPos.Y,
                                          clientRect.Right - clientRect.Left,
                                          clientRect.Bottom - clientRect.Top);
                    Main.AddLog($"Windowed detected - using client area at ({clientScreenPos.X}, {clientScreenPos.Y})");
                }
                else
                {
                    // Fullscreen - use window rect
                    Window = new Rectangle(windowRect.Left, windowRect.Top,
                                          windowRect.Right - windowRect.Left,
                                          windowRect.Bottom - windowRect.Top);
                    Main.AddLog($"Fullscreen detected (0x{styles.ToString("X8", Main.culture)}, {Window.ToString()}");

                    if (_settings.IsOverlaySelected)
                    {
                        Main.AddLog($"Showing the Fullscreen Reminder");
                        Main.RunOnUIThread(() =>
                        {
                            Main.SpawnFullscreenReminder();
                        });
                    }
                }
            }
        }

        private void GetFullscreenRect()
        {
            int width = Screen.Bounds.Width;
            int height = Screen.Bounds.Height;

            Window = new Rectangle(0, 0, width, height);
            DpiScaling = 1;

            Main.AddLog("Couldn't Detect Warframe Process. Using Primary Screen Bounds: " + Window.ToString() + " Named: " + Screen.DeviceName);
        }

        private void RefreshDPIScaling()
        {
            try
            {
                // Use current window center to select the monitor
                var center = new Point(Window.Left + Window.Width / 2, Window.Top + Window.Height / 2);
                var mon = Win32.MonitorFromPoint(center, 2 /*MONITOR_DEFAULT_TONEAREST*/);
                Win32.GetDpiForMonitor(mon, Win32.DpiType.Effective, out var dpiXEffective, out _);

                DpiScaling = dpiXEffective / 96.0;
                Main.AddLog($"Effective DPI: {dpiXEffective} ({DpiScaling:P0}) on monitor containing {Window}");
            }
            catch (Exception e)
            {
                Main.AddLog($"Was unable to set a new dpi scaling, defaulting to 100% zoom, exception: {e}");
                DpiScaling = 1;
            }
        }
    }
}
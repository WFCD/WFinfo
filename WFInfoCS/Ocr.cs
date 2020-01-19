using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WFInfoCS {
	class Ocr {
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool GetWindowRect(HandleRef hwnd, out Rectangle lpRect);

		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		static extern uint GetWindowLongPtr(HandleRef hwnd, int nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		private static extern IntPtr GetForegroundWindow();


		private static double TotalScaling;
		public static WindowStyle currentStyle;
		public enum WindowStyle {
			FULLSCREEN,
			BORDERLESS,
			WINDOWED
		}
		public static HandleRef HandleRef { get; private set; }
		public static float dpi { get; set; }
		private static Process Warframe;
		private static System.Drawing.Point center;
		public static Rectangle window { get; set; }
		private HandleRef handelRef;

		//todo  implemenet Tesseract
		//      implemenet pre-prossesing

		internal static int findRewards(Bitmap image) {
			doDebugDrawing(image);
			return 0;
			//throw new NotImplementedException();
		}

		private static void doDebugDrawing(Bitmap image) {
			using (Graphics graphics = Graphics.FromImage(image)) {
				graphics.DrawRectangle(new Pen(Color.Red), window);
				graphics.DrawRectangle(new Pen(Color.Pink), center.X, center.Y, 5, 5);
				image.Save( Main.appPath + @"\Debug\FullScreenShotDebug.png");
			}
		}

		public static Boolean verifyFocus() { // Returns True if warframe is in focuse, False if not
			uint processID = 0;
			uint threadID = GetWindowThreadProcessId(GetForegroundWindow(), out processID);
			if (processID == Warframe.Id) { return true; } else {
				Main.AddLog("Warframe is not focused");
				Main.updatedStatus("Warframe out of focus", 2);
				return false;
			}
		}

		public static Boolean verifyWarframe() {
			if (Warframe != null) { return true; }
			foreach (Process process in Process.GetProcesses()) {
				if (process.MainWindowTitle == "Warframe") {
					HandleRef = new HandleRef(process, process.MainWindowHandle);
					Warframe = process;
					return true;
				}
			}
			if (Settings.debug) { return true; } else {
				Main.AddLog("Unable to detect Warframe in list of current active processes");
				Main.updatedStatus("Unable to detect Warframe process", 1);
				return false;
			}
		}

		private static Bitmap turnBW(Bitmap source) {
			throw new NotImplementedException();
		}

		private static void refresScaling() {
			using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero)) {
				dpi = (graphics.DpiX / 96); //assuming that y and x axis dpi scaling will be uniform. So only checking one value
				TotalScaling = dpi * (Settings.Scaling / 100);
				Main.AddLog("Scaling updated to: " + TotalScaling + ". User has a DPI scaling of: " + dpi / 96 + " And a set UI scaling of: " + Settings.Scaling + "%");
			}
		}


		public static void updateCenter(Bitmap image = null) {
			Rectangle temprect;

			if (!GetWindowRect(HandleRef, out temprect)) { // get window size of warframe
				if (Settings.debug && Warframe == null) { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
					Main.AddLog("No warframe detected, thus using center of image");
					window = new Rectangle(0,0,image.Width,image.Height);
					center = new Point(window.Width / 2, window.Height / 2);
					return;
				} else {
					Main.AddLog("Failed to get window bounds");
					Main.updatedStatus("Failed to get window bounds", 1);
					return;
				}
			}
			if (window.X < -20000 || window.Y < -20000) { Warframe = null; window = Rectangle.Empty; return; }
			// if the window is in the VOID delete current process and re-set window to nothing

			if (window.Width != temprect.Width || window.Height != temprect.Height) { // checks if old window size is the right size if not change it
				window = new Rectangle (temprect.Left, temprect.Top, temprect.Width - temprect.Left, temprect.Height - temprect.Top); // gett Rectangle out of rect
				//Rectangle is (x, y, width, height) RECT is (x, y, x+width, y+height) 
				Main.AddLog("Window size updated to: " + window.ToString());
				int GWL_style = -16;
				uint Fullscreen = 885981184;
				uint Borderless = 2483027968;
				uint styles = GetWindowLongPtr(HandleRef, GWL_style);
				if (styles == Fullscreen) { currentStyle = WindowStyle.FULLSCREEN; Main.AddLog("Fullscreen detected"); } else if (styles == Borderless) { currentStyle = WindowStyle.BORDERLESS; Main.AddLog("Borderless detected"); } else { // windowed
					window = new Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38);
					Main.AddLog("Windowed detected, updated to: " + window.ToString());
					currentStyle = WindowStyle.WINDOWED;
				}
				center = new Point(window.Width / 2, window.Height / 2);
			}
			refresScaling();

		}

		internal static Bitmap getReward(int rewardSlot, int TotalRewards) {
			throw new NotImplementedException();
		}

		internal static void proces(Bitmap reward) {
			reward = turnBW(reward);
			throw new NotImplementedException();
		}
	}
}
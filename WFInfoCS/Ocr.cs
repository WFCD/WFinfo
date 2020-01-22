using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WFInfoCS {
	class Ocr {


		private static double TotalScaling;
		public static WindowStyle currentStyle;
		public enum WindowStyle {
			FULLSCREEN,
			BORDERLESS,
			WINDOWED
		}
		public static HandleRef HandleRef { get; private set; }
		public static float dpi { get; set; }
		private static Process Warframe = null;
		private static Point center;
		public static Rectangle window { get; set; }
		private HandleRef handelRef;                                 
		private static double xPrecentFirstReward = 379;  
		private static double xPrecentSecondReward = 184;  
		private static double yPrecentReward = 75; 
		private static Rectangle firstRewardRectangle;  
		private static Rectangle secondRewardRectangle;
		private static double screenScaling; // Additional to settings.scaling this is used to calculate any widescreen or 4:3 aspect content.
		//todo  implemenet Tesseract
		//      implemenet pre-prossesing

		internal static int findRewards(Bitmap image) {
			// Firstly check at the first possible possition with 4 rewards, which is at Width = 0.3097 % and Height = 0.4437 %
			// If not found, check first possible possition with 2 rewards, which is 0.4218 %
			// If also not found, there are 3 rewards

			if (image.Width / image.Height > 16 / 9) { // image is less than 16:9 aspect
				screenScaling = image.Height / 1080 * (Settings.Scaling/100) * dpi;
			} else {
				screenScaling = image.Width / 1920 * (Settings.Scaling / 100) * dpi; //image is higher than 16:9 aspect
			}
			center = new Point(image.Width / 2, image.Height / 2);

			Size gemBox = new Size((int)(40 * screenScaling), (int)(35 * screenScaling));


			firstRewardRectangle = getAdjustedRectangle((int)(xPrecentFirstReward * screenScaling), (int)(yPrecentReward * screenScaling), gemBox);
			secondRewardRectangle = getAdjustedRectangle((int)(xPrecentSecondReward * screenScaling), (int)(yPrecentReward * screenScaling), gemBox);

			Bitmap firstReward = image.Clone(firstRewardRectangle, image.PixelFormat);
			Bitmap secondReward = image.Clone(secondRewardRectangle, image.PixelFormat);

			doDebugDrawing(image);
			image.Dispose();
			if (verrifyReward(firstReward)) { 
				Console.WriteLine("4 rewards");
				return 4;
			} else if (verrifyReward(secondReward)) { 
				Console.WriteLine("2 rewards");
				return 2;
			} else { 
				Console.WriteLine("3 rewards");
				return 3;
			}
		}

		private static Rectangle getAdjustedRectangle(int x, int y, Size size) { //adjust the rectangle to capture from the center of the screen instead of from origin.
			int left = center.X - x;
			int top = center.Y - y;
			Console.WriteLine("Center cords: " + center.ToString());
			Console.WriteLine("Calculated position of rectangle " +left + "," + top);
			Console.WriteLine("pixels away from center " + x + "," + y);


			return new Rectangle(left, top, size.Width, size.Height);
		}

		private static Boolean verrifyReward(Bitmap image) {
			for (int x = 0; x < image.Width; x++) {
				for (int y = 0; y < image.Height; y++) {
					for (int color = 0; color < 3; color++) {
						if (ColorThreshold(image.GetPixel(x, y), ColorTranslator.FromHtml("#" + Settings.colorArray["rarityColor"][color].ToString()))) {
							Main.AddLog("Found color: " + ColorTranslator.FromHtml("#" + Settings.colorArray["rarityColor"][color].ToString()));
							return true;
						}

					}
				}
			}
			return false;
		}

		public static Boolean ColorThreshold(Color test, Color thresh, int Treshold = 10) {
			return (Math.Abs(test.R - thresh.R) < Treshold && Math.Abs(test.G - thresh.G) < Treshold && Math.Abs(test.B - thresh.B) < Treshold);
		}

		private static void doDebugDrawing(Bitmap image) {
			using (Graphics graphics = Graphics.FromImage(image)) {
				graphics.DrawRectangle(new Pen(Color.Red), new Rectangle(new Point(window.X, window.Y), new Size(window.Width - 1, window.Height - 1)));
				graphics.DrawRectangle(new Pen(Color.Red), firstRewardRectangle);
				graphics.DrawRectangle(new Pen(Color.Red), secondRewardRectangle);
				graphics.FillRectangle(Brushes.Pink, center.X, center.Y, 5, 5);
				image.Save(Main.appPath + @"\Debug\FullScreenShotDebug " + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png") ;
			}
		}

		public static Boolean verifyFocus() { // Returns True if warframe is in focuse, False if not
			uint processID = 0;
			uint threadID = Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out processID);
			try {
				if (processID == Warframe.Id || Settings.debug) { return true; } else {
					Main.AddLog("Warframe is not focused");
					Main.updatedStatus("Warframe is out of focus", 2);
					return false;
				}
			}
			catch (Exception ex) {
				Console.WriteLine(Warframe.ToString());
				Console.WriteLine(ex.ToString());
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
			Main.AddLog("Unable to detect Warframe in list of current active processes");
			Main.updatedStatus("Unable to detect Warframe process", 1);
			return false;

		}

		private static Bitmap turnBW(Bitmap source) {
			throw new NotImplementedException();
		}

		private static void refresScaling() {
			using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero)) {
				dpi = (graphics.DpiX / 96); //assuming that y and x axis dpi scaling will be uniform. So only checking one value
				TotalScaling = dpi * (Settings.Scaling / 100);
				Main.AddLog("Scaling updated to: " + TotalScaling + ". User has a DPI scaling of: " + dpi + " And a set UI scaling of: " + Settings.Scaling + "%");
			}
		}


		public static void updateWindow(Bitmap image = null) {
			Win32.RECT osRect;
			refresScaling();
			if (!Win32.GetWindowRect(HandleRef, out osRect)) { // get window size of warframe
				if (Settings.debug && Warframe == null) { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
					Main.AddLog("No warframe detected, thus using center of image");
					window = new Rectangle(0, 0, image.Width, image.Height);
					center = new Point(window.Width / 2, window.Height / 2);
					Console.WriteLine("Window is: " + window + " And center is: " + center);
					return;
				} else {
					Main.AddLog("Failed to get window bounds");
					Main.updatedStatus("Failed to get window bounds", 1);
					return;
				}
			}
			if (window.X < -20000 || window.Y < -20000) { Warframe = null; window = Rectangle.Empty; return; }
			// if the window is in the VOID delete current process and re-set window to nothing
			if (window.Left != osRect.Left || window.Right != osRect.Right || window.Top != osRect.Top || window.Bottom != osRect.Bottom) { // checks if old window size is the right size if not change it
				window = new Rectangle(osRect.Left, osRect.Top, osRect.Right - osRect.Left, osRect.Bottom - osRect.Top); // gett Rectangle out of rect
																														 //Rectangle is (x, y, width, height) RECT is (x, y, x+width, y+height) 
				Main.AddLog("Window size updated to: " + window.ToString());
				int GWL_style = -16;
				uint Fullscreen = 885981184;
				uint Borderless = 2483027968;
				uint styles = Win32.GetWindowLongPtr(HandleRef, GWL_style);
				if (styles == Fullscreen) { currentStyle = WindowStyle.FULLSCREEN; Main.AddLog("Fullscreen detected"); } //Fullscreen, don't do anything
				else if (styles == Borderless) { currentStyle = WindowStyle.BORDERLESS; Main.AddLog("Borderless detected"); } //Borderless, don't do anything
				else { // Windowed, adjust for thicc border
					window = new Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38);
					Main.AddLog("Windowed detected, compensating to to: " + window.ToString());
					currentStyle = WindowStyle.WINDOWED;
				}
				//center = new Point(window.Width / 2, window.Height / 2);
			}

		}

		internal static Bitmap getReward(int rewardSlot, int TotalRewards) {
			return null;
			throw new NotImplementedException();
		}

		internal static void proces(Bitmap reward) {
			return;
			reward = turnBW(reward);
			throw new NotImplementedException();
		}
	}
}
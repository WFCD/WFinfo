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
		private static double widthPrecentFirstReward = 0.301953125; // 773 /2560 = 0.301953125
		private static double widthPrecentSecondReward = 0.428125; // 1096 /2560 =0.428125
		private static double HeightPrecentReward = 0.430555555556; // 620 / 1440 = 0.430555555556
		private static Rectangle firstRewardRectangle; //40 x 35 for rare, catches common
		private static Rectangle secondRewardRectangle;
		//todo  implemenet Tesseract
		//      implemenet pre-prossesing

		internal static int findRewards(Bitmap image) {
			Console.WriteLine();
			// Firstly check at the first possible possition with 4 rewards, which is at Width = 0.3097 % and Height = 0.4437 %
			// If not found, check first possible possition with 2 rewards, which is 0.4218 %
			// If also not found, there are 3 rewards
			firstRewardRectangle = new Rectangle((int)(image.Width * widthPrecentFirstReward), (int)(image.Height * HeightPrecentReward), 40, 35);
			secondRewardRectangle = new Rectangle((int)(image.Width * widthPrecentSecondReward), (int)(image.Height * HeightPrecentReward), 40, 35);

			Bitmap firstReward = new Bitmap(7, 7);
			Bitmap secondReward = new Bitmap(7, 7);
			firstReward = image.Clone(firstRewardRectangle, image.PixelFormat);
			secondReward = image.Clone(secondRewardRectangle, image.PixelFormat);
			firstReward.Save(Main.appPath + @"\Debug\testImage1.png");
			secondReward.Save(Main.appPath + @"\Debug\testImage2.png");
			doDebugDrawing(image);
			if (verrifyReward(firstReward)) { // 4 rewards
				Console.WriteLine("4 rewards");
				return 4;
			} else if (verrifyReward(secondReward)) { // 2 rewards
				Console.WriteLine("2 rewards");
				return 2;
			} else { // 3 rewards
				Console.WriteLine("3 rewards");
				return 3;
			}
		}

		private static Boolean verrifyReward(Bitmap image) {
			for (int x = 0; x < image.Width; x++) {
				for (int y = 0; y < image.Height; y++) {
					for (int color = 0; color < 2; color++) {
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
				image.Save(Main.appPath + @"\Debug\FullScreenShotDebug.png");
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


		public static void updateCenter(Bitmap image = null) {
			Win32.RECT osRect;

			if (!Win32.GetWindowRect(HandleRef, out osRect)) { // get window size of warframe
				if (Settings.debug && Warframe == null) { //if debug is on AND warframe is not detected, sillently ignore missing process and use main monitor center.
					Main.AddLog("No warframe detected, thus using center of image");
					window = new Rectangle(0, 0, image.Width, image.Height);
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
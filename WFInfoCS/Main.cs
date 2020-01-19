using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace WFInfoCS {
	class Main {
		public static string appPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS";
		private System.Windows.Media.Brush lightBlue = new SolidColorBrush(System.Windows.Media.Color.FromRgb(177, 208, 217));
		public static string buildVersion = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
		public Main() {

		}

		public static void AddLog(string argm) { //write to the debug file, includes version and UTCtime
			string path = appPath + @"\Debug";
			Console.WriteLine(argm);
			Directory.CreateDirectory(path);
			using (StreamWriter sw = File.AppendText(path + @"\debug.txt")) {
				sw.WriteLineAsync("[" + DateTime.UtcNow + " " + buildVersion + "] \t" + argm);
			}
		}

		public delegate void statusHandler(string message, int serverity);
		public static statusHandler updatedStatus;
		public virtual void statusUpdate(string message, int serverity) {
			updatedStatus?.Invoke(message, serverity);
		}

		public void OnKeyAction(Keys key) {
			if (KeyInterop.KeyFromVirtualKey((int)key) == Settings.activationKey) { //check if user pressed activation key
				if (Settings.debug && (Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
					Main.AddLog("Loading screenshot from file");
					Main.updatedStatus("Offline testing with screenshot", 0);
					doWork(LoadScreenshot());
				} else {
					if (Ocr.verifyWarframe()) {
						if (Ocr.verifyFocus()) {
							doWork(CaptureScreenshot());
						}
					}
				}
			}
			//statusUpdate(key.ToString(), 0); //shows keypresses
		}

		private Bitmap CaptureScreenshot() {
			Bitmap image;
			Ocr.updateCenter();

			int height = Screen.PrimaryScreen.Bounds.Height * (int)Ocr.dpi;
			int width = Screen.PrimaryScreen.Bounds.Width * (int)Ocr.dpi;
			Bitmap Fullscreen = new Bitmap(width, height);
			Size FullscreenSize = new Size(Fullscreen.Width, Fullscreen.Height);
			using (Graphics graphics = Graphics.FromImage(Fullscreen)) {
				graphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.Left, Screen.PrimaryScreen.Bounds.Top, 0, 0, FullscreenSize, CopyPixelOperation.SourceCopy);
			}
			Fullscreen.Save(Main.appPath + @"\Debug\Fullscreenshot.png");

			image = Fullscreen;
			return image;
		}

		private Bitmap LoadScreenshot() {
			Bitmap image;
			using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
				openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
				openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
				openFileDialog.FilterIndex = 2;
				openFileDialog.RestoreDirectory = true;

				if (openFileDialog.ShowDialog() == DialogResult.OK) {
					//Get the path of specified file
					image = new Bitmap(openFileDialog.FileName);
					Ocr.updateCenter(image);
					return image;
				} else {
					statusUpdate("Faild to load image", 1);
					return null;
				}
			}
		}

		public void doWork(Bitmap image) {
			if (image == null) { return; }
			//if (Settings.debug){image.Save(AppPath + @"\Debug\FullScreenShot" + DateTime.UtcNow + ".jpg");} //save image if debug is on
			int Rewards = Ocr.findRewards(image);
			for (int i = 0; i < Rewards; i++) {
				Bitmap reward = Ocr.getReward(i, Rewards);
				Ocr.proces(reward);
			}
		}

		//getters, boring shit
		public string BuildVersion { get => buildVersion; }
		public System.Windows.Media.Brush LightBlue { get => lightBlue; }
		public string AppPath { get => appPath; }
	}
	public class Status {
		public string Message { get; set; }
		public int Serverity { get; set; }

		public Status(string msg, int ser) {
			Message = msg;
			Serverity = ser;
		}
	}
}

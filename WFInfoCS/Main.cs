using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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
					LoadScreenshot();
				} else if (OCR.verifyWarframe())
					//if (Ocr.verifyFocus()) 
					//   Removing because a player may focus on the app during selection if they're using the window style, or they have issues, or they only have one monitor and want to see status
					//   There's a lot of reasons why the focus won't be too useful, IMO -- Kekasi
					Task.Factory.StartNew(new Action(doWork));
			}
			//statusUpdate(key.ToString(), 0); //shows keypresses
		}

		private Bitmap LoadScreenshot() {
			Bitmap image;
			using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
				openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
				openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
				openFileDialog.FilterIndex = 2;
				openFileDialog.RestoreDirectory = true;
				openFileDialog.Multiselect = true;

				if (openFileDialog.ShowDialog() == DialogResult.OK) {
					foreach (string file in openFileDialog.FileNames) {
						Console.WriteLine("Testing file: "+file.ToString());
						try {
							//Get the path of specified file
							image = new Bitmap(file);
							OCR.updateWindow(image);
							doWork(image);
						}
						catch (Exception) {
							statusUpdate("Faild to load image", 1);
							return null;
						}

					}
				} else {
					statusUpdate("Faild to load image", 1);
					return null;
				}
				return null;
			}
		}

		public void doWork() {
			//if (Settings.debug){image.Save(AppPath + @"\Debug\FullScreenShot" + DateTime.UtcNow + ".jpg");} //save image if debug is on
			OCR.ProcessRewardScreen();
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

﻿using System;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Net;
using AutoUpdaterDotNET;
using System.Windows;
using System.Diagnostics;

namespace WFInfo
{
    class Main
    {
        public static Main INSTANCE;
        public static string appPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        public static string buildVersion;
        public static Data dataBase;
        public static RewardWindow window;
        public static Overlay[] overlays;
        public static RelicsWindow relicWindow;
        public static EquipmentWindow equipmentWindow;
        public static Settings settingsWindow;
        public static ErrorDialogue popup;
        public static UpdateDialogue update;
        public static SnapItOverlay snapItOverlayWindow;
        public static Stopwatch watch;
		public static SearchIt searchBox;
		public static Login browser;
        public Main()
        {
            INSTANCE = this;
            StartMessage();
            buildVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            buildVersion = buildVersion.Substring(0, buildVersion.LastIndexOf("."));
            overlays = new Overlay[4] { new Overlay(), new Overlay(), new Overlay(), new Overlay() };
            window = new RewardWindow();
            dataBase = new Data();
            relicWindow = new RelicsWindow();
            equipmentWindow = new EquipmentWindow();
            settingsWindow = new Settings();
            snapItOverlayWindow = new SnapItOverlay();
            watch = Stopwatch.StartNew();
			searchBox = new SearchIt();
			browser = new Login();

			AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
			AutoUpdater.Start("https://github.com/WFCD/WFinfo/releases/latest/download/update.xml");

			Task.Factory.StartNew(ThreadedDataLoad);
		}

		private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args) {
			update = new UpdateDialogue(args);
		}

		public static void ThreadedDataLoad() {
			try {
				StatusUpdate("Updating Databases...", 0);
				dataBase.Update();
				//RelicsWindow.LoadNodesOnThread();
				OCR.init();
				StatusUpdate("WFInfo Initialization Complete", 0);
				AddLog("WFInfo has launched successfully");
				if ((bool)Settings.settingsObj["Auto"])
					dataBase.EnableLogcapture();
			}
			catch (Exception ex) {
				AddLog("LOADING FAILED");
				AddLog(ex.ToString());
				StatusUpdate("Launch Failure - Please Restart", 0);
				new ErrorDialogue(DateTime.Now, 0);
			}
		}

		public static T CreateOnUIThread<T>(Func<T> act) {
			return MainWindow.INSTANCE.Dispatcher.Invoke(act);
		}

		public static void RunOnUIThread(Action act) {
			MainWindow.INSTANCE.Dispatcher.Invoke(act);
		}

		public static void StartMessage() {
			Directory.CreateDirectory(appPath);
			Directory.CreateDirectory(appPath + @"\debug");
			using (StreamWriter sw = File.AppendText(appPath + @"\debug.log")) {
				sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");
				sw.WriteLineAsync("   STARTING WFINFO " + buildVersion + " at " + DateTime.UtcNow);
				sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");
			}
		}

		public static void AddLog(string argm) { //write to the debug file, includes version and UTCtime
			Console.WriteLine(argm);
			Directory.CreateDirectory(appPath);
			using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
				sw.WriteLineAsync("[" + DateTime.UtcNow + " " + buildVersion + "]   " + argm);
		}

		public static void StatusUpdate(string message, int serverity) {
			MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.ChangeStatus(message, serverity); });
		}

		public void OnMouseAction(MouseButton key) {
			if (Settings.ActivationMouseButton != MouseButton.Left && key == Settings.ActivationMouseButton) { //check if user pressed activation key
				if (Keyboard.IsKeyDown(Key.Delete)) { //Close all overlays if hotkey + delete is held down
					foreach (Window overlay in App.Current.Windows) {
						if (overlay.GetType().ToString() == "WFInfo.Overlay") {
							overlay.Hide();
						}
					}
					return;
				}

				if (searchBox.isInUse) { //if key is pressed and searchbox is active then rederect keystokes to it.
					if (Keyboard.IsKeyDown(Key.Escape)) { // close it if esc is used.
						searchBox.finish();
						return;
					}
					searchBox.searchField.Focus();
					return;
				}

				if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey) && Keyboard.IsKeyDown(Settings.SnapitModifierKey)) { //snapit debug
					AddLog("Loading screenshot from file for snapit");
					StatusUpdate("Offline testing with screenshot for snapit", 0);
					LoadScreenshotSnap();
				} else if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey)) {//normal debug
					AddLog("Loading screenshot from file");
					StatusUpdate("Offline testing with screenshot", 0);
					LoadScreenshot();
				} else if (Keyboard.IsKeyDown(Settings.SnapitModifierKey)) {//snapit
					AddLog("Starting snap it");
					StatusUpdate("Starting snap it", 0);
					OCR.SnapScreenshot();
				} else if (Keyboard.IsKeyDown(Settings.SearchItModifierKey)) { //Searchit  
					AddLog("Starting search it");
					StatusUpdate("Starting search it", 0);
					searchBox.Start();
				} else if (Settings.debug || OCR.VerifyWarframe()) {
					Task.Factory.StartNew(() => OCR.ProcessRewardScreen());
				}
			}
		}

		public void OnKeyAction(Key key) {
			// close the snapit overlay when *any* key is pressed down
			if (snapItOverlayWindow.isEnabled && KeyInterop.KeyFromVirtualKey((int)key) != Key.None) {
				snapItOverlayWindow.closeOverlay();
				StatusUpdate("Closed snapit", 0);
				return;
			}
			if(searchBox.isInUse) { //if key is pressed and searchbox is active then rederect keystokes to it.
				if(key == Key.Escape) { // close it if esc is used.
					searchBox.finish();
					return;
				}
				searchBox.searchField.Focus();
				return;
			}

			if (key == Settings.ActivationKey) { //check if user pressed activation key
				if (Keyboard.IsKeyDown(Key.Delete)) { //Close all overlays if hotkey + delete is held down
					foreach (Window overlay in App.Current.Windows) {
						if (overlay.GetType().ToString() == "WFInfo.Overlay") {
							overlay.Hide();
						}
					}
					StatusUpdate("Overlays dissmissed", 1);
					return;
				}
				if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey) && Keyboard.IsKeyDown(Settings.SnapitModifierKey)) { //snapit debug
					AddLog("Loading screenshot from file for snapit");
					StatusUpdate("Offline testing with screenshot for snapit", 0);
					LoadScreenshotSnap();
				} else if (Settings.debug && Keyboard.IsKeyDown(Settings.DebugModifierKey)) {//normal debug
					AddLog("Loading screenshot from file");
					StatusUpdate("Offline testing with screenshot", 0);
					LoadScreenshot();
				} else if (Keyboard.IsKeyDown(Settings.SnapitModifierKey)) {//snapit
					AddLog("Starting snap it");
					StatusUpdate("Starting snap it", 0);
					OCR.SnapScreenshot();
				} else if (Keyboard.IsKeyDown(Settings.SearchItModifierKey)) { //Searchit  
					AddLog("Starting search it");
					StatusUpdate("Starting search it", 0);
					//searchBox.Start();
					browser.Show();
				} else if (Settings.debug || OCR.VerifyWarframe()) {
					Task.Factory.StartNew(() => OCR.ProcessRewardScreen());
				}
			}

		}

		// timestamp is the time to look for, and gap is the threshold of seconds different
		public static void SpawnErrorPopup(DateTime timeStamp, int gap = 30) {
			popup = new ErrorDialogue(timeStamp, gap);
		}

		private void LoadScreenshot() {
			// Using WinForms for the openFileDialog because it's simpler and much easier
			using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog()) {
				openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
				openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
				openFileDialog.FilterIndex = 2;
				openFileDialog.RestoreDirectory = true;
				openFileDialog.Multiselect = true;

				if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					Task.Factory.StartNew(() => {
						try {
							foreach (string file in openFileDialog.FileNames) {
								AddLog("Testing file: " + file.ToString());

								//Get the path of specified file
								Bitmap image = new Bitmap(file);
								OCR.UpdateWindow(image);
								OCR.ProcessRewardScreen(image);
							}

						}
						catch (Exception e) {
							AddLog(e.Message);
							StatusUpdate("Failed to load image", 1);
						}
					});
				} else {
					StatusUpdate("Failed to load image", 1);
				}
			}
		}

		private void LoadScreenshotSnap() {
			using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog()) {
				openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
				openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
				openFileDialog.FilterIndex = 2;
				openFileDialog.RestoreDirectory = true;
				openFileDialog.Multiselect = true;

				if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					Task.Factory.StartNew(() => {
						try {
							foreach (string file in openFileDialog.FileNames) {
								AddLog("Testing snapit on file: " + file.ToString());

								Bitmap image = new Bitmap(file);
								OCR.ProcessSnapIt(image, image, new System.Drawing.Point(0, 0));
							}

						}
						catch (Exception e) {
							AddLog(e.Message);
							StatusUpdate("Failed to load image", 1);
						}
					});
				} else {
					StatusUpdate("Failed to load image", 1);
				}
			}
		}

		//getters, boring shit
		//    you're boring shit
		public static string BuildVersion { get => buildVersion; }
		public string AppPath { get => appPath; }

		public static int VersionToInteger(string vers) {
			int ret = 0;
			string[] versParts = Regex.Replace(vers, "[^0-9.]+", "").Split('.');
			if (versParts.Length == 3)
				for (int i = 0; i < versParts.Length; i++) {
					if (versParts[i].Length == 0)
						return -1;
					ret += Convert.ToInt32(int.Parse(versParts[i]) * Math.Pow(100, 2 - i));
				}

			return ret;
		}

		// Glob
		public static System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en");
	}

	public class Status {
		public string message;
		public int severity;

		public Status(string msg, int ser) {
			message = msg;
			severity = ser;
		}
	}

}

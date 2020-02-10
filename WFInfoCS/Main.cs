﻿using System;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace WFInfoCS
{
    class Main
    {
        public static Main INSTANCE;
        public static string appPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        public static string buildVersion;
        public static Data dataBase;
        public static Window window;
        public static Overlay[] overlays;
        public static RelicsWindow relicWindow;
        public static EquipmentWindow equipmentWindow;
        public static Settings settingsWindow;
        public static ErrorDialogue popup;
        public Main()
        {
            INSTANCE = this;
            StartMessage();
            buildVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            buildVersion = buildVersion.Substring(0, buildVersion.LastIndexOf("."));
            overlays = new Overlay[4] { new Overlay(), new Overlay(), new Overlay(), new Overlay() };
            window = new Window();
            dataBase = new Data();
            relicWindow = new RelicsWindow();
            equipmentWindow = new EquipmentWindow();
            settingsWindow = new Settings();
            Task.Factory.StartNew(new Action(ThreadedDataLoad));
        }

        public static void ThreadedDataLoad()
        {
            dataBase.Update();
            //RelicsWindow.LoadNodesOnThread();
            RunOnUIThread(() => { MainWindow.INSTANCE.Market_Data.Content = "Market Data: " + dataBase.marketData["timestamp"].ToString().Substring(5, 11); });
            RunOnUIThread(() => { MainWindow.INSTANCE.Drop_Data.Content = "Drop Data: " + dataBase.equipmentData["timestamp"].ToString().Substring(5, 11); });
            RunOnUIThread(() => { MainWindow.INSTANCE.Wiki_Data.Content = "Wiki Data: " + dataBase.equipmentData["rqmts_timestamp"].ToString().Substring(5, 11); });
            StatusUpdate("WFInfo Initialization Complete", 0);
            AddLog("WFInfo has launched successfully");
            if ((bool)Settings.settingsObj["Auto"])
            {
                dataBase.EnableLogcapture();
            }
        }

        public static T CreateOnUIThread<T>(Func<T> act)
        {
            return MainWindow.INSTANCE.Dispatcher.Invoke(act);
        }

        public static void RunOnUIThread(Action act)
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(act);
        }

        public static void StartMessage()
        {
            Directory.CreateDirectory(appPath);
            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
            {
                sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");
                sw.WriteLineAsync("   STARTING WFINFO " + buildVersion + " at " + DateTime.UtcNow);
                sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");
            }
        }

        public static void AddLog(string argm)
        { //write to the debug file, includes version and UTCtime
            Console.WriteLine(argm);
            Directory.CreateDirectory(appPath);
            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                sw.WriteLineAsync("[" + DateTime.UtcNow + " " + buildVersion + "]   " + argm);
        }

        public static void StatusUpdate(string message, int serverity)
        {
            MainWindow.INSTANCE.Dispatcher.Invoke(() => { MainWindow.INSTANCE.ChangeStatus(message, serverity); });
        }

        public void OnKeyAction(Keys key)
        {
            if (KeyInterop.KeyFromVirtualKey((int)key) == Settings.activationKey)
            { //check if user pressed activation key
                if (Settings.debug && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    AddLog("Loading screenshot from file");
                    StatusUpdate("Offline testing with screenshot", 0);
                    LoadScreenshot();
                } else if (Settings.debug || OCR.VerifyWarframe())
                {
                    //if (Ocr.verifyFocus()) 
                    //   Removing because a player may focus on the app during selection if they're using the window style, or they have issues, or they only have one monitor and want to see status
                    //   There's a lot of reasons why the focus won't be too useful, IMO -- Kekasi
                    Task.Factory.StartNew(() => OCR.ProcessRewardScreen());
                }
            }

        }

        public static void SpawnErrorPopup()
        {
            popup = new ErrorDialogue();
        }

        private void LoadScreenshot()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            foreach (string file in openFileDialog.FileNames)
                            {
                                Console.WriteLine("Testing file: " + file.ToString());

                                //Get the path of specified file
                                Bitmap image = new Bitmap(file);
                                OCR.UpdateWindow(image);
                                OCR.ProcessRewardScreen(image);
                            }

                        }
                        catch (Exception)
                        {
                            StatusUpdate("Faild to load image", 1);
                        }
                    });
                } else
                {
                    StatusUpdate("Faild to load image", 1);
                }
            }
        }

        //getters, boring shit
        public static string BuildVersion { get => buildVersion; }
        public string AppPath { get => appPath; }

        public static int VersionToInteger(string vers)
        {
            int ret = 0;
            string[] versParts = Regex.Replace(vers, "[^0-9.]+", "").Split('.');
            if (versParts.Length == 3)
                for (int i = 0; i < versParts.Length; i++)
                {
                    if (versParts[i].Length == 0)
                        return -1;
                    ret += Convert.ToInt32(int.Parse(versParts[i]) * Math.Pow(100, 2 - i));
                }

            return ret;
        }

        // Glob
        public static System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en");
    }

    public class Status
    {
        public string message;
        public int severity;

        public Status(string msg, int ser)
        {
            message = msg;
            severity = ser;
        }
    }

}

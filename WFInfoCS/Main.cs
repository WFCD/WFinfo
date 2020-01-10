using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace WFInfoCS
{
    class Main{
        private string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS";
        private System.Windows.Media.Brush lightBlue = new SolidColorBrush(System.Windows.Media.Color.FromRgb(177, 208, 217));
        private string buildVersion = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private string hotKey = "Home";
        public Main(){

        }

        public void AddLog(string argm)
        { //write to the debug file, includes version and UTCtime
            string path = AppPath + @"\Debug";
            Console.WriteLine(argm);
            Directory.CreateDirectory(path);
            using (StreamWriter sw = File.AppendText(path + @"\debug.txt"))
            {
                sw.WriteLineAsync("[" + DateTime.UtcNow + " " + BuildVersion + "] \t" + argm);
            }
        }

        public delegate void statusHandler(string message, int serverity);
        public event statusHandler updatedStatus;
        public virtual void statusUpdate(string message, int serverity)
        {
            updatedStatus?.Invoke(message, serverity);
        }

        public void OnKeyAction(Keys key)
        {
            if (KeyInterop.KeyFromVirtualKey((int)key) == Settings.activationKey){ //check if user pressed activation key
                //todo if debug AND shift is hold down, use loadScreenshot() insetad.
                if (Settings.debug && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    doWork(LoadScreenshot());
                    Console.WriteLine("Load");
                }else{
                    doWork(CaptureScreenshot());
                    Console.WriteLine("Capture");
                }
                Console.WriteLine("Whoohoo");
            }
            statusUpdate(key.ToString(), 0);
        }

        private Bitmap CaptureScreenshot(){
            Bitmap image;
            //todo implement actual screenshoting
            image = LoadScreenshot();
            return image;
        }

        private Bitmap LoadScreenshot(){
            Bitmap image;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK){
                    //Get the path of specified file
                    image = new Bitmap(openFileDialog.FileName);
                    return image;
                }else{
                    statusUpdate("Faild to load image", 1);
                    return null;                
                }
            }
        }

        public void doWork(Bitmap image)
        {
            if (Settings.debug){image.Save(AppPath + @"\Debug\FullScreenShot" + 1 + ".jpg");}
            int players = Ocr.findPlayers(image);
        }

        //getters, boring shit
        public string BuildVersion { get => buildVersion; }
        public System.Windows.Media.Brush LightBlue { get => lightBlue; }
        public string HotKey { get => hotKey; set => hotKey = value; }
        public string AppPath { get => appPath; }
    }
    public class Status
    {
        public string Message { get; set; }
        public int Serverity { get; set; }

        public Status(string msg, int ser)
        {
            Message = msg;
            Serverity = ser;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace WFInfoCS
{
    class Main{
        private string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS";
        private Brush lightBlue = new SolidColorBrush(System.Windows.Media.Color.FromRgb(177, 208, 217));
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
            //Console.WriteLine(key);
            //statusUpdate(key.ToString(), 1);
        }


        public void doWork()
        {
            // to-do start of ocr chain
        }

        //getters, boring shit
        public string BuildVersion { get => buildVersion; }
        public Brush LightBlue { get => lightBlue; }
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

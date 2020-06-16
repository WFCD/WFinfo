using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WFInfo
{
    public delegate void LogWatcherEventHandler(object sender, String text);

    class LogCapture : IDisposable
    {
        private static string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static Timer timer;
        public event LogWatcherEventHandler TextChanged;
        private FileStream fileStream;
        private StreamReader streamReader;
        public LogCapture()
        {
            Main.AddLog("Starting LogCapture");

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(1);

            timer = new Timer((e) =>
            {
                checkLog();
            }, null, startTimeSpan, periodTimeSpan);
        }

        private void checkLog()
        {
            try
            {
                if ((OCR.Warframe == null) || (OCR.Warframe.HasExited))
                {
                    OCR.VerifyWarframe();
                }
                if (OCR.Warframe != null)
                {
                    var message = string.Empty;

                    if (fileStream == null)
                    {
                        fileStream = new FileStream(appdata + @"\..\Local\Warframe\EE.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    }
                    if(streamReader == null)
                    {
                        streamReader = new StreamReader(fileStream, Encoding.Default);
                        message = streamReader.ReadToEnd();
                    }

                    message = streamReader.ReadLine();
                    Console.WriteLine(message);
                    TextChanged(this, message.Trim());
                }

            }
            catch (Exception ex)
            {
                Main.AddLog(ex.ToString());
            }
        }

        public void Dispose()
        {
            fileStream.Dispose();
            streamReader.Dispose();
            timer.Dispose();
            Main.AddLog("Stoping LogCapture");
        }
    }
}

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
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly EventWaitHandle bufferReadyEvent;
        private readonly EventWaitHandle dataReadyEvent;
        readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private CancellationToken token;

        public event LogWatcherEventHandler TextChanged;

        public LogCapture()
        {
            token = tokenSource.Token;
            Main.AddLog("Starting LogCapture");
            memoryMappedFile = MemoryMappedFile.CreateOrOpen("DBWIN_BUFFER", 4096L);

            bufferReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_BUFFER_READY", out Boolean createdBuffer);

            if (!createdBuffer)
            {
                Main.AddLog("The DBWIN_BUFFER_READY event exists.");
                return;
            }

            dataReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_DATA_READY", out Boolean createdData);

            if (!createdData)
            {
                Main.AddLog("The DBWIN_DATA_READY event exists.");
                return;
            }

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(1);

            var timer = new Timer((e) =>
            {
                getProc();
            }, null, startTimeSpan, periodTimeSpan);
        }


        private void getProc()
        {
            Console.WriteLine("Testing to get warframe");
            try
            {
                if ((OCR.Warframe == null) || (OCR.Warframe.HasExited))
                {
                    OCR.VerifyWarframe();
                    return;
                }
                var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                //using (MemoryMappedViewStream stream = memoryMappedFile.CreateViewStream())
                //{
                //    using (BinaryReader reader = new BinaryReader(stream, Encoding.Default))
                //    {
                //        uint processId = reader.ReadUInt32();
                //        if (processId == OCR.Warframe.Id)
                //        {
                //            char[] chars = reader.ReadChars(4092);
                //            int index = Array.IndexOf(chars, '\0');
                //            string message = new string(chars, 0, index);
                //            TextChanged(this, message.Trim());
                //        }
                //    }
                //}


                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(appdata + @"\..\Local\Warframe\EE.log", FileMode.Open))
                {
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        using (BinaryReader reader = new BinaryReader(stream, Encoding.Default))
                        {
                            uint processId = reader.ReadUInt32();
                            Main.AddLog($"Proces ID: {processId}, and warframe id: {OCR.Warframe.Id}");

                                char[] chars = reader.ReadChars(4092);
                                int index = Array.IndexOf(chars, '\0');
                                string message = new string(chars, 0, index);
                                TextChanged(this, message.Trim());
                            
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Main.AddLog(ex.ToString());
                new ErrorDialogue(DateTime.Now, 0);
            }
            finally
            {
                if (memoryMappedFile != null)
                    memoryMappedFile.Dispose();

                if (bufferReadyEvent != null)
                    bufferReadyEvent.Dispose();

                if (dataReadyEvent != null)
                    dataReadyEvent.Dispose();
            }
        }

        public void Dispose()
        {
            if (memoryMappedFile != null)
                memoryMappedFile.Dispose();

            if (bufferReadyEvent != null)
                bufferReadyEvent.Dispose();

            if (dataReadyEvent != null)
                dataReadyEvent.Dispose();

            tokenSource.Cancel();
            tokenSource.Dispose();
            Main.AddLog("Stoping LogCapture");
        }
    }
}

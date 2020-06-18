using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using WebSocketSharp;

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
        private static Timer timer;
        public event LogWatcherEventHandler TextChanged;
        public LogCapture()
        {
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

            timer = new Timer((e) =>
            {
                getProc();
            }, null, startTimeSpan, periodTimeSpan);

            token = tokenSource.Token;
        }

        private void getProc()
        {
            try
            {
                if ((OCR.Warframe == null) || (OCR.Warframe.HasExited))
                {
                    OCR.VerifyWarframe();
                }
                if (OCR.Warframe != null)
                {
                    getStream();
                }

            }
            catch (Exception ex)
            {
                Main.AddLog(ex.ToString());
            }
        }

        private void getStream()
        {
            try
            {
                bufferReadyEvent.Set();
                while (!token.IsCancellationRequested)
                {             
                    using (MemoryMappedViewStream stream = memoryMappedFile.CreateViewStream())
                    {
                        using (BinaryReader reader = new BinaryReader(stream, Encoding.Default))
                        {
                            uint processId = reader.ReadUInt32();
                            if (processId == OCR.Warframe.Id)
                            {
                                if(timer != null)
                                    timer.Dispose();
                                char[] chars = reader.ReadChars(4092);
                                int index = Array.IndexOf(chars, '\0');
                                string message = new string(chars, 0, index);
                                TextChanged(this, message.Trim());
                            }
                        }
                    }
                    
                    bufferReadyEvent.Set();
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
            timer.Dispose();
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

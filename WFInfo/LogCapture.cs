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

            Task.Factory.StartNew(Run);
        }

        private void Run()
        {

            try
            {
                TimeSpan timeout = TimeSpan.FromSeconds(1.0);
                bufferReadyEvent.Set();
                while (!token.IsCancellationRequested)
                {
                    if (!dataReadyEvent.WaitOne(timeout))
                    {
                        continue;
                    }

                    if ((OCR.Warframe == null) || (OCR.Warframe.HasExited))
                    {
                        OCR.VerifyWarframe();
                    }

                    if (OCR.Warframe != null)
                    {
                        using (MemoryMappedViewStream stream = memoryMappedFile.CreateViewStream())
                        {
                            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default))
                            {
                                uint processId = reader.ReadUInt32();
                                if (processId == OCR.Warframe.Id)
                                {
                                    char[] chars = reader.ReadChars(4092);
                                    int index = Array.IndexOf(chars, '\0');
                                    string message = new string(chars, 0, index);
                                    TextChanged(this, message.Trim());
                                }
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

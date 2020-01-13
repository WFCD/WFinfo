using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WFInfoCS
{
    public delegate void LogWatcherEventHandler(object sender, String text);

    class LogCapture : IDisposable
    {
        private MemoryMappedFile memoryMappedFile;
        private EventWaitHandle bufferReadyEvent;
        private EventWaitHandle dataReadyEvent;
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        private CancellationToken token;

        public event LogWatcherEventHandler TextChanged;

        public LogCapture()
        {
            token = tokenSource.Token;
            Main.AddLog("STARTING LogCapture");
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
            Process proc = null; //parser2.GetWFProc(); // WIP

            try
            {
                TimeSpan Timeout = TimeSpan.FromSeconds(1.0);
                bufferReadyEvent.Set();
                while (!token.IsCancellationRequested)
                {
                    if (!dataReadyEvent.WaitOne(Timeout))
                    {
                        continue;
                    }

                    if ((proc == null) || (proc.HasExited))
                    {
                        proc = null; //parser2.GetWFProc();
                    }

                    if (proc != null)
                    {
                        using (MemoryMappedViewStream stream = memoryMappedFile.CreateViewStream())
                        {
                            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default))
                            {
                                uint processId = reader.ReadUInt32();
                                if (processId == proc.Id)
                                {
                                    var chars = reader.ReadChars(4092);
                                    var index = Array.IndexOf(chars, "\0");
                                    var message = new String(chars, 0, index);
                                    TextChanged(this, message.Trim());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (memoryMappedFile != null)
                {
                    memoryMappedFile.Dispose();
                }

                if (bufferReadyEvent != null)
                {
                    bufferReadyEvent.Dispose();
                }

                if (dataReadyEvent != null)
                {
                    dataReadyEvent.Dispose();
                }
            }
        }

        public void Dispose()
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
            Main.AddLog("STOPPING LogCapture");
        }
    }
}

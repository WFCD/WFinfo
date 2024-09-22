using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WFInfo.Settings;
using System.Windows.Threading;

namespace WFInfo.Services.WarframeProcess
{
    public class WarframeProcessFinder : IProcessFinder
    {
        public Process Warframe 
        {
            get
            {
                return _warframe;
            }
        }

        public HandleRef HandleRef => IsRunning ? new HandleRef(Warframe, Warframe.MainWindowHandle) : new HandleRef();
        public bool IsRunning => Warframe != null && !Warframe.HasExited;
        public bool GameIsStreamed => Warframe?.MainWindowTitle.Contains("GeForce NOW") ?? false;
        public event ProcessChangedArgs OnProcessChanged;

        private Process _warframe;

        private readonly IReadOnlyApplicationSettings _settings;
        private DispatcherTimer find_process_timer;

        public WarframeProcessFinder(IReadOnlyApplicationSettings settings)
        {
            _settings = settings;
            find_process_timer = new DispatcherTimer();
            find_process_timer.Interval = TimeSpan.FromSeconds(30);
            find_process_timer.Tick += FindProcess;
            find_process_timer.Start();
            FindProcess(find_process_timer, new EventArgs());
        }

        private void FindProcess(object sender, EventArgs ea)
        {
            // process already found
            if (_warframe != null)
            {
                if (_warframe.HasExited)
                { //reset warframe process variables, and reset LogCapture so new game process gets noticed
                    Main.dataBase.DisableLogCapture();
                    _warframe.Dispose();
                    _warframe = null;

                    if (_settings.Auto)
                        Main.dataBase.EnableLogCapture();
                }
                else return; // Current process is still good
            }
            Main.AddLog("Yes i'm finding a process now");
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName == "Warframe.x64" && process.MainWindowTitle == "Warframe")
                {
                    _warframe = process;

                    if (Main.dataBase.GetSocketAliveStatus())
                        Debug.WriteLine("Socket was open in verify warframe");
                    Task.Run(async () =>
                    {
                        await Main.dataBase.SetWebsocketStatus("in game");
                    });
                    Main.AddLog("Found Warframe Process: ID - " + process.Id + ", MainTitle - " + process.MainWindowTitle + ", Process Name - " + process.ProcessName);

                    //try and catch any UAC related issues
                    try
                    {
                        bool _ = _warframe.HasExited;
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        _warframe = null;

                        Main.AddLog($"Failed to get Warframe process due to: {e.Message}");
                        Main.StatusUpdate("Restart Warframe without admin privileges", 1);

                    }
                    OnProcessChanged?.Invoke(_warframe);
                    return;
                }
                else if (process.MainWindowTitle.Contains("Warframe") && process.MainWindowTitle.Contains("GeForce NOW"))
                {
                    Main.RunOnUIThread(() =>
                    {
                        Main.SpawnGFNWarning();
                    });
                    Main.AddLog("GFN -- Found Warframe Process: ID - " + process.Id + ", MainTitle - " + process.MainWindowTitle + ", Process Name - " + process.ProcessName);

                    _warframe = process;

                    //try and catch any UAC related issues
                    try
                    {
                        bool _ = _warframe.HasExited;
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        _warframe = null;

                        Main.AddLog($"Failed to get Warframe process due to: {e.Message}");
                        Main.StatusUpdate("Restart Warframe without admin privileges, or WFInfo with admin privileges", 1);

                    }
                    OnProcessChanged?.Invoke(_warframe);
                    return;
                }
            }

            if (!_settings.Debug)
            {
                Main.AddLog("Did Not Detect Warframe Process");
                Main.StatusUpdate("Unable to Detect Warframe Process", 1);
            }
        }
    }
}

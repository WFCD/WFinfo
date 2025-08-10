using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WFInfo.Settings;
using System.Threading;

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
            private set
            {
                if (_warframe == value)
                {
                    // nothing to do if process didn't change
                    return;
                }

                if (_warframe != null)
                {
                    // Old warframe process is still cached, lets dispose it
                    Main.dataBase.DisableLogCapture();
                    _warframe.Dispose();
                    _warframe = null;
                }

                // actually switching process
                _warframe = value;
                // cache new GameIsStreamed value. No need to constantly re-check title
                GameIsStreamed = _warframe?.MainWindowTitle.Contains("GeForce NOW") ?? false;

                if (_warframe != null)
                {
                    // New process found
                    if (Main.dataBase.GetSocketAliveStatus()) 
                    {
                        Debug.WriteLine("Socket was open in verify warframe");
                    }
                    Task.Run(async () =>
                    {
                        await Main.dataBase.SetWebsocketStatus("ingame");
                    });

                    if (_settings.Auto && !GameIsStreamed)
                    {
                        Main.dataBase.EnableLogCapture(); 
                    }
                }

                // Invoking action
                OnProcessChanged?.Invoke(_warframe);
            }
        }

        public HandleRef HandleRef => IsRunning ? new HandleRef(Warframe, Warframe.MainWindowHandle) : new HandleRef();
        public bool IsRunning => Warframe != null && !Warframe.HasExited;
        public bool GameIsStreamed { get; private set; }
        public event ProcessChangedArgs OnProcessChanged;

        private Process _warframe;

        private readonly IReadOnlyApplicationSettings _settings;
        private Timer find_process_timer;
        private const int FindProcessTimerDuration = 40000; // ms

        public WarframeProcessFinder(IReadOnlyApplicationSettings settings)
        {
            _settings = settings;
            // create timer, but don't start yet. Some static fields may not be ready yet
            find_process_timer = new Timer(FindProcess, null, Timeout.Infinite, FindProcessTimerDuration);
            Main.RegisterOnFinishedLoading(Main_OnInitialized);
        }

        private void Main_OnInitialized(object sender, EventArgs e)
        {
            // main is Initialized, start timer
            find_process_timer.Change(0, FindProcessTimerDuration); 
        }

        private void FindProcess(Object stateInfo)
        {
            // Process was already found previously
            if (_warframe != null)
            {
                if (!_warframe.HasExited)
                {
                    return; // Current process is still good
                }
            }

            //Main.AddLog("FindProcess have been triggered");

            Process identified_process = null;
            // Search for Warframe related process
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName == "Warframe.x64" && process.MainWindowTitle == "Warframe")
                {
                    identified_process = process;
                    Main.AddLog("Found Warframe Process: ID - " + process.Id + ", MainTitle - " + process.MainWindowTitle + ", Process Name - " + process.ProcessName);
                    break;
                }
                else if (process.MainWindowTitle.Contains("Warframe") && process.MainWindowTitle.Contains("GeForce NOW"))
                {
                    Main.RunOnUIThread(() =>
                    {
                        Main.SpawnGFNWarning();
                    });
                    Main.AddLog("GFN -- Found Warframe Process: ID - " + process.Id + ", MainTitle - " + process.MainWindowTitle + ", Process Name - " + process.ProcessName);
                    identified_process = process;
                    break;
                }
            }

            // Try and catch any UAC related issues
            if (identified_process != null)
            {
                try
                {
                    bool _ = identified_process.HasExited;
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    identified_process = null;

                    Main.AddLog($"Failed to get Warframe process due to: {e.Message}");
                    Main.StatusUpdate("Restart Warframe without admin privileges, or WFInfo with admin privileges", 1);
                }
            }
            else
            {
                if (!_settings.Debug)
                {
                    Main.AddLog("Did Not Detect Warframe Process");
                    Main.StatusUpdate("Unable to Detect Warframe Process", 1);
                }
            }
            
            // set new process, or null if none found
            Warframe = identified_process;
        }
    }
}

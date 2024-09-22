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
        private System.Threading.Timer find_process_timer;

        public WarframeProcessFinder(IReadOnlyApplicationSettings settings)
        {
            _settings = settings;
            find_process_timer = new System.Threading.Timer(FindProcess, null, 0, 40000);
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

            Process _identified_process = null;
            // Search for Warframe related process
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName == "Warframe.x64" && process.MainWindowTitle == "Warframe")
                {
                    _identified_process = process;
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
                    _identified_process = process;
                    break;
                }
            }

            // Try and catch any UAC related issues
            if (_identified_process != null)
            {
                try
                {
                    bool _ = _identified_process.HasExited;
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    _identified_process = null;

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

            // Old warframe process is still cached, lets dispose it and refer to new process
            if (_warframe != null)
            {
                if (_warframe.HasExited)
                {
                    Main.dataBase.DisableLogCapture();
                    _warframe.Dispose();
                    _warframe = null;
                }
            }
            
            // New process found? lets refer to it
            if (_identified_process != null)
            {
                _warframe = _identified_process;
                if (Main.dataBase.GetSocketAliveStatus())
                    Debug.WriteLine("Socket was open in verify warframe");
                Task.Run(async () =>
                {
                    await Main.dataBase.SetWebsocketStatus("in game");
                });

                if (_settings.Auto && !GameIsStreamed)
                    Main.dataBase.EnableLogCapture();
            }

            // Invoking action
            OnProcessChanged?.Invoke(_warframe);
            return;
        }
    }
}

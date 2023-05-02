using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WFInfo.Services.WarframeProcess
{
    /// <summary>
    /// Used to signal game process changes.
    /// </summary>
    /// <param name="newProcess">Newly detected process</param>
    public delegate void ProcessChangedArgs(Process newProcess);

    /// <summary>
    /// Finds and provides handles to the game process
    /// </summary>
    public interface IProcessFinder
    {
        /// <summary>
        /// Gets the game process, accessing this property will automatically check if the process is still valid. Is <see langword="null"/> when no process can be found.
        /// </summary>
        Process Warframe { get; }

        /// <summary>
        /// Gets the game window handle, accessing this property will automatically check if the process is still valid. Is a null pointer when no process is found.
        /// </summary>
        HandleRef HandleRef { get; }

        /// <summary>
        /// Determines whether the game process is running, accessing this property will automatically check if the process is still valid.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Determines whather the game is being streamed.
        /// </summary>
        bool GameIsStreamed { get; }
        
        /// <summary>
        /// Invoked whenever the game process state changes.
        /// </summary>
        event ProcessChangedArgs OnProcessChanged;
    }
}

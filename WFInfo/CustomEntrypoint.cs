using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Management;

namespace WFInfo
{
    public class CustomEntrypoint
    {

        private const string traineddata = "engbest.traineddata";
        private const string traineddata_hotlink = "https://raw.githubusercontent.com/WFCD/WFinfo/master/WFInfo/tessdata/" + traineddata;
        private const string traineddata_md5 = "7af2ad02d11702c7092a5f8dd044d52f";

        private const string liblept = "liblept1760";
        private const string libtesseract = "libtesseract400";
        private const string tesseract_version_folder = "tesseract4";

        private static string[] list_of_dlls = new string[]
        {
                @"\x86\" + libtesseract + ".dll",
                @"\x86\" + liblept + ".dll",
                @"\x64\" + libtesseract + ".dll",
                @"\x64\" + liblept + ".dll",
                @"\Tesseract.dll"
        };

        private static string[] list_of_checksums = new string[]
        {
                "e5254009fce68dc5ace9307cb5e0ee5f",     //  x86/libtesseract400
                "99d45c6347e46c35ece6d735df42f7f1",     //  x86/liblept1760  
                "bfbaf1f36f4767648a229c65e46ec338",     //  x64/libtesseract400 
                "bd2b84e121f1a3e7786f2cfa2d351eea",     //  x64/liblept1760 
                "7849c3e838444e696fcfaa5e8b9b5c1e"      //  Tesseract
        };

        private static string[] list_of_checksums_AVX_free = new string[]
        {
                "b03b474606c397c716bd509f19fb8a2d",     //  x86/libtesseract400
                "99d45c6347e46c35ece6d735df42f7f1",     //  x86/liblept1760  
                "cf729c20c0fe44f27b235f8ca5efe6b3",     //  x64/libtesseract400 
                "bd2b84e121f1a3e7786f2cfa2d351eea",     //  x64/liblept1760 
                "7849c3e838444e696fcfaa5e8b9b5c1e"      //  Tesseract
        };

        private static readonly string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private static string tesseract_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/master/WFInfo/lib";
        private static readonly string app_data_tesseract_catalog = appPath + @"\" + tesseract_version_folder;

        private const string tessdata_local = @"tessdata\" + traineddata;
        private static readonly string appdata_tessdata_folder = appPath + @"\tessdata";
        private static readonly string app_data_traineddata = appdata_tessdata_folder + @"\" + traineddata;

        private static readonly InitialDialogue dialogue = new InitialDialogue();
        private static bool AvxSupport = true;
        public static CancellationTokenSource stopDownloadTask;

        [STAThreadAttribute]
        public static void Main()
        {
	        AppDomain currentDomain = AppDomain.CurrentDomain;
	        currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            Directory.CreateDirectory(appPath);
            Directory.CreateDirectory(app_data_tesseract_catalog);
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x86");
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x64");

            Directory.CreateDirectory(appdata_tessdata_folder);

            AvxSupport = isAVX2Available();

            if (!AvxSupport || File.Exists(appPath + @"\useSSE.txt"))
            {
                using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + "CPU doesn't support AVX optimizations, falling back to SSE2");
                }

                // SSE2 version without AVX optimizations - for very old pre-2013 CPUs
                tesseract_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/vb-archive/WFInfo/lib";
            }

            int filesNeeded = 0;
            if (!File.Exists(app_data_traineddata) || GetMD5hash(app_data_traineddata) != traineddata_md5)
                filesNeeded++;

            for (int i = 0; i < list_of_dlls.Length; i++)
            {
                string dll = list_of_dlls[i];
                string path = app_data_tesseract_catalog + dll;
                string md5 = AvxSupport ? list_of_checksums[i] : list_of_checksums_AVX_free[i];
                if (!File.Exists(path) || GetMD5hash(path) != md5)
                    filesNeeded++;
            }
            if (filesNeeded > 0)
            {
                dialogue.SetFilesNeed(filesNeeded);
                stopDownloadTask = new CancellationTokenSource();
                Task.Run(() =>
                {
                    try
                    {
                        RefreshTesseractDlls(stopDownloadTask.Token);
                    }
                    catch (Exception ex)
                    {
                        if (stopDownloadTask.IsCancellationRequested)
                        {
                            dialogue.Dispatcher.Invoke(() => { dialogue.Close(); });
                        }
                        else
                        {
                            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                            {
                                sw.WriteLineAsync("--------------------------------------------------------------------------------------------");
                                sw.WriteLineAsync("--------------------------------------------------------------------------------------------");
                                sw.WriteLineAsync("[" + DateTime.UtcNow + "]   ERROR DURING INITIAL LOAD");
                                sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + ex.ToString());
                            }
                        }
                    }
                }, stopDownloadTask.Token);
                dialogue.ShowDialog();
            }
            if (stopDownloadTask == null || !stopDownloadTask.IsCancellationRequested)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve_Tesseract;
                AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
                App.Main();
            }

        }

        // External method from DLL to identify AVX2 support
        // Source code can be found on https://github.com/dimon222/CustomCPUID
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool isAVX2supported();

        static void MyHandler(object sender, UnhandledExceptionEventArgs args) {
	        Exception e = (Exception)args.ExceptionObject;
	        AddLog("MyHandler caught : " + e.Message);
	        AddLog("Runtime terminating: ," + args.IsTerminating);
            AddLog(e.StackTrace);
            AddLog(e.InnerException.Message);
            AddLog(e.InnerException.StackTrace);

        }

        public static void AddLog(string argm) { //write to the debug file, includes version and UTCtime
	        Console.WriteLine(argm);
	        Directory.CreateDirectory(appPath);
	        using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
		        sw.WriteLineAsync("[" + DateTime.UtcNow + "Still in custom entery point" +  "]   " + argm);
        }

        public static bool isAVX2Available()
        {
            string dll = "CustomCPUID.dll";
            string path = app_data_tesseract_catalog + @"\" + dll;
            string md5 = "745d1bdb33e1d2c8df1a90ce1a6cebcd";
            // Redownload if DLL is not present or got corrupted
            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
            {
                sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");
                ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
                foreach (ManagementObject mo in mos.Get())
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "] CPU model name is " + mo["Name"]);
                }

                if (!File.Exists(path) || GetMD5hash(path) != md5)
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "] AVX2 DLL is missing or corrupted, downloading");
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(tesseract_hotlink_prefix + "/" + dll, path);
                }

                // Import DLL that includes low level check for AVX2 support
                IntPtr pDll = NativeMethods.LoadLibrary(path);
                if (pDll == IntPtr.Zero)
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "] AVX2 DLL Pointer is pointing to null, fallback to SSE");
                    return false;
                    // throw new Exception("DLL pointer to CustomCPUID.dll is not identified");
                }

                IntPtr pAddressOfFunctionToCall = NativeMethods.GetProcAddress(pDll, "isAVX2supported");
                if (pAddressOfFunctionToCall == IntPtr.Zero)
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "] AVX2 DLL Function Pointer is pointing to null, fallback to SSE");
                    return false;
                    // throw new Exception("DLL function pointer in CustomCPUID.dll is not identified");
                }
                isAVX2supported isAvx2Supported = (isAVX2supported)Marshal.GetDelegateForFunctionPointer(
                    pAddressOfFunctionToCall,
                    typeof(isAVX2supported));

                return isAvx2Supported();
            }
        }
        static class NativeMethods
        {
            [DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern IntPtr LoadLibrary(string dllToLoad);

            [DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

            [DllImport("kernel32.dll")]
            public static extern bool FreeLibrary(IntPtr hModule);
        }

        public static string GetMD5hash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            dialogue.Dispatcher.Invoke(() => { dialogue.UpdatePercentage(e.ProgressPercentage); });
        }

        private static async void RefreshTesseractDlls(CancellationToken token)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
            token.Register(webClient.CancelAsync);

            if (!File.Exists(app_data_traineddata) || GetMD5hash(app_data_traineddata) != traineddata_md5)
            {
                if (Directory.Exists("tessdata") && File.Exists(tessdata_local))
                {
                    File.Copy(tessdata_local, app_data_traineddata);
                }
                else
                {
                    try
                    {
                        await webClient.DownloadFileTaskAsync(traineddata_hotlink, app_data_traineddata);
                    }
                    catch (Exception) when (stopDownloadTask.Token.IsCancellationRequested) { }
                }
                dialogue.Dispatcher.Invoke(() => { dialogue.FileComplete(); });
            }

            for (int i = 0; i < list_of_dlls.Length; i++)
            {
                if (token.IsCancellationRequested)
                    break;
                string dll = list_of_dlls[i];
                string path = app_data_tesseract_catalog + dll;
                string md5 = AvxSupport ? list_of_checksums[i] : list_of_checksums_AVX_free[i];
                if (!File.Exists(path) || GetMD5hash(path) != md5)
                {
                    if (File.Exists(path))
                        File.Delete(path);

                    if (token.IsCancellationRequested)
                        break;
                    bool success = false;
                    try
                    {
                        if (Directory.Exists("lib") && File.Exists("lib" + dll))
                        {
                            File.Copy("lib" + dll, app_data_tesseract_catalog + dll);
                            success = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                        {
                            await sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + dll + " couldn't be moved");
                            await sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + ex.ToString());
                        }
                    }
                    if (token.IsCancellationRequested)
                        break;

                    if (!success)
                    {
                        try
                        {
                            await webClient.DownloadFileTaskAsync(tesseract_hotlink_prefix + dll.Replace("\\", "/"), app_data_tesseract_catalog + dll);
                        }
                        catch (Exception) when (stopDownloadTask.Token.IsCancellationRequested) { }
                    }
                    dialogue.Dispatcher.Invoke(() => { dialogue.FileComplete(); });
                }
            }
            webClient.Dispose();

            dialogue.Dispatcher.Invoke(() =>
            {
                dialogue.Close();
            });
        }

        private static Assembly CurrentDomain_AssemblyResolve_Tesseract(object sender, ResolveEventArgs args)
        {
            string probingPath = appPath + @"\" + tesseract_version_folder;
            string assyName = new AssemblyName(args.Name).Name;

            string newPath = Path.Combine(probingPath, assyName);
            if (!newPath.EndsWith(".dll"))
                newPath += ".dll";

            if (File.Exists(newPath))
                return Assembly.LoadFile(newPath);

            return null;
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";
            if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);

            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
    }
}

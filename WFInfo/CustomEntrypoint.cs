using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Management;

namespace WFInfo
{
    public class CustomEntrypoint
    {
        private const string liblept = "leptonica-1.80.0";
        private const string libtesseract = "tesseract41";
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
                "f236077a6e5c1a558d5461841245a3d0",     //  x86/tesseract41
                "bfc156ebfe3b86fbce3f24b475c19f20",     //  x86/leptonica-1.80.0
                "a5dbd2c4a232f03913c206ed27eacb40",     //  x64/tesseract41
                "35e3f58d3d868e244b103f901fb2c66d",     //  x64/leptonica-1.80.0
                "02633504a3bb24517de50530bf6bc57c"      //  Tesseract
        };

        private static string[] list_of_checksums_AVX_free = new string[]
        {
                "298c0d71957cb2d82654373582c2313d",     //  x86/tesseract41
                "bfc156ebfe3b86fbce3f24b475c19f20",     //  x86/leptonica-1.80.0
                "3255fefb5dda0bb81adfbb1722fe45ef",     //  x64/tesseract41
                "35e3f58d3d868e244b103f901fb2c66d",     //  x64/leptonica-1.80.0
                "02633504a3bb24517de50530bf6bc57c"      //  Tesseract
        };

        private static readonly string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private static readonly string libs_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/libs";
        private static readonly string tesseract_hotlink_prefix = libs_hotlink_prefix + @"/tesseract41/";
        private static string tesseract_hotlink_platform_specific_prefix;
        private static readonly string app_data_tesseract_catalog = appPath + @"\" + tesseract_version_folder;

        public static readonly string appdata_tessdata_folder = appPath + @"\tessdata";

        private static readonly InitialDialogue dialogue = new InitialDialogue();
        public static CancellationTokenSource stopDownloadTask;


        public static void cleanLegacyTesseractIfNeeded()
        {
            string[] legacy_dll_names = new string[]
            {
                @"\x86\libtesseract400.dll",
                @"\x86\liblept1760.dll",
                @"\x64\libtesseract400.dll",
                @"\x64\liblept1760.dll"
            };
            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
            {
                string path_to_check;
                foreach (string legacy_ddl_name in legacy_dll_names)
                {
                    path_to_check = app_data_tesseract_catalog + legacy_ddl_name;
                    if (File.Exists(path_to_check))
                    {
                        sw.WriteLineAsync("Cleaning legacy leftover - " + legacy_ddl_name);
                        File.Delete(path_to_check);
                    }
                }
            }
        }

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

            cleanLegacyTesseractIfNeeded();
            bool AvxSupport = isAVX2Available();

            if (!AvxSupport || File.Exists(appPath + @"\useSSE.txt"))
            {
                using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + "CPU doesn't support AVX optimizations, falling back to SSE2");
                }

                // SSE2 version without AVX optimizations - for very old pre-2013 CPUs
                tesseract_hotlink_platform_specific_prefix = tesseract_hotlink_prefix + @"/sse2";
            }
            else
            {
                tesseract_hotlink_platform_specific_prefix = tesseract_hotlink_prefix + @"/avx2";
            }

            // Refresh traineddata structure
            // This is temporary, to be removed in half year from now
            if (File.Exists(appdata_tessdata_folder + @"\engbest.traineddata"))
            {
                // To avoid conflicts for folks who like to experiment...
                if (File.Exists(appdata_tessdata_folder + @"\en.traineddata"))
                {
                    File.Delete(appdata_tessdata_folder + @"\en.traineddata");
                }
                File.Move(appdata_tessdata_folder + @"\engbest.traineddata", appdata_tessdata_folder + @"\en.traineddata");
            }
            //

            int filesNeeded = 0;
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
                        RefreshTesseractDlls(stopDownloadTask.Token, AvxSupport);
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

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            AddLog("MyHandler caught : " + e.Message);
            AddLog("Runtime terminating: ," + args.IsTerminating);
            AddLog(e.StackTrace);
            AddLog(e.InnerException.Message);
            AddLog(e.InnerException.StackTrace);

        }

        public static void AddLog(string argm)
        { //write to the debug file, includes version and UTCtime
            Debug.WriteLine(argm);
            Directory.CreateDirectory(appPath);
            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                sw.WriteLineAsync("[" + DateTime.UtcNow + "Still in custom entery point" + "]   " + argm);
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
                    webClient.DownloadFile(libs_hotlink_prefix + "/" + dll, path);
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
        public static string GetMD5hashByURL(string url)
        {
            Debug.WriteLine(url);
            WebClient webClient = new WebClient();
            using (var md5 = MD5.Create())
            {
                byte[] stream = webClient.DownloadData(url);
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            dialogue.Dispatcher.Invoke(() => { dialogue.UpdatePercentage(e.ProgressPercentage); });
        }

        private static async void RefreshTesseractDlls(CancellationToken token, bool AvxSupport)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
            token.Register(webClient.CancelAsync);

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
                            if (dll != @"\Tesseract.dll")
                            {
                                await webClient.DownloadFileTaskAsync(tesseract_hotlink_platform_specific_prefix + dll.Replace("\\", "/"), app_data_tesseract_catalog + dll);
                            }
                            else
                            {
                                await webClient.DownloadFileTaskAsync(tesseract_hotlink_prefix + dll.Replace("\\", "/"), app_data_tesseract_catalog + dll);
                            }
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

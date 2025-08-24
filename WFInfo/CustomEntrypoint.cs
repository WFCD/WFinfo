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
using Microsoft.Win32;
using System.Windows;
using System.Linq;
using System.CodeDom;
using Tesseract;

namespace WFInfo
{
    public class CustomEntrypoint
    {
        private const string liblept = "leptonica-1.82.0";
        private const string libtesseract = "tesseract50";
        private const string tesseract_version_folder = "tesseract5";

        private static readonly string[] list_of_dlls = new string[]
        {
                @"\x86\" + libtesseract + ".dll",
                @"\x86\" + liblept + ".dll",
                @"\x64\" + libtesseract + ".dll",
                @"\x64\" + liblept + ".dll",
                @"\Tesseract.dll"
        };

        private static readonly string[] list_of_checksums = new string[]
        {
                "a87ba6ac613b8ecb5ed033e57b871e6f",     //  x86/tesseract50
                "e62f9ef3dd31df439fa2a37793b035db",     //  x86/leptonica-1.82.0
                "446370b590a3c14e0fda0a2029b8e6fa",     //  x64/tesseract50
                "2813455700fb7c1bc09738ca56ae7da7",     //  x64/leptonica-1.82.0
                "528d4d1eb0e07cfe1370b592da6f49fd"      //  Tesseract
        };

        private static readonly string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private static readonly string libs_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/libs";
        private static readonly string tesseract_hotlink_prefix = libs_hotlink_prefix + @"/" + libtesseract + @"/";
        private static string tesseract_hotlink_platform_specific_prefix;
        private static readonly string app_data_tesseract_catalog = appPath + @"\" + tesseract_version_folder;

        private static readonly InitialDialogue dialogue = new InitialDialogue();
        public static CancellationTokenSource stopDownloadTask;
        public static string build_version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static void CleanLegacyTesseractIfNeeded()
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

            string thisprocessname = Process.GetCurrentProcess().ProcessName;
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
            {
                using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "]   Duplicate process found - start canceled. Version: " + version);
                }
                MessageBox.Show("Another instance of WFInfo is already running, close it and try again", "WFInfo V" + version);
                return;
            }

            Directory.CreateDirectory(app_data_tesseract_catalog);
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x86");
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x64");

            // TrainedData folder/content handled by TesseractService

            CleanLegacyTesseractIfNeeded();
            CollectDebugInfo();
            tesseract_hotlink_platform_specific_prefix = tesseract_hotlink_prefix;

            int filesNeeded = 0;
            for (int i = 0; i < list_of_dlls.Length; i++)
            {
                string dll = list_of_dlls[i];
                string path = app_data_tesseract_catalog + dll;
                string md5 = list_of_checksums[i];
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
                TesseractEnviornment.CustomSearchPath = app_data_tesseract_catalog;
                App.Main();
            }

        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            AddLog("MyHandler caught: " + e.Message);
            AddLog("Runtime terminating: " + args.IsTerminating);
            AddLog(e.StackTrace);
            AddLog(e.InnerException.Message);
            AddLog(e.InnerException.StackTrace);

        }

        public static void AddLog(string argm)
        { //write to the debug file, includes version and UTCtime
            Debug.WriteLine(argm);
            Directory.CreateDirectory(appPath);
            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                sw.WriteLineAsync("[" + DateTime.UtcNow + " - Still in custom entrypoint]   " + argm);
        }

        public static WebClient CreateNewWebClient()
        {
            WebProxy proxy = null;
            String proxy_string = Environment.GetEnvironmentVariable("http_proxy");
            if (proxy_string != null)
            {
                proxy = new WebProxy(new Uri(proxy_string));
            }
            WebClient webClient = new WebClient() { Proxy = proxy };
            webClient.Headers.Add("User-Agent", "WFInfo/" + build_version);
            return webClient;
        }

        public static void CollectDebugInfo()
        {
            // Redownload if DLL is not present or got corrupted
            using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
            {
                sw.WriteLineAsync("--------------------------------------------------------------------------------------------------------------------------------------------");

                try
                {
                    ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
                    foreach (ManagementObject mo in mos.Get().Cast<ManagementObject>())
                    {
                        sw.WriteLineAsync("[" + DateTime.UtcNow + "] CPU model is " + mo["Name"]);
                    }
                }
                catch (Exception e)
                {
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "] Unable to fetch CPU model due to:" + e);
                }

                //Log OS version
                sw.WriteLineAsync("[" + DateTime.UtcNow + $"] Detected Windows version: {Environment.OSVersion}");

                //Log .net Version
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\")) {
                    try
                    {
                        int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                        if (true)
                        {
                            sw.WriteLineAsync("[" + DateTime.UtcNow + $"] Detected .net version: {CheckFor45DotVersion(releaseKey)}");
                        }
                    }
                    catch (Exception e)
                    {
                        sw.WriteLineAsync("[" + DateTime.UtcNow + $"] Unable to fetch .net version due to: {e}");
                    }

                }

                //Log C++ x64 runtimes 14.29
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32).OpenSubKey("Installer\\Dependencies")) {
                    try
                    {
                        foreach (var item in ndpKey.GetSubKeyNames()) // VC,redist.x64,amd64,14.30,bundle
                        {
                            if (item.Contains("VC,redist.x64,amd64"))
                            {
                                sw.WriteLineAsync("[" + DateTime.UtcNow + $"] {ndpKey.OpenSubKey(item).GetValue("DisplayName")}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        sw.WriteLineAsync("[" + DateTime.UtcNow + $"] Unable to fetch x64 runtime due to: {e}");
                    }

                }

                //Log C++ x86 runtimes 14.29
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32).OpenSubKey("Installer\\Dependencies")) {
                    try
                    {
                        foreach (var item in ndpKey.GetSubKeyNames()) // VC,redist.x86,x86,14.30,bundle
                        {
                            if (item.Contains("VC,redist.x86,x86"))
                            {
                                sw.WriteLineAsync("[" + DateTime.UtcNow + $"] {ndpKey.OpenSubKey(item).GetValue("DisplayName")}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        sw.WriteLineAsync("[" + DateTime.UtcNow + $"] Unable to fetch x86 runtime due to: {e}");
                    }
                }
            }
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
            WebClient webClient = CreateNewWebClient();
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

        private static async void RefreshTesseractDlls(CancellationToken token)
        {
            WebClient webClient = CreateNewWebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
            token.Register(webClient.CancelAsync);

            for (int i = 0; i < list_of_dlls.Length; i++)
            {
                if (token.IsCancellationRequested)
                    break;
                string dll = list_of_dlls[i];
                string path = app_data_tesseract_catalog + dll;
                string md5 = list_of_checksums[i];
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

        // From: https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed

        // Checking the version using >= will enable forward compatibility,  
        // however you should always compile your code on newer versions of 
        // the framework to ensure your app works the same. 
        private static string CheckFor45DotVersion(int releaseKey) {
            if (releaseKey >= 528040) {
                return "4.8 or later";
            }
            if (releaseKey >= 461808) {
                return "4.7.2 or later";
            }
            if (releaseKey >= 461308) {
                return "4.7.1 or later";
            }
            if (releaseKey >= 460798) {
                return "4.7 or later";
            }
            if (releaseKey >= 394802) {
                return "4.6.2 or later";
            }
            if (releaseKey >= 394254) {
                return "4.6.1 or later";
            }
            if (releaseKey >= 393295) {
                return "4.6 or later";
            }
            if (releaseKey >= 393273) {
                return "4.6 RC or later";
            }
            if ((releaseKey >= 379893)) {
                return "4.5.2 or later";
            }
            if ((releaseKey >= 378675)) {
                return "4.5.1 or later";
            }
            if ((releaseKey >= 378389)) {
                return "4.5 or later";
            }
            // This line should never execute. A non-null release key should mean 
            // that 4.5 or later is installed. 
            return "No 4.5 or later version detected";
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";
            if (assemblyName.CultureInfo != null && !assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture))
                path = string.Format(@"{0}\{1}", assemblyName.CultureInfo, path);

            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);  // Ensures full stream is read safely
                    return Assembly.Load(memoryStream.ToArray());
                }
            }
        }
    }


}

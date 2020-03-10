using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;

namespace WFInfo
{
    public class CustomEntrypoint
    {
        public static string appPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        public static string tesseract_version_folder { get; } = "tesseract4";

        [STAThreadAttribute]
        public static void Main()
        {
            Directory.CreateDirectory(appPath);
            RefreshTesseractDlls();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve_Tesseract;
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            App.Main();
        }

        // Detect if CPU has necessary optimizations
        public static bool HasAvxSupport()
        {
            try
            {
                return (GetEnabledXStateFeatures() & 4) != 0;
            }
            catch
            {
                return false;
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern long GetEnabledXStateFeatures();
        //

        private static void RefreshTesseractDlls(string libtesseract = "libtesseract400", string liblept = "liblept1760")
        {
            string tesseract_hotlink_prefix;
            if (HasAvxSupport())
            {
                tesseract_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/master/WFInfo/lib";
            } else
            {
                using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                {
                    sw.WriteLineAsync("--------------------------------------------------------------------------------------------");
                    sw.WriteLineAsync("--------------------------------------------------------------------------------------------");
                    sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + "CPU doesn't support AVX optimizations, falling back to SSE2");
                }
                // SSE2 version without AVX optimizations - for very old pre-2013 CPUs
                tesseract_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/vb-archive/WFInfo/lib";
            }

            string app_data_tesseract_catalog = appPath + @"\" + tesseract_version_folder;
            Directory.CreateDirectory(app_data_tesseract_catalog);
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x86");
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x64");

            WebClient webClient = new WebClient();

            List<String> list_of_dlls = new List<String>()
            {
                @"\x86\" + libtesseract + ".dll",
                @"\x86\" + liblept + ".dll",
                @"\x64\" + libtesseract + ".dll",
                @"\x64\" + liblept + ".dll",
                @"\Tesseract.dll"
            };

            foreach (var dll in list_of_dlls)
            {
                if (!File.Exists(app_data_tesseract_catalog + dll))
                {
                    bool success = false;
                    try
                    {
                        if (Directory.Exists("lib") && File.Exists("lib" + dll))
                        {
                            File.Copy("lib" + dll, app_data_tesseract_catalog + dll);
                            success = true;
                            Directory.Delete("lib" + dll);
                        }
                    }
                    catch (Exception ex)
                    {
                        using (StreamWriter sw = File.AppendText(appPath + @"\debug.log"))
                        {
                            sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + dll + " couldn't be moved");
                            sw.WriteLineAsync("[" + DateTime.UtcNow + "]   " + ex.ToString());
                        }
                    }

                    if (!success)
                        webClient.DownloadFile(tesseract_hotlink_prefix + dll.Replace("\\", "/"), app_data_tesseract_catalog + dll);
                }
            }

            if (Directory.Exists("lib") && Directory.GetFiles("lib").Length == 0)
                Directory.Delete("lib", true);

            webClient.Dispose();
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

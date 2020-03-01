using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;

namespace WFInfo
{
    public class CustonEntrypoint
    {
        public static string appPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        public static string tesseract_version_folder { get; } = "tesseract4";

        [STAThreadAttribute]
        public static void Main()
        {
            RefreshTesseractDlls();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve_Tesseract;
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            App.Main();
        }

        private static void RefreshTesseractDlls(string libtesseract = "libtesseract400", string liblept = "liblept1760")
        {
            string tesseract_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/master/WFInfo/lib";
            string app_data_tesseract_catalog = appPath + @"\" + tesseract_version_folder;
            Directory.CreateDirectory(app_data_tesseract_catalog);
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x86");
            Directory.CreateDirectory(app_data_tesseract_catalog + @"\x64");

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
                    if (Directory.Exists("lib") && File.Exists("lib" + dll))
                    {
                        Directory.Move("lib" + dll, app_data_tesseract_catalog + dll);
                    } else
                    {
                        //AddLog("Trained english data is not present in appData and locally, downloading it.");
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(tesseract_hotlink_prefix + dll.Replace("\\", "/"), app_data_tesseract_catalog + dll);
                    }
                }
            }

            if (Directory.Exists("lib"))
            {
                Directory.Delete("lib", true);
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve_Tesseract(object sender, ResolveEventArgs args)
        {
            var probingPath = appPath + @"\" + tesseract_version_folder;
            var assyName = new AssemblyName(args.Name);

            var newPath = Path.Combine(probingPath, assyName.Name);
            if (!newPath.EndsWith(".dll"))
            {
                newPath = newPath + ".dll";
            }
            if (File.Exists(newPath))
            {
                var assy = Assembly.LoadFile(newPath);
                return assy;
            }
            return null;
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";
            if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }

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

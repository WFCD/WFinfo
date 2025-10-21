using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Tesseract;
using WFInfo.Settings;

namespace WFInfo
{
    public interface ITesseractService
    {
        /// <summary>
        /// Inventory/Profile engine
        /// </summary>
        TesseractEngine FirstEngine { get; }

        /// <summary>
        /// Second slow pass engine
        /// </summary>
        TesseractEngine SecondEngine { get; }

        /// <summary>
        /// Engines for parallel processing the reward screen and snapit
        /// </summary>
        TesseractEngine[] Engines { get; }

        void Init();
        void ReloadEngines();
    }

    /// <summary>
    /// Holds all the TesseractEngine instances and is responsible for loadind/reloading them
    /// They are all configured in the same way
    /// </summary>
    public class TesseractService : ITesseractService
    {
        /// <summary>
        /// Inventory/Profile engine
        /// </summary>
        public TesseractEngine FirstEngine { get; private set; }
        /// <summary>
        /// Second slow pass engine
        /// </summary>
        public TesseractEngine SecondEngine { get; private set; }
        /// <summary>
        /// Engines for parallel processing the reward screen and snapit
        /// </summary>
        public TesseractEngine[] Engines { get; } = new TesseractEngine[4];

        private static string Locale => ApplicationSettings.GlobalReadonlySettings.Locale;
        private static readonly string ApplicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private static readonly string NormalDataPath = ApplicationDirectory + @"\tessdata";
        private static readonly string FallbackDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\WFInfo" + @"\tessdata";
        private string DataPath;

        public TesseractService()
        {
            Directory.CreateDirectory(NormalDataPath);
            DataPath = NormalDataPath;
            getLocaleTessdata();
            try
            {
                FirstEngine = CreateEngine();
            }
            catch (TesseractException) 
            {
                // Tesseract doesn't like characters from non-english languages in the file path to tessdata.
                // Since we store those in %appdata% and that contains the username, we sometimes get issues with that.
                // In such cases, we copy the tessdata to a different file path to circumvent the issue.
                Main.AddLog("Exception during first engine creation, Switching to fallback path: " + FallbackDataPath);
                DirectoryInfo fallbackDir = Directory.CreateDirectory(FallbackDataPath);
                FileInfo[] normalDirFiles = new DirectoryInfo(NormalDataPath).GetFiles();

                // Delete any files that exist within fallback location, but not in the normal location
                FileInfo[] fallbackExtraFiles = fallbackDir.GetFiles().Where(fallbackFile => normalDirFiles.All(normalFile => normalFile.Name != fallbackFile.Name)).ToArray();
                foreach (FileInfo extraFile in fallbackExtraFiles)
                {
                    extraFile.Delete();
                }

                // Copy files from normal location to fallback location
                foreach (FileInfo file in normalDirFiles)
                {
                    string newFullName = Path.Combine(fallbackDir.FullName, file.Name);

                    // if file is missing or is different, copy it over (with overwrite)
                    if (!File.Exists(newFullName) 
                        ||  CustomEntrypoint.GetMD5hash(newFullName) != CustomEntrypoint.GetMD5hash(file.FullName))
                    {
                        file.CopyTo(newFullName, true);
                    }
                }

                DataPath = FallbackDataPath;

                FirstEngine = CreateEngine();
            }
            SecondEngine = CreateEngine();
        }

        private TesseractEngine CreateEngine() => new TesseractEngine(DataPath, Locale)
        {
            DefaultPageSegMode = PageSegMode.SingleBlock
        };
        
        public void Init()
        {
            LoadEngines();
        }

        private void LoadEngines()
        {
            for (var i = 0; i < 4; i++)
            {
                Engines[i]?.Dispose();
                Engines[i] = CreateEngine();
            }
        }

        public void ReloadEngines()
        {
            getLocaleTessdata();
            LoadEngines();
            FirstEngine?.Dispose();
            FirstEngine = CreateEngine();
            SecondEngine?.Dispose();
            SecondEngine = CreateEngine();
        }
        private void getLocaleTessdata()
        {
            string traineddata_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/libs/tessdata/";
            JObject traineddata_checksums = new JObject
            {
                {"en", "7af2ad02d11702c7092a5f8dd044d52f"},
                {"de", "f5488b7c3186e822e0e6c5c05c1aaf1f"},
                {"ko", "c776744205668b7e76b190cc648765da"}
            };

            // get trainned data
            string traineddata_hotlink = traineddata_hotlink_prefix + Locale + ".traineddata";
            string app_data_traineddata_path = NormalDataPath + @"\" + Locale + ".traineddata";
            string curr_data_traineddata_path = DataPath + @"\" + Locale + ".traineddata";

            WebClient webClient = CustomEntrypoint.CreateNewWebClient();

            if (!File.Exists(app_data_traineddata_path) || CustomEntrypoint.GetMD5hash(app_data_traineddata_path) != traineddata_checksums.GetValue(Locale).ToObject<string>())
            {
                try
                {
                    webClient.DownloadFile(traineddata_hotlink, app_data_traineddata_path);
                    // We download to normal data path. If current data path differs, copy it to there too
                    if (curr_data_traineddata_path != app_data_traineddata_path)
                    {
                        File.Copy(app_data_traineddata_path, curr_data_traineddata_path, true);
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
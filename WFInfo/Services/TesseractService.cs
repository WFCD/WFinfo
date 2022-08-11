using System;
using System.IO;
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
        private static string AppdataTessdataFolder => CustomEntrypoint.appdata_tessdata_folder;
        private static readonly string ApplicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private static readonly string DataPath = ApplicationDirectory + @"\tessdata";

        public TesseractService()
        {
            getLocaleTessdata();
            FirstEngine = CreateEngine();
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
                {"ko", "c776744205668b7e76b190cc648765da"},
                {"ru", "2e2022eddce032b754300a8188b41419"}
            };

            // get trainned data
            string traineddata_hotlink = traineddata_hotlink_prefix + Locale + ".traineddata";
            string app_data_traineddata_path = AppdataTessdataFolder + @"\" + Locale + ".traineddata";

            WebClient webClient = new WebClient();

            if (!File.Exists(app_data_traineddata_path) || CustomEntrypoint.GetMD5hash(app_data_traineddata_path) != traineddata_checksums.GetValue(Locale).ToObject<string>())
            {
                try
                {
                    webClient.DownloadFile(traineddata_hotlink, app_data_traineddata_path);
                }
                catch (Exception) { }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Tesseract;
using WFInfo.Settings;
using WFInfo.LanguageProcessing;

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

        /// <summary>
        /// Dedicated numbers-only engine for item counting (avoids race conditions)
        /// </summary>
        TesseractEngine NumbersOnlyEngine { get; }

        void Init();
        void ReloadEngines();
    }

    /// <summary>
    /// Holds all TesseractEngine instances and is responsible for loadind/reloading them
    /// They are all configured with language-specific character whitelists to reduce noise
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
        /// Engines for parallel processing of reward screen and snapit
        /// </summary>
        public TesseractEngine[] Engines { get; } = new TesseractEngine[4];
        /// <summary>
        /// Dedicated numbers-only engine for item counting (avoids race conditions with FirstEngine)
        /// </summary>
        public TesseractEngine NumbersOnlyEngine { get; private set; }

        private static string Locale => ApplicationSettings.GlobalReadonlySettings.Locale;
        private static readonly string ApplicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private static readonly string NormalDataPath = ApplicationDirectory + @"\tessdata";
        private static readonly string FallbackDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\WFInfo" + @"\tessdata";
        private string DataPath;

        // Fallback whitelist for unknown locales
        private const string DefaultWhitelist = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        
        // Numbers-only whitelist for item counting
        private const string NumbersOnlyWhitelist = "0123456789";

        public TesseractService()
        {
            Directory.CreateDirectory(NormalDataPath);
            DataPath = NormalDataPath;
        }

        private TesseractEngine CreateEngine()
        {
            var engine = new TesseractEngine(DataPath, Locale, EngineMode.LstmOnly);
            try
            {
                // Apply universal OCR improvements for all languages
                engine.SetVariable("tessedit_zero_rejection", "false");
                engine.SetVariable("tessedit_write_rep_codes", "false");
                engine.SetVariable("tessedit_write_unlv", "false");
                engine.SetVariable("tessedit_fix_fuzzy_spaces", "true");
                engine.SetVariable("tessedit_prefer_joined_broken", "false");
                engine.SetVariable("preserve_interword_spaces", "1");
                engine.SetVariable("language_model_penalty_case_ok", "0.1");
                engine.SetVariable("language_model_penalty_case_bad", "0.4");
                engine.SetVariable("thresholding_method", "0");

                // CJK-specific optimizations
                if (Locale == "ko" || Locale == "zh-hans" || Locale == "zh-hant")
                {
                    engine.SetVariable("textord_noise_normratio", "2.0");
                    engine.SetVariable("chop_enable", "0");
                    engine.SetVariable("use_new_state_cost", "1");
                    engine.SetVariable("load_system_dawg", "true");
                    engine.SetVariable("load_freq_dawg", "true");
                    engine.SetVariable("language_model_penalty_non_dict_word", "0");
                    engine.SetVariable("user_defined_dpi", "300");
                    engine.SetVariable("segment_nonalphabetic_script", "1");
                }
                else if (Locale == "en")
                {
                    engine.SetVariable("load_system_dawg", "false");
                    engine.SetVariable("load_freq_dawg", "false");
                    engine.SetVariable("user_defined_dpi", "300");
                    engine.SetVariable("textord_noise_normratio", "1.0");
                }

                // Character whitelist from language processor
                string whitelist;
                try
                {
                    var processor = LanguageProcessorFactory.GetProcessor(Locale);
                    whitelist = processor?.CharacterWhitelist ?? DefaultWhitelist;
                }
                catch (InvalidOperationException)
                {
                    whitelist = DefaultWhitelist;
                }
                engine.SetVariable("tessedit_char_whitelist", whitelist);

                return engine;
            }
            catch (Exception)
            {
                engine.Dispose();
                throw;
            }
        }

        private TesseractEngine CreateNumbersOnlyEngine()
        {
            var engine = new TesseractEngine(DataPath, Locale, EngineMode.LstmOnly);
            try
            {
                engine.SetVariable("tessedit_char_whitelist", NumbersOnlyWhitelist);
                engine.SetVariable("tessedit_zero_rejection", "false");
                engine.SetVariable("preserve_interword_spaces", "0");
                return engine;
            }
            catch (Exception)
            {
                engine.Dispose();
                throw;
            }
        }
        
        public void Init()
        {
            // Dispose existing engines before re-creating
            DisposeEngines();

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
                    extraFile.Delete();

                // Copy files from normal location to fallback location
                foreach (FileInfo file in normalDirFiles)
                {
                    string newFullName = Path.Combine(fallbackDir.FullName, file.Name);
                    if (!File.Exists(newFullName)
                        || CustomEntrypoint.GetMD5hash(newFullName) != CustomEntrypoint.GetMD5hash(file.FullName))
                    {
                        file.CopyTo(newFullName, true);
                    }
                }

                DataPath = FallbackDataPath;
                FirstEngine = CreateEngine();
            }
            try
            {
                SecondEngine = CreateEngine();
                NumbersOnlyEngine = CreateNumbersOnlyEngine();
            }
            catch (Exception ex)
            {
                Main.AddLog($"Failed to create secondary engines: {ex.Message}");
                FirstEngine?.Dispose();
                FirstEngine = null;
                throw;
            }
            LoadEngines();
        }

        private void DisposeEngines()
        {
            FirstEngine?.Dispose();
            FirstEngine = null;
            SecondEngine?.Dispose();
            SecondEngine = null;
            NumbersOnlyEngine?.Dispose();
            NumbersOnlyEngine = null;
            for (var i = 0; i < Engines.Length; i++)
            {
                Engines[i]?.Dispose();
                Engines[i] = null;
            }
        }

        private void LoadEngines()
        {
            for (var i = 0; i < Engines.Length; i++)
                Engines[i] = CreateEngine();
        }

        public void ReloadEngines()
        {
            DisposeEngines();
            DataPath = NormalDataPath;
            getLocaleTessdata();
            try
            {
                try
                {
                    FirstEngine = CreateEngine();
                }
                catch (TesseractException)
                {
                    DataPath = FallbackDataPath;
                    FirstEngine = CreateEngine();
                }
                SecondEngine = CreateEngine();
                NumbersOnlyEngine = CreateNumbersOnlyEngine();
                LoadEngines();
            }
            catch (Exception)
            {
                // Dispose any engines created before failure to match Init() cleanup pattern
                DisposeEngines();
                throw;
            }
        }
        
        private void getLocaleTessdata()
        {
            string traineddata_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/libs/tessdata/";
            JObject traineddata_checksums = new JObject
            {
                {"en", "7af2ad02d11702c7092a5f8dd044d52f"},
                {"ko", "c776744205668b7e76b190cc648765da"},
                {"fr", "ac0a3da6bf50ed0dab61b46415e82c17"},
                {"uk", "fe1312cbfb602fc179796dbf54ee65fe"},
                {"it", "401cd425084217b224f99c3f55c78518"},
                {"de", "d37aac5fce1c7d8f279a42f076c935d8"},
                {"es", "130215a6355e9ea651f483279271d354"},
                {"pt", "9627fa0ccecdc9dfdb9ac232bbbd744f"},
                {"pl", "33bb3c504011b839cf6e2b689ea68578"},
                //{"tr", "df810a344d6725b2ee3e76682de5a86b"}, - cannot be supported until WFM supports it
                {"ru", "2e2022eddce032b754300a8188b41419"},
                //{"ja", "synthetic_md5_japanese"}, - cannot be supported until WFM supports it
                {"zh-hans", "921bdf9c27a17ce5c7c77c10345ad8fb"},
                {"zh-hant", "5865dded9ef6d035c165fb14317f1402"},
                //{"th", "synthetic_md5_thai"} - cannot be supported until WFM supports it
            };

            // get trainned data
            string traineddata_hotlink = traineddata_hotlink_prefix + Locale + ".traineddata";
            string app_data_traineddata_path = NormalDataPath + @"\" + Locale + ".traineddata";
            string curr_data_traineddata_path = DataPath + @"\" + Locale + ".traineddata";

            using (var webClient = CustomEntrypoint.CreateNewWebClient())
            {
            // Check if locale is supported before accessing checksums
            if (traineddata_checksums.TryGetValue(Locale, out JToken checksumToken))
            {
                string expectedChecksum = checksumToken.ToObject<string>();
                
                if (!File.Exists(app_data_traineddata_path) || CustomEntrypoint.GetMD5hash(app_data_traineddata_path) != expectedChecksum)
                {
                    try
                    {
                        webClient.DownloadFile(traineddata_hotlink, app_data_traineddata_path);
                    }
                    catch (Exception ex)
                    {
                        Main.AddLog($"Failed to download traineddata for locale '{Locale}': {ex.Message}. Source: {traineddata_hotlink}, Target: {app_data_traineddata_path}");
                        // Don't throw during initialization to allow service to continue with existing data
                    }
                }
                // Always ensure the active tessdata is in sync when paths differ
                if (curr_data_traineddata_path != app_data_traineddata_path
                    && File.Exists(app_data_traineddata_path)
                    && (!File.Exists(curr_data_traineddata_path)
                        || CustomEntrypoint.GetMD5hash(curr_data_traineddata_path) != CustomEntrypoint.GetMD5hash(app_data_traineddata_path)))
                {
                    File.Copy(app_data_traineddata_path, curr_data_traineddata_path, true);
                }
            }
            else
            {
                // Unsupported locale - skip download and log warning
                Main.AddLog($"Unsupported locale '{Locale}' - no traineddata checksum available, skipping download");
            }
            } // end using webClient
        }
    }
}
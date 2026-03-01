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

        void Init();
        void ReloadEngines();
        
        /// <summary>
        /// Sets the FirstEngine to numbers-only mode for item counting
        /// </summary>
        void SetNumbersOnlyMode();
        
        /// <summary>
        /// Resets the FirstEngine to its default language-specific whitelist
        /// </summary>
        void ResetToDefaultMode();
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

        private TesseractEngine CreateEngine()
        {
            //Main.AddLog($"Creating Tesseract engine for locale: '{Locale}'");
            var engine = new TesseractEngine(DataPath, Locale);

            // Apply universal OCR improvements for all languages
            
            // This causes crash
            //engine.SetVariable("tessedit_reject_mode", "1"); // Reject questionable characters
            //engine.SetVariable("textord_heavy_nr", "1");  // Enable heavy noise reduction
            
            engine.SetVariable("tessedit_zero_rejection", "false"); // Don't force recognition of uncertain characters
            engine.SetVariable("tessedit_write_rep_codes", "false"); // Don't write rejection codes
            engine.SetVariable("tessedit_write_unlv", "false"); // Don't write UNLV format
            engine.SetVariable("tessedit_fix_fuzzy_spaces", "true"); // Fix spacing issues
            engine.SetVariable("tessedit_prefer_joined_broken", "false"); // Don't join broken characters
            engine.SetVariable("tessedit_font_id", "0"); // Use default font (Tesseract 5+)
            
            // Dictionary and spacing improvements for UI text
            engine.SetVariable("preserve_interword_spaces", "1"); // Preserve spacing for stable output
                     
            // Language model penalties that work across all languages
            engine.SetVariable("language_model_penalty_case_ok", "0.1"); // Small penalty for case mismatches
            engine.SetVariable("language_model_penalty_case_bad", "0.4"); // Higher penalty for bad case
            
            // Thresholding parameters for better binarization (Tesseract 5+)
            engine.SetVariable("thresholding_method", "0"); // Use default thresholding
            engine.SetVariable("thresholding_window_size", "5"); // Smaller window for better noise reduction
            
            // Apply language-specific optimizations
            // CJK languages (Korean, Simplified Chinese, Traditional Chinese) share similar OCR challenges
            if (Locale == "ko" || Locale == "zh-hans" || Locale == "zh-hant")
            {
                // CJK-specific OCR improvements for better character recognition
                engine.SetVariable("textord_noise_normratio", "2.0"); // More aggressive noise reduction for CJK
                engine.SetVariable("chop_enable", "0"); // Disable character chopping for CJK characters
                engine.SetVariable("use_new_state_cost", "1"); // Use new state cost for better CJK recognition
                engine.SetVariable("load_system_dawg", "true"); // Enable system dictionary for better text segmentation
                engine.SetVariable("load_freq_dawg", "true"); // Enable frequency dictionary for better text segmentation
                engine.SetVariable("language_model_penalty_non_dict_word", "0"); // Don't penalize non-dictionary words (item names aren't dictionary words)
                engine.SetVariable("user_defined_dpi", "300"); // Improve recognition for scaled/filtered UI text
                engine.SetVariable("segment_nonalphabetic_script", "1"); // Better segmentation for non-alphabetic scripts
            }
            else if (Locale == "en")
            {
                // Aggressive settings for English to reduce noise
                engine.SetVariable("language_model_penalty_non_dict_word", "0.3"); // Penalize non-dictionary words heavily
                engine.SetVariable("load_system_dawg", "false"); // Disable system dictionary for better UI text recognition
                engine.SetVariable("load_freq_dawg", "false"); // Disable frequency dictionary for better UI text recognition
                engine.SetVariable("textord_force_make_prop_words", "true"); // Help with compound words
                
            }
            
            // Apply language-specific character whitelist from language processor
            var processor = LanguageProcessorFactory.GetProcessor(Locale);
            var whitelist = processor?.CharacterWhitelist ?? DefaultWhitelist;
            engine.SetVariable("tessedit_char_whitelist", whitelist);
            //Main.AddLog($"Tesseract whitelist for '{Locale}': '{whitelist}'");
            
            return engine;
        }
        
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
        
        public void SetNumbersOnlyMode()
        {
            FirstEngine?.SetVariable("tessedit_char_whitelist", NumbersOnlyWhitelist);
        }
        
        public void ResetToDefaultMode()
        {
            if (FirstEngine != null)
            {
                var processor = LanguageProcessorFactory.GetProcessor(Locale);
                var whitelist = processor?.CharacterWhitelist ?? DefaultWhitelist;
                FirstEngine.SetVariable("tessedit_char_whitelist", whitelist);
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

            WebClient webClient = CustomEntrypoint.CreateNewWebClient();

            // Check if locale is supported before accessing checksums
            if (traineddata_checksums.TryGetValue(Locale, out JToken checksumToken))
            {
                string expectedChecksum = checksumToken.ToObject<string>();
                
                if (!File.Exists(app_data_traineddata_path) || CustomEntrypoint.GetMD5hash(app_data_traineddata_path) != expectedChecksum)
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
            else
            {
                // Unsupported locale - skip download and log warning
                Main.AddLog($"Unsupported locale '{Locale}' - no traineddata checksum available, skipping download");
            }
        }
    }
}
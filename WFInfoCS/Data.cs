using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLua;

namespace WFInfoCS
{

    class Data
    {
        public Dictionary<string, string> market_items =
            new Dictionary<string, string>(); // Warframe.market item listing           {<id>: "<name>|<url_name>", ...}
        public JObject market_data; // Contains warframe.market ducatonator listing     {<partName>: {"ducats": <ducat_val>,"plat": <plat_val>}, ...}
        public JObject relic_data; // Contains relic_data from Warframe PC Drops        {<Era>: {"A1":{"vaulted": true,<rare1/uncommon[12]/common[123]>: <part>}, ...}, "Meso": ..., "Neo": ..., "Axi": ...}
        public JObject eqmt_data; // Contains eqmt_data from Warframe PC Drops          {<EQMT>: {"vaulted": true, "PARTS": {<NAME>:{"relic_name":<name>|"","count":<num>}, ...}},  ...}
        public JObject name_data; //C ontains relic to market name translation          {<relic_name>: <market_name>}

        private string ApplicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS";
        private string _marketItemsPath;
        private string _marketDataPath;
        private string _eqpmtDataPath;
        private string _relicDataPath;
        private string _nameDataPath;

        private int save_count = 0;
        WebClient WebClient;
        private FileSystemWatcher _screenshotWatcher = new FileSystemWatcher();
        private LogCapture _EElogWatcher;

        private Sheets _sheetsApi;
        private NLua.Lua _lua;

        public Data()
        {
            Main.AddLog("CREATING DATABASE");
            _marketItemsPath = ApplicationDirectory + @"\market_items.json";
            _marketDataPath = ApplicationDirectory + @"\market_data.json";
            _eqpmtDataPath = ApplicationDirectory + @"\eqmt_data.json";
            _relicDataPath = ApplicationDirectory + @"\relic_data.json";
            _nameDataPath = ApplicationDirectory + @"\name_data.json";

            Directory.CreateDirectory(ApplicationDirectory);

            WebClient = new WebClient();
            WebClient.Headers.Add("platform", "pc");
            WebClient.Headers.Add("language", "en");

            _sheetsApi = new Sheets();

            string warframePictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Warframe"; 
            Directory.CreateDirectory(warframePictures);
            _screenshotWatcher.Path = warframePictures;
            _screenshotWatcher.EnableRaisingEvents = true;

            _lua = new NLua.Lua();
            if (My.Settings.Auto) // WIP
            {
                Enable_LogCapture();
            }
        }

        private void Enable_LogCapture()
        {
            if (_EElogWatcher == null)
            {
                try
                {
                    _EElogWatcher = new LogCapture();
                    _EElogWatcher.TextChanged += log_Changed;
                }
                catch (Exception ex)
                {
                    Main.AddLog("FAILED TO START LogCapture");
                    // WIP - show on screen on main window that there's problem with log capture
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private void Disable_LogCapture()
        {
            if (_EElogWatcher != null)
            {
                _EElogWatcher.TextChanged -= log_Changed;
                _EElogWatcher.Dispose();
                _EElogWatcher = null;
            }
        }

        private void Save_JObject(JObject data)
        {
            Main.AddLog("SAVING DEBUG JSON: debug" + save_count.ToString() + ".json");
            File.WriteAllText(Path.Combine(ApplicationDirectory + @"\debug" + save_count.ToString() + ".json"), JsonConvert.SerializeObject(data, Formatting.Indented));
            save_count += 1;
        }

        private void Save_JArray(JArray data)
        {
            Main.AddLog("SAVING DEBUG JSON: debug" + save_count.ToString() + ".json");
            File.WriteAllText(Path.Combine(ApplicationDirectory + @"\debug" + save_count.ToString() + ".json"), JsonConvert.SerializeObject(data, Formatting.Indented));
            save_count += 1;
        }

        private void Save_Market()
        {
            Main.AddLog("SAVING MARKET DATABASE");
            File.WriteAllText(_marketItemsPath, JsonConvert.SerializeObject(market_items, Formatting.Indented));
            File.WriteAllText(_marketDataPath, JsonConvert.SerializeObject(market_data, Formatting.Indented));
        }

        private void Save_Relics()
        {
            Main.AddLog("SAVING RELIC DATABASE");
            File.WriteAllText(_relicDataPath, JsonConvert.SerializeObject(relic_data, Formatting.Indented));
        }

        private void Save_Names()
        {
            Main.AddLog("SAVING NAME DATABASE");
            File.WriteAllText(_nameDataPath, JsonConvert.SerializeObject(name_data, Formatting.Indented));
        }

        private void Save_Eqmt()
        {
            Main.AddLog("SAVING EQMT DATABASE");
            File.WriteAllText(_eqpmtDataPath, JsonConvert.SerializeObject(eqmt_data, Formatting.Indented));
        }

        ///
        ///
        /// WIP - the rest code is TBD
        ///
        /// 

        private void log_Changed(object sender, string line)
        {
            Console.WriteLine(line);
            if (line.Contains("Sys [Info]: Created /Lotus/Interface/ProjectionsCountdown.swf"))
            {
                Task.Factory.StartNew(Main.DoDelayWork()); // WIP
            }
        }
    }
}

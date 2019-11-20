using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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

        private string MarketItemsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\market_items.json";
        private string MarketDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\market_data.json";
        private string EqpmtDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\eqmt_data.json";
        private string RelicDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\relic_data.json";
        private string NameDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS\name_data.json";

        WebClient WebClient = new WebClient();
        private Sheets sheetsAPI;
        //private NLua.Lua Lua;
    }
}

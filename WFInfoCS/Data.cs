using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLua;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Application = System.Windows.Application;

namespace WFInfoCS
{

    class Data
    {
        public Dictionary<string, string> marketItems = new Dictionary<string, string>(); // Warframe.market item listing           {<id>: "<name>|<url_name>", ...}
        public JObject marketData; // Contains warframe.market ducatonator listing     {<partName>: {"ducats": <ducat_val>,"plat": <plat_val>}, ...}
        public JObject relicData; // Contains relicData from Warframe PC Drops        {<Era>: {"A1":{"vaulted": true,<rare1/uncommon[12]/common[123]>: <part>}, ...}, "Meso": ..., "Neo": ..., "Axi": ...}
        public JObject equipmentData; // Contains equipmentData from Warframe PC Drops          {<EQMT>: {"vaulted": true, "PARTS": {<NAME>:{"relic_name":<name>|"","count":<num>}, ...}},  ...}
        public JObject nameData; // Contains relic to market name translation          {<relic_name>: <market_name>}

        private readonly string applicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfoCS";
        private readonly string marketItemsPath;
        private readonly string marketDataPath;
        private readonly string eqmtDataPath;
        private readonly string relicDataPath;
        private readonly string nameDataPath;

        private readonly string officialLootTable = "https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html";
        private readonly string weaponRequirementWikiURL = "https://warframe.fandom.com/wiki/Special:Export/Module:Weapons/data";
        readonly WebClient WebClient;
        private readonly Sheets sheetsApi;
        private readonly Lua lua;
        private string githubVersion;

        private readonly FileSystemWatcher screenshotWatcher = new FileSystemWatcher();
        private LogCapture EElogWatcher;
        private readonly string currentDirectory = Directory.GetCurrentDirectory();

        public Data()
        {
            Main.AddLog("Creating database");
            marketItemsPath = applicationDirectory + @"\market_items.json";
            marketDataPath = applicationDirectory + @"\market_data.json";
            eqmtDataPath = applicationDirectory + @"\eqmt_data.json";
            relicDataPath = applicationDirectory + @"\relic_data.json";
            nameDataPath = applicationDirectory + @"\name_data.json";

            Directory.CreateDirectory(applicationDirectory);

            WebClient = new WebClient();
            WebClient.Headers.Add("platform", "pc");
            WebClient.Headers.Add("language", "en");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            sheetsApi = new Sheets();

            string warframePictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Warframe"; //outdated? Was old work around for user who couldn't activate the program
            Directory.CreateDirectory(warframePictures);
            screenshotWatcher.Path = warframePictures;
            screenshotWatcher.EnableRaisingEvents = true;

            lua = new NLua.Lua();
            //if (false) //My.Settings.Auto) // WIP
            //{
            //    EnableLogcapture();
            //}
        }

        private void EnableLogcapture()
        {
            if (EElogWatcher == null)
            {
                try
                {
                    EElogWatcher = new LogCapture();
                    EElogWatcher.TextChanged += LogChanged;
                }
                catch (Exception ex)
                {
                    Main.AddLog("Failed to start logcapture, exception: " + ex.ToString());
                    Main.StatusUpdate("Failed to start capturing log", 1);
                }
            }
        }

        private void DisableLogCapture()
        {
            if (EElogWatcher != null)
            {
                EElogWatcher.TextChanged -= LogChanged;
                EElogWatcher.Dispose();
                EElogWatcher = null;
            }
        }

        private void SaveJObject(JObject data)
        {
            Main.AddLog("Saving debug JSON: debug" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json");
            File.WriteAllText(Path.Combine(applicationDirectory + @"\debug" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json"), JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        private void SaveJArray(JArray data)
        {
            Main.AddLog("Saving debug JSON: debug" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json");
            File.WriteAllText(Path.Combine(applicationDirectory + @"\debug" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json"), JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        private void SaveMarket()
        {
            Main.AddLog("Saving market database");
            File.WriteAllText(marketItemsPath, JsonConvert.SerializeObject(marketItems, Formatting.Indented));
            File.WriteAllText(marketDataPath, JsonConvert.SerializeObject(marketData, Formatting.Indented));
        }

        private void SaveRelics()
        {
            Main.AddLog("Saving relic database");
            File.WriteAllText(relicDataPath, JsonConvert.SerializeObject(relicData, Formatting.Indented));
        }

        private void SaveNames()
        {
            Main.AddLog("Saving name database");
            File.WriteAllText(nameDataPath, JsonConvert.SerializeObject(nameData, Formatting.Indented));
        }

        private void SaveEquipment()
        {
            Main.AddLog("Saving equipment database");
            File.WriteAllText(eqmtDataPath, JsonConvert.SerializeObject(equipmentData, Formatting.Indented));
        }

        public int GetGithubVersion()
        {
            WebClient.Headers.Add("User-Agent", "WFCD");
            JObject github =
                JsonConvert.DeserializeObject<JObject>(
                    WebClient.DownloadString("https://api.github.com/repos/WFCD/WFInfo/releases/latest"));
            if (github.ContainsKey("tag_name"))
            {
                githubVersion = github["tag_name"].ToObject<string>();
                return Main.VersionToInteger(githubVersion);
            }
            return Main.VersionToInteger(Main.BuildVersion);
        }

        private void Download(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)req.GetResponse();
            string resUri = response.ResponseUri.AbsoluteUri;
            Main.AddLog("Downloading to " + currentDirectory + "/WFinfo" + githubVersion + ".exe");
            try
            {
                WebClient.DownloadFile(resUri, currentDirectory + "/WFInfonew.exe");
                FileStream fs = File.Create(currentDirectory + "/update.bat");
                byte[] info = new UTF8Encoding(true).GetBytes(
                    "timeout 2" + Environment.NewLine + "del WFInfo.exe" + Environment.NewLine + "rename WFInfonew.exe WFInfo.exe" +
                    Environment.NewLine + "start WFInfo.exe");
                fs.Write(info, 0, info.Length);
                fs.Close();
                Process.Start(currentDirectory + "/update.bat");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Main.AddLog("An error occured on Data.CS download(" + url + "), " + ex.ToString());
                Main.StatusUpdate("Couldn't download " + url + "Due to " + ex.ToString(), 1);
            }
        }

        private bool LoadItems(bool force = false)
        {
            if (!force && File.Exists(marketItemsPath))
            {
                if (marketItems == null)
                {
                    marketItems = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(marketItemsPath));
                }

                if (marketItems.TryGetValue("version", out _) && marketItems["version"] == Main.buildVersion)
                {
                    Main.AddLog("Item database is good");
                    return false;
                }
            }

            Main.AddLog("Item databse is loading new items");
            marketItems = new Dictionary<string, string>();

            IList<IList<object>> sheet = sheetsApi.GetSheet("items!A:C");
            foreach (IList<object> row in sheet)
            {
                string name = row[1].ToString();
                if (name.Contains("Prime "))
                {
                    marketItems[row[0].ToString()] = name + "|" + row[2].ToString();
                }
            }

            marketItems["version"] = Main.BuildVersion;
            Main.AddLog("Item database is good");
            return true;
        }

        private bool LoadMarket(bool force = false)
        {
            if (!force && File.Exists(marketDataPath))
            {
                if (marketData == null)
                {
                    marketData = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(marketDataPath));
                }

                if (marketData.TryGetValue("version", out JToken version) && (marketData["version"].ToObject<string>() == Main.BuildVersion))
                {
                    DateTime timestamp = marketData["timestamp"].ToObject<DateTime>();
                    DateTime dayAgo = DateTime.Now.AddDays(-1);
                    if (timestamp > dayAgo)
                    {
                        Main.AddLog("Platinum database is good");
                        return false;
                    }
                }
            }

            Main.AddLog("Platinum databse is loading new items");
            marketData = new JObject();

            IList<IList<object>> sheet = sheetsApi.GetSheet("prices!A:I");
            foreach (IList<object> row in sheet)
            {
                string name = row[0].ToString();
                if (name.Contains("Prime "))
                {
                    marketData[name] = new JObject
                    {
                        {"plat", double.Parse(row[8].ToString(), Main.culture)},
                        {"ducats", 0},
                        {"volume", int.Parse(row[4].ToString()) + int.Parse(row[6].ToString())}
                    };
                }
            }

            marketData["Forma Blueprint"] = new JObject
            {
                { "ducats", 0 },
                { "plat", 0 },
                { "volume", 0 },
            };

            marketData["timestamp"] = DateTime.Now.ToString("R");
            marketData["version"] = Main.BuildVersion;

            Main.AddLog("Platinum database is good");
            LoadDucats();
            if (force && relicData != null)
            {
                CheckDucats();
            }

            return true;
        }

        private void LoadDucats()
        {
            Main.AddLog("Ducat database is loading new items");
            JObject market_temp =
                JsonConvert.DeserializeObject<JObject>(
                    WebClient.DownloadString("https://api.warframe.market/v1/tools/ducats"));
            foreach (JToken elem in market_temp["payload"]["previous_day"])
            {
                if (!marketItems.TryGetValue(elem["item"].ToString(), out string item_name))
                {
                    Main.AddLog("Unknwon market id: " + elem["item"].ToObject<string>());
                }
                else
                {

                    item_name = item_name.Split('|')[0];
                    if (!marketData.TryGetValue(item_name, out _))
                    {
                        Main.AddLog("Missing item in MarketData: " + item_name);
                    }

                    if (item_name.Contains(" Set"))
                    {
                        LoadItems(true);
                    }
                    else
                    {
                        marketData[item_name]["ducats"] = elem["ducats"];
                    }
                }
            }

            Main.AddLog("Ducat database is good");
        }

        public void CheckDucats()
        {
            JObject job;
            List<string> needDucats = new List<string>();

            foreach (KeyValuePair<string, JToken> elem in marketData)
            {
                if (elem.Key.Contains("Prime"))
                {
                    job = elem.Value.ToObject<JObject>();
                    if (job["ducats"].ToObject<int>() == 0)
                    {
                        Console.WriteLine("A null value was found for: " + elem.Key);
                        needDucats.Add(elem.Key);
                    }
                }
            }

            foreach (KeyValuePair<string, JToken> era in relicData)
            {
                if (era.Key.Length < 5)
                {
                    foreach (KeyValuePair<string, JToken> relic in era.Value.ToObject<JObject>())
                    {
                        foreach (KeyValuePair<string, JToken> rarity in relic.Value.ToObject<JObject>())
                        {
                            string name = rarity.Value.ToObject<string>();
                            if (needDucats.Contains(name))
                            {
                                if (rarity.Key.Contains("rare"))
                                {
                                    marketData[name]["ducats"] = 100;
                                }
                                else if (rarity.Key.Contains("un"))
                                {
                                    marketData[name]["ducats"] = 45;
                                }
                                else
                                {
                                    marketData[name]["ducats"] = 15;
                                }

                                needDucats.Remove(name);
                                if (needDucats.Count == 0)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private double GetSetPlat(JObject job, bool unowned = false)
        {
            double ret = 0;
            foreach (KeyValuePair<string, JToken> kvp in job["parts"].ToObject<JObject>())
            {
                int count = kvp.Value["count"].ToObject<int>();
                int owned = kvp.Value["owned"].ToObject<int>();
                if (unowned)
                {
                    count -= owned;
                }

                if (marketData.TryGetValue(kvp.Key, out JToken temp))
                {
                    ret += count * temp["plat"].ToObject<double>();
                }
                else if (equipmentData.TryGetValue(kvp.Key, out temp))
                {
                    // Need to confirm that this adjusted logic won't cause recursive bomb
                    double plat = GetSetPlat(temp.ToObject<JObject>());
                    marketData[kvp.Key] = new JObject
                    {
                        { "ducats", 0 },
                        { "plat", plat },
                    };
                    SaveMarket();

                    ret += count * plat;
                }
            }

            return ret;
        }

        private void LoadMarketItem(string item_name, string url)
        {
            Main.AddLog("Load missing market item: " + item_name);

            JObject stats =
                JsonConvert.DeserializeObject<JObject>(
                    WebClient.DownloadString("https://api.warframe.market/v1/items/" + url + "/statistics"));
            stats = stats["payload"]["statistics_closed"]["90days"].Last.ToObject<JObject>();

            JObject ducats = JsonConvert.DeserializeObject<JObject>(
                WebClient.DownloadString("https://api.warframe.market/v1/items/" + url));
            ducats = ducats["payload"]["item"].ToObject<JObject>();
            string id = ducats["id"].ToObject<string>();
            ducats = ducats["items_in_set"].AsParallel().First(part => (string)part["id"] == id).ToObject<JObject>();
            string ducat;
            if (!ducats.TryGetValue("ducats", out JToken temp))
            {
                ducat = "0";
            }
            else
            {
                ducat = temp.ToObject<string>();
            }

            marketData[item_name] = new JObject
            {
                { "ducats", ducat },
                { "plat", stats["avg_price"] }
            };
        }

        private Boolean LoadDropData(Boolean force = false)
        {
            Main.AddLog("Loading drop database");
            WebRequest request;
            if (equipmentData == null)
            {
                if (File.Exists(eqmtDataPath))
                {
                    equipmentData = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(eqmtDataPath));
                }
                else
                {
                    equipmentData = new JObject();
                }
            }

            // Temp variable for using with TryGetValue
            if ((!force) && File.Exists(relicDataPath) && File.Exists(eqmtDataPath) && equipmentData.TryGetValue("version", out JToken temp) && equipmentData["version"].ToObject<string>() == Main.BuildVersion)
            {
                request = WebRequest.Create(officialLootTable);
                request.Method = "HEAD";
                using (WebResponse resp = request.GetResponse())
                {
                    // Move last_mod back one hour, so that it doesn't equal timestamp
                    DateTime lastModified = DateTime.Parse(resp.Headers.Get("Last-Modified")).AddHours(-1);

                    if (relicData == null)
                    {
                        relicData = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(relicDataPath));
                    }

                    if (nameData == null)
                    {
                        nameData =
                            JsonConvert.DeserializeObject<JObject>(File.ReadAllText(nameDataPath));
                    }

                    if (relicData.TryGetValue("timestamp", out temp) && equipmentData.TryGetValue("timestamp", out temp) && equipmentData["timestamp"].ToObject<string>() == relicData["timestamp"].ToObject<string>() && lastModified < relicData["timestamp"].ToObject<DateTime>())
                    {
                        return false;
                    }
                }
            }

            Main.AddLog("Loading new drop database");

            relicData = new JObject();
            nameData = new JObject();

            string dropData;
            request = WebRequest.Create(officialLootTable);
            using (WebResponse response = request.GetResponse())
            {
                relicData["timestamp"] = response.Headers.Get("Last-Modified");
                equipmentData["timestamp"] = response.Headers.Get("Last-Modified");

                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    dropData = reader.ReadToEnd();
                }
            }

            // Load Relic Info
            // Get table start + end locations
            // There's a bit of rocket science involved. Perhaps we should consider using native HTML parsing libraries like people use XML?
            int first = dropData.IndexOf("id=\"relicRewards\"");
            first = dropData.IndexOf("<table>", first);
            int last = dropData.IndexOf("</table>", first);

            // Loop through each row
            // Get start > while not at end > get last > parse > get start > goto while
            int index = dropData.IndexOf("<tr>", first);
            int rowEnd = 0;
            while (index < last && index != -1)
            {
                rowEnd = dropData.IndexOf("</tr>", index);
                string rowContent = dropData.Substring(index, rowEnd - index);

                if (rowContent.Contains("Relic") && rowContent.Contains("Intact"))
                {
                    rowContent = Regex.Replace(rowContent, "<[^>]+>|\\([^\\)]+\\)", "");
                    string[] split = rowContent.Split(' ');
                    string era = split[0];
                    string relic = split[1];
                    if (!relicData.TryGetValue(era, out _))
                    {
                        relicData[era] = new JObject();
                    }

                    // Will check if not vaulted in future
                    relicData[era][relic] = new JObject
                    {
                        {"vaulted", true}
                    };

                    int numberOfCommon = 1;
                    int numberOfUncommon = 1;
                    index = dropData.IndexOf("<tr", rowEnd);
                    rowEnd = dropData.IndexOf("</tr>", index);
                    rowContent = dropData.Substring(index, rowEnd - index);
                    while (!rowContent.Contains("blank-row"))
                    {
                        rowContent = rowContent.Replace("<tr><td>", "").Replace("</td>", "").Replace("td>", "");
                        split = rowContent.Split('<');
                        string name = split[0];
                        if (name.Contains("Kavasa"))
                        {
                            if (name.Contains("Kubrow"))
                            {
                                name = name.Replace("Kubrow ", "");
                            }
                            else
                            {
                                name = name.Replace("Prime", "Prime Collar");
                            }
                        }
                        else if (!name.Contains("Prime Blueprint") && !name.Contains("Forma"))
                        {
                            name = name.Replace(" Blueprint", "");
                        }

                        if (split[1].Contains("2."))
                        {
                            relicData[era][relic]["rare1"] = name;
                        }
                        else if (split[1].Contains("11"))
                        {
                            relicData[era][relic]["uncommon" + numberOfUncommon.ToString()] = name;
                            numberOfUncommon += 1;
                        }
                        else
                        {
                            relicData[era][relic]["common" + numberOfCommon.ToString()] = name;
                            numberOfCommon += 1;
                        }

                        string prime = name;
                        if (prime.IndexOf("Prime") != -1)
                        {
                            prime = prime.Substring(0, prime.IndexOf("Prime") + 5);
                            if (!equipmentData.TryGetValue(prime, out _))
                            {
                                equipmentData[prime] = new JObject
                                {
                                    {"parts", new JObject()},
                                    {"type", ""},
                                    {"vaulted", true}
                                };
                            }

                            JObject job = equipmentData[prime]["parts"].ToObject<JObject>();

                            if (!job.TryGetValue(name, out _))
                            {
                                job = new JObject
                                {
                                    {"count", 1},
                                    {"owned", 0},
                                    {"vaulted", true}
                                };
                                equipmentData[prime]["parts"][name] = job;
                            }

                            if (name.Contains("Harness"))
                            {
                                equipmentData[prime]["type"] = "Archwing";
                            }
                            else if (name.Contains("Chassis"))
                            {
                                equipmentData[prime]["type"] = "Warframe";
                            }
                            else if (name.Contains("Carapace") || name.Contains("Collar Blueprint"))
                            {
                                equipmentData[prime]["type"] = "Companion";
                            }
                        }

                        if (!nameData.ContainsKey(split[0]))
                        {
                            nameData[split[0]] = name;
                        }

                        index = dropData.IndexOf("<tr", rowEnd);
                        rowEnd = dropData.IndexOf("</tr>", index);
                        rowContent = dropData.Substring(index, rowEnd - index);
                    }
                }

                index = dropData.IndexOf("<tr>", rowEnd);
            }

            MarkAllEquipmentVaulted();

            // Find NOT Vauled Relics in Missions
            last = dropData.IndexOf("id=\"relicRewards\"");
            index = dropData.IndexOf("<tr>");
            while (index < last && index != -1)
            {
                rowEnd = dropData.IndexOf("</tr>", index);
                string result = dropData.Substring(index, rowEnd - index);
                index = result.IndexOf("Relic");
                if (index != -1)
                {
                    result = result.Substring(0, index - 1);
                    index = result.LastIndexOf(">") + 1;
                    result = result.Substring(index);
                    string[] split = result.Split(' ');
                    string era = split[0];
                    string relic = split[1];

                    if (relicData.TryGetValue(era, out _))
                    {
                        relicData[era][relic]["vaulted"] = false;
                        MarkEquipmentUnvaulted(era, relic);
                    }
                }

                index = dropData.IndexOf("<tr>", rowEnd);
            }

            GetSetValueStatus();
            equipmentData["version"] = Main.BuildVersion;
            return true;
        }

        private void MarkAllEquipmentVaulted()
        {
            foreach (KeyValuePair<string, JToken> kvp in equipmentData)
            {
                if (kvp.Key.Contains("Prime"))
                {
                    equipmentData[kvp.Key]["vaulted"] = true;
                    foreach (KeyValuePair<string, JToken> part in kvp.Value["parts"].ToObject<JObject>())
                    {
                        equipmentData[kvp.Key]["parts"][part.Key]["vaulted"] = true;
                    }
                }
            }
        }

        private void GetSetValueStatus()
        {
            foreach (KeyValuePair<string, JToken> keyValuePair in equipmentData)
            {
                if (keyValuePair.Key.Contains("Prime"))
                {
                    bool vaulted = false;
                    foreach (KeyValuePair<string, JToken> part in keyValuePair.Value["parts"].ToObject<JObject>())
                    {
                        if (part.Value["vaulted"].ToObject<bool>())
                        {
                            vaulted = true;
                            break;
                        }
                    }

                    equipmentData[keyValuePair.Key]["vaulted"] = vaulted;
                }
            }
        }

        private void MarkEquipmentUnvaulted(string era, string name)
        {
            JObject job = relicData[era][name].ToObject<JObject>();
            foreach (KeyValuePair<string, JToken> keyValuePair in job)
            {
                string str = keyValuePair.Value.ToObject<string>();
                if (str.IndexOf("Prime") != -1)
                {
                    // Cut the name of actual part without Prime prefix ??
                    string eqmt = str.Substring(0, str.IndexOf("Prime") + 5);
                    if (equipmentData.TryGetValue(eqmt, out JToken temp))
                    {
                        equipmentData[eqmt]["parts"][str]["vaulted"] = false;
                    }
                    else
                    {
                        Console.WriteLine("Cannot find: " + eqmt + " in equipmentData");
                    }
                }
            }
        }

        private Boolean LoadEquipmentRequirements(Boolean force = false)
        {
            // Load wiki data on prime eqmt requirements
            // Mainly weapons
            // WIP - perhaps should be rewritten to use warframe-items Github jsons

            if (!force)
            {
                DateTime timestamp = equipmentData["rqmts_timestamp"].ToObject<DateTime>();
                DateTime dayAgo = DateTime.Now.AddDays(-1);
                if (timestamp > dayAgo)
                {
                    Main.AddLog("Wiki database is good");
                    return false;
                }
            }

            Main.AddLog("Loading new wiki database");
            string data = WebClient.DownloadString(weaponRequirementWikiURL);
            int start = data.IndexOf("<timestamp>") + 11;
            int last = data.IndexOf("<", start);
            equipmentData["rqmts_timestamp"] = DateTime.Now.ToString("R");
            data = data.Substring(data.IndexOf("{", data.IndexOf("<text")));
            data = data.Substring(0, data.LastIndexOf("}") + 1);
            data = Regex.Replace(data, "&quot;", "\"");
            data = Regex.Replace(data, "&amp;", "&");

            LuaTable tempLua = (LuaTable)((LuaTable)lua.DoString("return " + data)[0])["Weapons"];
            Dictionary<Object, Object> dataDict = lua.GetTableDict(tempLua);


            foreach (KeyValuePair<string, JToken> keyValuePair in equipmentData)
            {
                if (!keyValuePair.Key.Contains("timestamp") && dataDict.ContainsKey(keyValuePair.Key))
                {
                    equipmentData[keyValuePair.Key]["type"] = JToken.FromObject(((LuaTable)tempLua[keyValuePair.Key])["Type"]);
                    Dictionary<string, int> temp = new Dictionary<string, int>();
                    // I just want to say I'm sorry

                    foreach (LuaTable part in ((LuaTable)((LuaTable)((LuaTable)tempLua[keyValuePair.Key])["Cost"])["Parts"]).Values)
                    {
                        if ((string)part["Type"] == "PrimePart")
                        {

                            foreach (KeyValuePair<string, JToken> relicPart in (JObject)keyValuePair.Value["parts"])
                            {
                                if (relicPart.Key.Contains(part["Name"].ToString()))
                                {
                                    equipmentData[keyValuePair.Key]["parts"][relicPart.Key]["count"] =
                                        JToken.FromObject(part["Count"]);
                                    break;
                                }
                            }
                        }
                        else if (part["Type"].ToString() == "Weapon" && part["Name"].ToString().Contains("Prime"))
                        {
                            if (!temp.ContainsKey(part["Name"].ToString()))
                            {
                                temp[part["Name"].ToString()] = 0;
                            }

                            temp[part["Name"].ToString()] += Convert.ToInt32(part["Count"]);
                        }

                        if (temp.Count() > 0)
                        {
                            foreach (KeyValuePair<string, int> entry in temp)
                            {
                                JObject job = equipmentData[keyValuePair.Key]["parts"].ToObject<JObject>();
                                if (!job.TryGetValue(entry.Key, out JToken tempOut))
                                {
                                    equipmentData[keyValuePair.Key]["parts"][entry.Key] = new JObject
                                    {
                                        {"owned", 0},
                                        {"vaulted", false}
                                    };
                                }
                                equipmentData[keyValuePair.Key]["parts"][entry.Key]["count"] = entry.Value;
                            }
                        }
                    }

                }
            }

            return true;
        }

        public bool Update()
        {
            Main.AddLog("Updating databaes");
            bool saveMarket = LoadItems() | LoadMarket();

            foreach (KeyValuePair<string, string> elem in marketItems)
            {
                if (elem.Key != "version")
                {
                    string[] split = elem.Value.Split('|');
                    string itemName = split[0];
                    string itemUrl = split[1];
                    if (!itemName.Contains(" Set") && !marketData.TryGetValue(itemName, out JToken tempOut))
                    {
                        LoadMarketItem(itemName, itemUrl);
                        saveMarket = true;
                    }
                }
            }

            Boolean saveDrop = LoadDropData();
            saveDrop = LoadEquipmentRequirements(saveDrop);
            if (saveDrop)
            {
                SaveEquipment();
                SaveRelics();
                SaveNames();
            }

            if (saveMarket || saveDrop)
            {
                CheckDucats();
                SaveMarket();
            }

            if (saveMarket || saveDrop)
            {
                Main.AddLog("Databases needed updates");
            }
            else
            {
                Main.AddLog("Databases did not need updates");
            }

            return saveMarket || saveDrop;
        }

        public void ForceMarketUpdate()
        {
            Main.AddLog("Forcing market update");
            LoadItems(true);
            LoadMarket(true);

            foreach (KeyValuePair<string, string> elem in marketItems)
            {
                if (elem.Key != "version")
                {
                    string[] split = elem.Value.Split('|');
                    string itemName = split[0];
                    string itemUrl = split[1];
                    if (!itemName.Contains(" Set") && !marketData.TryGetValue(itemName, out _))
                    {
                        LoadMarketItem(itemName, itemUrl);
                    }
                }
            }
            SaveMarket();
        }

        public void ForceEqmtUpdate()
        {
            Main.AddLog("Forcing equipment update");
            LoadDropData(true);
            LoadEquipmentRequirements(true);
            SaveEquipment();
            SaveRelics();
            SaveNames();
        }

        public void ForceWikiUpdate()
        {
            Main.AddLog("Forcing wiki update");
            LoadEquipmentRequirements(true);
            SaveEquipment();
        }

        public JArray GetPlatLive(string itemUrl)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            JObject stats = JsonConvert.DeserializeObject<JObject>(
                WebClient.DownloadString("https://api.warframe.market/v1/items/" + itemUrl + "/orders"));
            stopWatch.Stop();
            Console.WriteLine("Time taken to download all listings: " + stopWatch.ElapsedMilliseconds + "ms");

            stopWatch.Start();
            JArray sellers = new JArray();
            foreach (JToken listing in stats["payload"]["orders"])
            {
                if (listing["order_type"].ToObject<string>() == "buy" ||
                    listing["user"]["status"].ToObject<string>() == "offline")
                {
                    continue;
                }

                sellers.Add(listing);
            }
            stopWatch.Stop();
            Console.WriteLine("Time taken to process sell and online listings: " + stopWatch.ElapsedMilliseconds + "ms");
            Console.WriteLine(sellers);
            return sellers;
        }

        public Boolean IsPartVaulted(string name)
        {
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            return equipmentData[eqmt][name]["vaulted"].ToObject<Boolean>();
        }

        public string PartsOwned(string name)
        {
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            return equipmentData[eqmt]["parts"][name]["owned"].ToString() + "/" + equipmentData[eqmt]["parts"][name]["count"].ToString();
        }

        private void AddElement(int[,] d, List<int> xList, List<int> yList, int x, int y)
        {
            int loc = 0;
            int temp = d[x, y];
            while (loc < xList.Count && temp > d[xList[loc], yList[loc]])
            {
                loc += 1;
            }

            if (loc == xList.Count)
            {
                xList.Add(x);
                yList.Add(y);
                return;
            }

            xList.Insert(loc, x);
            yList.Insert(loc, y);
        }

        readonly char[,] ReplacementList = null;

        private int GetDifference(char c1, char c2)
        {
            if (c1 == c2 || c1 == '?' || c2 == '?')
            {
                return 0;
            }

            for (int i = 0; i < ReplacementList.GetLength(0) - 1; i++)
            {
                if ((c1 == ReplacementList[i, 0] || c2 == ReplacementList[i, 0]) &&
                    (c1 == ReplacementList[i, 1] || c2 == ReplacementList[i, 1]))
                {
                    return 0;
                }
            }

            return 1;
        }

        private int LevenshteinDistance(string s, string t)
        {
            // Levenshtein Distance determines how many character changes it takes to form a known result
            // For example: Nuvo Prime is closer to Nova Prime (2) then Ash Prime (4)
            // For more info see: https://en.wikipedia.org/wiki/Levenshtein_distance
            s = s.Replace("*", "").ToLower();
            t = t.Replace("*", "").ToLower();
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }


            for (int i = 0; i < n; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 0; j < m; j++)
            {
                d[0, j] = j;
            }

            for (int i = 1; i < n; i++)
            {
                for (int j = 1; j < m; j++)
                {
                    int cost;
                    if (t[j - 1] == s[i - 1])
                    {
                        cost = 0;
                    }
                    else
                    {
                        cost = 1;
                    }

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        private int LevenshteinDistanceSecond(string str1, string str2, int limit = -1)
        {
            int num;
            Boolean maxY;
            int temp;
            Boolean maxX;
            string s = str1.ToLower();
            string t = str2.ToLower();
            int n = s.Length;
            int m = t.Length;
            if (!(n == 0 || m == 0))
            {
                int[,] d = new int[n + 1 + 1 - 1, m + 1 + 1 - 1];
                List<int> activeX = new List<int>();
                List<int> activeY = new List<int>();
                d[0, 0] = 1;
                activeX.Add(0);
                activeY.Add(0);
                do
                {
                    int currX = activeX[0];
                    activeX.RemoveAt(0);
                    int currY = activeY[0];
                    activeY.RemoveAt(0);

                    temp = d[currX, currY];
                    if (limit != -1 && temp > limit)
                    {
                        return temp;
                    }

                    maxX = currX == n;
                    maxY = currY == m;
                    if (!maxX)
                    {
                        temp = d[currX, currY] + 1;
                        if (temp < d[currX + 1, currY] || d[currX + 1, currY] == 0)
                        {
                            d[currX + 1, currY] = temp;
                            AddElement(d, activeX, activeY, currX + 1, currY);
                        }
                    }

                    if (!maxY)
                    {
                        temp = d[currX, currY] + 1;
                        if (temp < d[currX, currY + 1] || d[currX, currY + 1] == 0)
                        {
                            d[currX, currY + 1] = temp;
                            AddElement(d, activeX, activeY, currX, currY + 1);
                        }
                    }

                    if (!maxX && !maxY)
                    {
                        temp = d[currX, currY] + GetDifference(s[currX], t[currY]);
                        if (temp < d[currX + 1, currY + 1] || d[currX + 1, currY + 1] == 0)
                        {
                            d[currX + 1, currY + 1] = temp;
                            AddElement(d, activeX, activeY, currX + 1, currY + 1);
                        }
                    }
                } while (!(maxX && maxY));

                num = d[n, m] - 1;
            }
            else
            {
                num = n + m;
            }

            return num;
        }

        public string GetPartName(string name)
        { // Checks the Levenshtein Distance of a string and returns the index in Names() of the closest part
            string lowest = null;
            int low = 9999;
            foreach (KeyValuePair<string, JToken> prop in nameData)
            {
                int val = LevenshteinDistance(prop.Key, name);
                if (val < low)
                {
                    low = val;
                    lowest = prop.Value.ToObject<string>();
                }
            }

            Main.AddLog("Found part: " + lowest + Environment.NewLine + Environment.NewLine + " from: " + name);
            return lowest;
        }

        public string GetSetName(string name)
        {
            name = name.ToLower();
            name = name.Replace("*", "");
            string result = null;
            int low = 9999;

            foreach (KeyValuePair<string, JToken> prop in marketData)
            {
                string str = prop.Key.ToLower();
                str = str.Replace("neuroptics", "");
                str = str.Replace("chassis", "");
                str = str.Replace("sytems", "");
                str = str.Replace("carapace", "");
                str = str.Replace("cerebrum", "");
                str = str.Replace("blueprint", "");
                str = str.Replace("harness", "");
                str = str.Replace("blade", "");
                str = str.Replace("pouch", "");
                str = str.Replace("barrel", "");
                str = str.Replace("receiver", "");
                str = str.Replace("stock", "");
                str = str.Replace("disc", "");
                str = str.Replace("grip", "");
                str = str.Replace("string", "");
                str = str.Replace("handle", "");
                str = str.Replace("ornament", "");
                str = str.Replace("wings", "");
                str = str.Replace("blades", "");
                str = str.Replace("hilt", "");
                str = str.TrimEnd();
                int val = LevenshteinDistance(str, name);
                if (val < low)
                {
                    low = val;
                    result = prop.Key;
                }
            }

            result = result.ToLower();
            result = result.Replace("neuroptics", "");
            result = result.Replace("chassis", "");
            result = result.Replace("sytems", "");
            result = result.Replace("carapace", "");
            result = result.Replace("cerebrum", "");
            result = result.Replace("blueprint", "");
            result = result.Replace("harness", "");
            result = result.Replace("blade", "");
            result = result.Replace("pouch", "");
            result = result.Replace("head", "");
            result = result.Replace("barrel", "");
            result = result.Replace("receiver", "");
            result = result.Replace("stock", "");
            result = result.Replace("disc", "");
            result = result.Replace("grip", "");
            result = result.Replace("string", "");
            result = result.Replace("handle", "");
            result = result.Replace("ornament", "");
            result = result.Replace("wings", "");
            result = result.Replace("blades", "");
            result = result.Replace("hilt", "");
            result = result.TrimEnd() + " set";
            result = Main.culture.TextInfo.ToTitleCase(result);
            return result;
        }

        public string GetRelicName(string string1)
        {
            string lowest = null;
            int low = 999;
            int temp = 0;
            string eraName = null;
            JObject job = null;

            foreach (KeyValuePair<string, JToken> era in relicData)
            {
                if (!era.Key.Contains("timestamp"))
                {
                    temp = LevenshteinDistanceSecond(string1, era.Key + "??RELIC", low);
                    if (temp < low)
                    {
                        job = era.Value.ToObject<JObject>();
                        eraName = era.Key;
                        low = temp;
                    }
                }
            }

            low = 999;
            foreach (KeyValuePair<string, JToken> relic in job)
            {
                temp = LevenshteinDistanceSecond(string1, eraName + relic.Key + "RELIC", low);
                if (temp < low)
                {
                    lowest = eraName + " " + relic.Key;
                    low = temp;
                }
            }

            return lowest;
        }

        private Boolean waiting = false;
        private void WatcherCreated(Object sender, FileSystemEventArgs e)
        {
            waiting = true;
        }

        private void WatcherChanged(Object sender, FileSystemEventArgs e)
        {
            if (waiting)
            {
                waiting = false;
                Thread.Sleep(500);
                OCR.ParseFile(e.FullPath);
            }
        }
        ///
        ///
        /// WIP - the rest code is TBD
        ///
        /// 

        private void LogChanged(object sender, string line)
        {
            Console.WriteLine(line);
            if (line.Contains("Sys [Info]: Created /Lotus/Interface/ProjectionsCountdown.swf"))
            {
                //Task.Factory.StartNew(Main.DoDelayWork()); // WIP
            }
        }
    }
}

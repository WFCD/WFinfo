using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WFInfo
{

    class Data
    {
        public JObject marketItems; // Warframe.market item listing           {<id>: "<name>|<url_name>", ...}
        public JObject marketData; // Contains warframe.market ducatonator listing     {<partName>: {"ducats": <ducat_val>,"plat": <plat_val>}, ...}
        public JObject relicData; // Contains relicData from Warframe PC Drops        {<Era>: {"A1":{"vaulted": true,<rare1/uncommon[12]/common[123]>: <part>}, ...}, "Meso": ..., "Neo": ..., "Axi": ...}
        public JObject equipmentData; // Contains equipmentData from Warframe PC Drops          {<EQMT>: {"vaulted": true, "PARTS": {<NAME>:{"relic_name":<name>|"","count":<num>}, ...}},  ...}
        public JObject nameData; // Contains relic to market name translation          {<relic_name>: <market_name>}

        private readonly string applicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private readonly string marketItemsPath;
        private readonly string marketDataPath;
        private readonly string eqmtDataPath;
        private readonly string relicDataPath;
        private readonly string nameDataPath;
        private readonly string debugDataPath;

        private readonly string filterAllJSON = "https://docs.google.com/uc?id=1zqI55GqcXMfbvZgBjASC34ad71GDTkta&export=download";

        readonly WebClient WebClient;
        private readonly Sheets sheetsApi;
        private string githubVersion;

        private LogCapture EElogWatcher;

        public Data()
        {
            Main.AddLog("Initializing Databases");
            marketItemsPath = applicationDirectory + @"\market_items.json";
            marketDataPath = applicationDirectory + @"\market_data.json";
            eqmtDataPath = applicationDirectory + @"\eqmt_data.json";
            relicDataPath = applicationDirectory + @"\relic_data.json";
            nameDataPath = applicationDirectory + @"\name_data.json";
            debugDataPath = applicationDirectory + @"\debug_data.json";

            Directory.CreateDirectory(applicationDirectory);

            WebClient = new WebClient();
            WebClient.Headers.Add("platform", "pc");
            WebClient.Headers.Add("language", "en");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            sheetsApi = new Sheets();
        }

        public void EnableLogcapture()
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
                    Main.AddLog("Failed to start logcapture, exception: " + ex);
                    Main.StatusUpdate("Failed to start capturing log", 1);
                }
            }
        }

        public void DisableLogCapture()
        {
            if (EElogWatcher != null)
            {
                EElogWatcher.TextChanged -= LogChanged;
                EElogWatcher.Dispose();
                EElogWatcher = null;
            }
        }

        private void SaveDatabase(string path, object db)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(db, Formatting.Indented));
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

        // Load item list from Sheets
        private void ReloadItems()
        {
            marketItems = new JObject();

            IList<IList<object>> sheet = sheetsApi.GetSheet("items!A:C");
            foreach (IList<object> row in sheet)
            {
                string name = row[1].ToString();
                if (name.Contains("Prime "))
                    marketItems[row[0].ToString()] = name + "|" + row[2].ToString();
            }

            marketItems["version"] = Main.BuildVersion;
            Main.AddLog("Item database has been downloaded");
        }

        // Load market data from Sheets
        private bool LoadMarket(bool force = false)
        {
            if (!force && File.Exists(marketDataPath) && File.Exists(marketItemsPath))
            {
                if (marketData == null)
                    marketData = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(marketDataPath));
                if (marketItems == null)
                    marketItems = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(marketItemsPath));

                if (marketData.TryGetValue("version", out JToken version) && (marketData["version"].ToObject<string>() == Main.BuildVersion))
                {
                    DateTime timestamp = marketData["timestamp"].ToObject<DateTime>();
                    if (timestamp > DateTime.Now.AddHours(-12))
                    {
                        Main.AddLog("Market Databases are up to date");
                        return false;
                    }
                }
            }
            ReloadItems();
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

            marketData["timestamp"] = DateTime.Now;
            marketData["version"] = Main.BuildVersion;

            Main.AddLog("Plat database has been downloaded");

            return true;
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

        private bool LoadEqmtData(bool force = false)
        {
            if (equipmentData == null)
                equipmentData = File.Exists(eqmtDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(eqmtDataPath)) : new JObject();
            if (relicData == null)
                relicData = File.Exists(relicDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(relicDataPath)) : new JObject();
            if (nameData == null)
                nameData = File.Exists(nameDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(nameDataPath)) : new JObject();

            // fill in equipmentData (NO OVERWRITE)
            // fill in nameData
            // fill in relicData

            JObject allFiltered = JsonConvert.DeserializeObject<JObject>(WebClient.DownloadString(filterAllJSON));

            DateTime filteredDate = allFiltered["timestamp"].ToObject<DateTime>().ToLocalTime().AddHours(-1);
            DateTime eqmtDate = equipmentData.TryGetValue("timestamp", out _) ? equipmentData["timestamp"].ToObject<DateTime>() : filteredDate;

            if (force || eqmtDate.CompareTo(filteredDate) <= 0)
            {
                equipmentData["timestamp"] = DateTime.Now;
                relicData = new JObject();
                relicData["timestamp"] = DateTime.Now;
                nameData = new JObject();
                nameData["Forma Blueprint"] = "Forma Blueprint";

                foreach (KeyValuePair<string, JToken> era in allFiltered["relics"].ToObject<JObject>())
                {
                    relicData[era.Key] = new JObject();
                    foreach (KeyValuePair<string, JToken> relic in era.Value.ToObject<JObject>())
                        relicData[era.Key][relic.Key] = relic.Value;
                }

                foreach (KeyValuePair<string, JToken> prime in allFiltered["eqmt"].ToObject<JObject>())
                {
                    string primeName = prime.Key.Substring(0, prime.Key.IndexOf("Prime") + 5);
                    if (!equipmentData.TryGetValue(primeName, out _))
                        equipmentData[primeName] = new JObject();
                    equipmentData[primeName]["vaulted"] = prime.Value["vaulted"];
                    equipmentData[primeName]["type"] = prime.Value["type"];

                    if (!equipmentData[primeName].ToObject<JObject>().TryGetValue("parts", out _))
                        equipmentData[primeName]["parts"] = new JObject();


                    foreach (KeyValuePair<string, JToken> part in prime.Value["parts"].ToObject<JObject>())
                    {
                        string partName = part.Key;
                        if (prime.Key.Contains("Collar"))
                        {
                            if (partName.Contains("Kubrow"))
                                partName = partName.Replace(" Kubrow", "");
                            else
                                partName = partName.Replace("Prime", "Prime Collar");
                        }
                        if (!equipmentData[primeName]["parts"].ToObject<JObject>().TryGetValue(partName, out _))
                            equipmentData[primeName]["parts"][partName] = new JObject();
                        if (!equipmentData[primeName]["parts"][partName].ToObject<JObject>().TryGetValue("owned", out _))
                            equipmentData[primeName]["parts"][partName]["owned"] = 0;
                        equipmentData[primeName]["parts"][partName]["vaulted"] = part.Value["vaulted"];
                        equipmentData[primeName]["parts"][partName]["count"] = part.Value["count"];
                        equipmentData[primeName]["parts"][partName]["ducats"] = part.Value["ducats"];


                        string gameName = part.Key;
                        if (prime.Value["type"].ToString() == "Archwing" && (part.Key.Contains("Systems") || part.Key.Contains("Harness") || part.Key.Contains("Wings")))
                        {
                            gameName += " Blueprint";
                        }
                        else if (prime.Value["type"].ToString() == "Warframes" && (part.Key.Contains("Systems") || part.Key.Contains("Neuroptics") || part.Key.Contains("Chassis")))
                        {
                            gameName += " Blueprint";
                        }
                        if (marketData.TryGetValue(partName, out _))
                        {
                            nameData[gameName] = partName;
                            marketData[partName]["ducats"] = Convert.ToInt32(part.Value["ducats"].ToString());
                        }
                    }
                }


                Main.AddLog("Prime Database has been downloaded");
                return true;
            }
            Main.AddLog("Prime Database is up to date");
            return false;
        }

        private void RefreshMarketDucats()
        {
            //equipmentData[primeName]["parts"][partName]["ducats"]
            foreach (KeyValuePair<string, JToken> prime in equipmentData)
                if (prime.Key != "timestamp")
                    foreach (KeyValuePair<string, JToken> part in equipmentData[prime.Key]["parts"].ToObject<JObject>())
                        if (marketData.TryGetValue(part.Key, out _))
                            marketData[part.Key]["ducats"] = Convert.ToInt32(part.Value["ducats"].ToString());
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

        private void GetSetVaultStatus()
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

        public bool Update()
        {
            Main.AddLog("Checking for Updates to Databases");
            bool saveDatabases = LoadMarket();

            foreach (KeyValuePair<string, JToken> elem in marketItems)
            {
                if (elem.Key != "version")
                {
                    string[] split = elem.Value.ToString().Split('|');
                    string itemName = split[0];
                    string itemUrl = split[1];
                    if (!itemName.Contains(" Set") && !marketData.TryGetValue(itemName, out _))
                    {
                        LoadMarketItem(itemName, itemUrl);
                        saveDatabases = true;
                    }
                }
            }
            Main.RunOnUIThread(() => { MainWindow.INSTANCE.Market_Data.Content = marketData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm"); });

            saveDatabases = LoadEqmtData(saveDatabases);
            Main.RunOnUIThread(() => { MainWindow.INSTANCE.Drop_Data.Content = equipmentData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm"); });

            if (saveDatabases)
                SaveAllJSONs();

            return saveDatabases;
        }

        public void ForceMarketUpdate()
        {
            try
            {
                Main.AddLog("Forcing market update");
                LoadMarket(true);

                foreach (KeyValuePair<string, JToken> elem in marketItems)
                {
                    if (elem.Key != "version")
                    {
                        string[] split = elem.Value.ToString().Split('|');
                        string itemName = split[0];
                        string itemUrl = split[1];
                        if (!itemName.Contains(" Set") && !marketData.TryGetValue(itemName, out _))
                        {
                            LoadMarketItem(itemName, itemUrl);
                        }
                    }
                }

                RefreshMarketDucats();

                SaveDatabase(marketItemsPath, marketItems);
                SaveDatabase(marketDataPath, marketData);
                Main.RunOnUIThread(() =>
                {
                    MainWindow.INSTANCE.Market_Data.Content = marketData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm");
                    Main.StatusUpdate("Market Update Complete", 0);
                    MainWindow.INSTANCE.ReloadDrop.IsEnabled = true;
                    MainWindow.INSTANCE.ReloadMarket.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Main.AddLog("Market Update Failed");
                Main.AddLog(ex.ToString());
                Main.StatusUpdate("Market Update Failed", 0);
                new ErrorDialogue(DateTime.Now, 0);
            }
        }

        public void SaveAllJSONs()
        {
            SaveDatabase(eqmtDataPath, equipmentData);
            SaveDatabase(relicDataPath, relicData);
            SaveDatabase(nameDataPath, nameData);
            SaveDatabase(marketItemsPath, marketItems);
            SaveDatabase(marketDataPath, marketData);
        }

        public void ForceEquipmentUpdate()
        {
            try
            {
                Main.AddLog("Forcing equipment update");
                LoadEqmtData(true);
                SaveAllJSONs();
                Main.RunOnUIThread(() =>
                {
                    MainWindow.INSTANCE.Drop_Data.Content = equipmentData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm");
                    Main.StatusUpdate("Prime Update Complete", 0);

                    MainWindow.INSTANCE.ReloadDrop.IsEnabled = true;
                    MainWindow.INSTANCE.ReloadMarket.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Main.AddLog("Prime Update Failed");
                Main.AddLog(ex.ToString());
                Main.StatusUpdate("Prime Update Failed", 0);
                new ErrorDialogue(DateTime.Now, 0);
            }
        }

        public bool IsPartVaulted(string name)
        {
            if (name.IndexOf("Prime") < 0)
                return false;
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            return equipmentData[eqmt]["parts"][name]["vaulted"].ToObject<bool>();
        }

        public string PartsOwned(string name)
        {
            if (name.IndexOf("Prime") < 0)
                return "";
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            string owned = equipmentData[eqmt]["parts"][name]["owned"].ToString();
            if (owned == "0")
                return "";
            return owned + "/" + equipmentData[eqmt]["parts"][name]["count"].ToString();
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

        public int GetDifference(char c1, char c2)
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

        public static int LevenshteinDistance(string s, string t)
        {
            // Levenshtein Distance determines how many character changes it takes to form a known result
            // For example: Nuvo Prime is closer to Nova Prime (2) then Ash Prime (4)
            // For more info see: https://en.wikipedia.org/wiki/Levenshtein_distance
            s = s.ToLower();
            t = t.ToLower();
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0 || m == 0)
                return n + m;

            d[0, 0] = 0;

            int count = 0;
            for (int i = 1; i <= n; i++)
                d[i, 0] = (s[i - 1] == ' ' ? count : ++count);

            count = 0;
            for (int j = 1; j <= m; j++)
                d[0, j] = (t[j - 1] == ' ' ? count : ++count);

            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                {
                    // deletion of s
                    int opt1 = d[i - 1, j];
                    if (s[i - 1] != ' ')
                        opt1++;

                    // deletion of t
                    int opt2 = d[i, j - 1];
                    if (t[j - 1] != ' ')
                        opt2++;

                    // swapping s to t
                    int opt3 = d[i - 1, j - 1];
                    if (t[j - 1] != s[i - 1])
                        opt3++;
                    d[i, j] = Math.Min(Math.Min(opt1, opt2), opt3);
                }



            return d[n, m];
        }

        public int LevenshteinDistanceSecond(string str1, string str2, int limit = -1)
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

        public string GetPartName(string name, out int low)
        { // Checks the Levenshtein Distance of a string and returns the index in Names() of the closest part
            string lowest = null;
            string lowest_unfiltered = null;
            low = 9999;
            foreach (KeyValuePair<string, JToken> prop in nameData)
            {
                int val = LevenshteinDistance(prop.Key, name);
                if (val < low)
                {
                    low = val;
                    lowest = prop.Value.ToObject<string>();
                    lowest_unfiltered = prop.Key;
                }
            }


            Main.AddLog("Found part(" + low + "): \"" + lowest_unfiltered + "\" from \"" + name + "\"");
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

        //private Boolean waiting = false;
        //private void WatcherCreated(Object sender, FileSystemEventArgs e)
        //{
        //    waiting = true;
        //}

        //private void WatcherChanged(Object sender, FileSystemEventArgs e)
        //{
        //    if (waiting)
        //    {
        //        waiting = false;
        //        Thread.Sleep(500);
        //        OCR.ParseFile(e.FullPath);
        //    }
        //}
        ///
        ///
        /// WIP - the rest code is TBD
        ///
        /// 

        private Task autoThread;

        private void LogChanged(object sender, string line)
        {
            if (autoThread == null || autoThread.IsCompleted)
            {
                if (autoThread != null)
                {
                    autoThread.Dispose();
                    autoThread = null;
                }

                if (line.Contains("Pause countdown done") || line.Contains("Got rewards"))
                    autoThread = Task.Factory.StartNew(AutoTriggered);
            }
        }

        public static void AutoTriggered()
        {
            try
            {
                var watch = Stopwatch.StartNew();
                long stop = watch.ElapsedMilliseconds + 5000;
                long wait = watch.ElapsedMilliseconds;

                OCR.UpdateWindow();
                int diff;

                while (watch.ElapsedMilliseconds < stop)
                {
                    if (watch.ElapsedMilliseconds > wait)
                    {
                        wait += Settings.autoDelay;
                        diff = OCR.GetThemeThreshold();
                        if (diff < 5)
                        {
                            while (watch.ElapsedMilliseconds < wait) ;
                            OCR.ProcessRewardScreen();
                            break;
                        }
                    }
                }
                watch.Stop();
            } catch(Exception ex)
            {
                Main.AddLog("AUTO FAILED");
                Main.AddLog(ex.ToString());
                Main.StatusUpdate("Auto Detection Failed", 0);
                new ErrorDialogue(DateTime.Now, 0);
            }
        }
    }
}

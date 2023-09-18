using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WFInfo.Services.WarframeProcess;
using WFInfo.Services.WindowInfo;
using WFInfo.Settings;

namespace WFInfo
{

    class Data
    {
        public JObject marketItems; // Warframe.market item listing           {<id>: "<name>|<url_name>", ...}
        public JObject marketData; // Contains warframe.market ducatonator listing     {<partName>: {"ducats": <ducat_val>,"plat": <plat_val>}, ...}
        public JObject relicData; // Contains relicData from Warframe PC Drops        {<Era>: {"A1":{"vaulted": true,<rare1/uncommon[12]/common[123]>: <part>}, ...}, "Meso": ..., "Neo": ..., "Axi": ...}
        public JObject equipmentData; // Contains equipmentData from Warframe PC Drops          {<EQMT>: {"vaulted": true, "PARTS": {<NAME>:{"relic_name":<name>|"","count":<num>}, ...}},  ...}
        public JObject nameData; // Contains relic to market name translation          {<relic_name>: <market_name>}

        private static List<Dictionary<int, List<int>>> korean = new List<Dictionary<int, List<int>>>() {
            new Dictionary<int, List<int>>() {
                { 0, new List<int>{ 6, 7, 8, 16 } }, // ㅁ, ㅂ, ㅃ, ㅍ
                { 1, new List<int>{ 2, 3, 4, 16, 5, 9, 10 } }, // ㄴ, ㄷ, ㄸ, ㅌ, ㄹ, ㅅ, ㅆ
                { 2, new List<int>{ 12, 13, 14 } }, // ㅈ, ㅉ, ㅊ
                { 3, new List<int>{ 0, 1, 15, 11, 18 } } // ㄱ, ㄲ, ㅋ, ㅇ, ㅎ
            },
            new Dictionary<int, List<int>>() {
                { 0, new List<int>{ 20, 5, 1, 7, 3, 19 } }, // ㅣ, ㅔ, ㅐ, ㅖ, ㅒ, ㅢ
                { 1, new List<int>{ 16, 11, 15, 10 } }, // ㅟ, ㅚ, ㅞ, ㅙ
                { 2, new List<int>{ 4, 0, 6, 2, 14, 9 } }, // ㅓ, ㅏ, ㅕ, ㅑ, ㅝ, ㅘ
                { 3, new List<int>{ 18, 13, 8, 17, 12 } } // ㅡ, ㅜ, ㅗ, ㅠ, ㅛ
            },
            new Dictionary<int, List<int>>() {
                { 0, new List<int>{ 16, 17, 18, 26 } }, // ㅁ, ㅂ, ㅄ, ㅍ
                { 1, new List<int>{ 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 19, 20, 25 } }, // ㄴ, ㄵ, ㄶ, ㄷ, ㄹ, ㄺ, ㄻ, ㄼ, ㄽ, ㄾ, ㄿ, ㅀ, ㅅ, ㅆ, ㅌ
                { 2, new List<int>{ 22, 23 } }, // ㅈ, ㅊ
                { 3, new List<int>{ 1, 2, 3, 24, 21, 27 } }, // ㄱ, ㄲ, ㄳ, ㅋ, ㅑ, ㅎ
                { 4, new List<int>{ 0 } }, // 
            }
        };

        private readonly string applicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
        private readonly string marketItemsPath;
        private readonly string marketDataPath;
        private readonly string equipmentDataPath;
        private readonly string relicDataPath;
        private readonly string nameDataPath;
        public string JWT; // JWT is the security key, store this as email+pw combo
        private readonly WebSocket marketSocket = new WebSocket("wss://warframe.market/socket?platform=pc");
        private readonly string filterAllJSON = "https://api.warframestat.us/wfinfo/filtered_items";
        private readonly string sheetJsonUrl = "https://api.warframestat.us/wfinfo/prices";
        public string inGameName = string.Empty;
        readonly HttpClient client;
        private string githubVersion;
        public bool rememberMe;
        private LogCapture EElogWatcher;
        private Task autoThread;

        private readonly IReadOnlyApplicationSettings _settings;
        private readonly IProcessFinder _process;
        private readonly IWindowInfoService _window;

        public WebClient createWfmClient()
        {
            WebClient webClient = CustomEntrypoint.createNewWebClient();
            webClient.Headers.Add("platform", "pc");
            webClient.Headers.Add("language", "en");
            return webClient;
        }

        public Data(IReadOnlyApplicationSettings settings, IProcessFinder process, IWindowInfoService window)
        {
            _settings = settings;
            _process = process;
            _window = window;

            Main.AddLog("Initializing Databases");
            marketItemsPath = applicationDirectory + @"\market_items.json";
            marketDataPath = applicationDirectory + @"\market_data.json";
            equipmentDataPath = applicationDirectory + @"\eqmt_data.json";
            relicDataPath = applicationDirectory + @"\relic_data.json";
            nameDataPath = applicationDirectory + @"\name_data.json";

            Directory.CreateDirectory(applicationDirectory);

            // Create websocket for WFM
            WebProxy proxy = null;
            String proxy_string = Environment.GetEnvironmentVariable("http_proxy");
            if (proxy_string != null)
            {
                proxy = new WebProxy(new Uri(proxy_string));
            }
            HttpClientHandler handler = new HttpClientHandler
            {
                Proxy = proxy
            };
            handler.UseCookies = false;
            client = new HttpClient(handler);

            marketSocket.SslConfiguration.EnabledSslProtocols = SslProtocols.None;
        }

        public void EnableLogCapture()
        {
            if (EElogWatcher == null)
            {
                try
                {
                    EElogWatcher = new LogCapture(_process);
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

        public bool IsJwtLoggedIn()
        {
            return JWT != null && JWT.Length > 300; //check if the token is of the right length
        }

        public int GetGithubVersion()
        {
            WebClient githubWebClient = CustomEntrypoint.createNewWebClient();
            JObject github =
                JsonConvert.DeserializeObject<JObject>(
                    githubWebClient.DownloadString("https://api.github.com/repos/WFCD/WFInfo/releases/latest"));
            if (github.ContainsKey("tag_name"))
            {
                githubVersion = github["tag_name"]?.ToObject<string>();
                return Main.VersionToInteger(githubVersion);
            }
            return Main.VersionToInteger(Main.BuildVersion);
        }

        // Load item list from Sheets
        public void ReloadItems()
        {
            marketItems = new JObject();
            WebClient webClient = createWfmClient();
            JObject obj =
                JsonConvert.DeserializeObject<JObject>(
                    webClient.DownloadString("https://api.warframe.market/v1/items"));

            JArray items = JArray.FromObject(obj["payload"]["items"]);
            foreach (var item in items)
            {
                string name = item["item_name"].ToString();
                if (name.Contains("Prime "))
                {
                    if ((name.Contains("Neuroptics") || name.Contains("Chassis") || name.Contains("Systems") || name.Contains("Harness") || name.Contains("Wings")))
                    {
                        name = name.Replace(" Blueprint", "");
                    }
                    marketItems[item["id"].ToString()] = name + "|" + item["url_name"];
                }
            }

            try
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v1/items"),
                    Method = HttpMethod.Get
                })
                {
                    request.Headers.Add("language", _settings.Locale);
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    var task = Task.Run(() => client.SendAsync(request));
                    task.Wait();
                    var response = task.Result;

                    var respTask = Task.Run(() => response.Content.ReadAsStringAsync());
                    respTask.Wait();
                    var body = respTask.Result;
                    var payload = JsonConvert.DeserializeObject<JObject>(body);
                    Debug.WriteLine(body);

                    obj = JsonConvert.DeserializeObject<JObject>(body);
                    items = JArray.FromObject(obj["payload"]["items"]);
                    foreach (var item in items)
                    {
                        string name = item["url_name"].ToString();
                        if (name.Contains("prime") && marketItems.ContainsKey(item["id"].ToString()))
                            marketItems[item["id"].ToString()] = marketItems[item["id"].ToString()] + "|" + item["item_name"];
                    }
                }
            }
            catch (Exception e)
            {
                Main.AddLog("GetTopListings: " + e.Message);
            }


            marketItems["version"] = Main.BuildVersion;
            Main.AddLog("Item database has been downloaded");
        }

        // Load market data from Sheets
        private bool LoadMarket(JObject allFiltered, bool force = false)
        {
            if (!force && File.Exists(marketDataPath) && File.Exists(marketItemsPath))
            {
                if (marketData == null)
                    marketData = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(marketDataPath));
                if (marketItems == null)
                    marketItems = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(marketItemsPath));

                if (marketData.TryGetValue("version", out _) && (marketData["version"].ToObject<string>() == Main.BuildVersion))
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
            WebClient webClient = CustomEntrypoint.createNewWebClient();
            JArray rows = JsonConvert.DeserializeObject<JArray>(webClient.DownloadString(sheetJsonUrl));

            foreach (var row in rows)
            {
                string name = row["name"].ToString();
                if (name.Contains("Prime "))
                {
                    if ((name.Contains("Neuroptics") || name.Contains("Chassis") || name.Contains("Systems") || name.Contains("Harness") || name.Contains("Wings")))
                    {
                       name = name.Replace(" Blueprint", "");
                    }
                    marketData[name] = new JObject
                    {
                        {"plat", double.Parse(row["custom_avg"].ToString(), Main.culture)},
                        {"ducats", 0},
                        {"volume", int.Parse(row["yesterday_vol"].ToString(), Main.culture) + int.Parse(row["today_vol"].ToString(), Main.culture)}
                    };
                }
            }

            // Add default values for ignored items
            foreach (KeyValuePair<string, JToken> ignored in allFiltered["ignored_items"].ToObject<JObject>())
            {
                marketData[ignored.Key] = ignored.Value;
            }

            marketData["timestamp"] = DateTime.Now;
            marketData["version"] = Main.BuildVersion;

            Main.AddLog("Plat database has been downloaded");

            return true;
        }

        private void LoadMarketItem(string item_name, string url)
        {
            Main.AddLog("Load missing market item: " + item_name);

            Thread.Sleep(333);
            WebClient webClient = createWfmClient();
            JObject stats =
                JsonConvert.DeserializeObject<JObject>(
                    webClient.DownloadString("https://api.warframe.market/v1/items/" + url + "/statistics"));
            JToken latestStats = stats["payload"]["statistics_closed"]["90days"].LastOrDefault();
            if (latestStats == null)
            {
                stats = new JObject
                {
                    { "avg_price", 999 },
                    { "volume", 0 }
                };
            } 
            else
            {
                stats = latestStats.ToObject<JObject>();
            }

            Thread.Sleep(333);
            webClient = createWfmClient();
            JObject ducats = JsonConvert.DeserializeObject<JObject>(
                webClient.DownloadString("https://api.warframe.market/v1/items/" + url));
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
                { "plat", stats["avg_price"] },
                { "volume", stats["volume"] }
            };
        }

        private bool LoadEqmtData(JObject allFiltered, bool force = false)
        {
            if (equipmentData == null)
                equipmentData = File.Exists(equipmentDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(equipmentDataPath)) : new JObject();
            if (relicData == null)
                relicData = File.Exists(relicDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(relicDataPath)) : new JObject();
            if (nameData == null)
                nameData = File.Exists(nameDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(nameDataPath)) : new JObject();

            // fill in equipmentData (NO OVERWRITE)
            // fill in nameData
            // fill in relicData

            DateTime filteredDate = allFiltered["timestamp"].ToObject<DateTime>().ToLocalTime().AddHours(-1);
            DateTime eqmtDate = equipmentData.TryGetValue("timestamp", out _) ? equipmentData["timestamp"].ToObject<DateTime>() : filteredDate;

            if (force || eqmtDate.CompareTo(filteredDate) <= 0)
            {
                equipmentData["timestamp"] = DateTime.Now;
                relicData["timestamp"] = DateTime.Now;
                nameData = new JObject();

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
                    if (!equipmentData[primeName].ToObject<JObject>().TryGetValue("mastered", out _))
                        equipmentData[primeName]["mastered"] = false;

                    if (!equipmentData[primeName].ToObject<JObject>().TryGetValue("parts", out _))
                        equipmentData[primeName]["parts"] = new JObject();


                    foreach (KeyValuePair<string, JToken> part in prime.Value["parts"].ToObject<JObject>())
                    {
                        string partName = part.Key;
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
                            marketData[partName]["ducats"] = Convert.ToInt32(part.Value["ducats"].ToString(), Main.culture);
                        }
                    }
                }

                // Add default values for ignored items
                foreach (KeyValuePair<string, JToken> ignored in allFiltered["ignored_items"].ToObject<JObject>())
                {
                    nameData[ignored.Key] = ignored.Key;
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
                            marketData[part.Key]["ducats"] = Convert.ToInt32(part.Value["ducats"].ToString(), Main.culture);
        }

        public bool Update()
        {
            Main.AddLog("Checking for Updates to Databases");
            WebClient webClient = CustomEntrypoint.createNewWebClient();
            JObject allFiltered = JsonConvert.DeserializeObject<JObject>(webClient.DownloadString(filterAllJSON));
            bool saveDatabases = LoadMarket(allFiltered);

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

            if (marketData["timestamp"] == null)
            {
                Main.RunOnUIThread(() => { MainWindow.INSTANCE.MarketData.Content = "VERIFY"; });
                Main.RunOnUIThread(() => { MainWindow.INSTANCE.DropData.Content = "TIME"; });

                return false;
            }

            Main.RunOnUIThread(() => { MainWindow.INSTANCE.MarketData.Content = marketData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm", Main.culture); });

            saveDatabases = LoadEqmtData(allFiltered, saveDatabases);
            Main.RunOnUIThread(() => { MainWindow.INSTANCE.DropData.Content = equipmentData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm", Main.culture); });

            if (saveDatabases)
                SaveAllJSONs();

            return saveDatabases;
        }

        public void ForceMarketUpdate()
        {
            try
            {
                Main.AddLog("Forcing market update");
                WebClient webClient = CustomEntrypoint.createNewWebClient();
                JObject allFiltered = JsonConvert.DeserializeObject<JObject>(webClient.DownloadString(filterAllJSON));
                LoadMarket(allFiltered, true);

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
                    MainWindow.INSTANCE.MarketData.Content = marketData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm", Main.culture);
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
                Main.RunOnUIThread(() =>
                {
                    _ = new ErrorDialogue(DateTime.Now, 0);
                });
            }
        }

        public void SaveAllJSONs()
        {
            SaveDatabase(equipmentDataPath, equipmentData);
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
                WebClient webClient = CustomEntrypoint.createNewWebClient();
                JObject allFiltered = JsonConvert.DeserializeObject<JObject>(webClient.DownloadString(filterAllJSON));
                LoadEqmtData(allFiltered, true);
                SaveAllJSONs();
                Main.RunOnUIThread(() =>
                {
                    MainWindow.INSTANCE.DropData.Content = equipmentData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm", Main.culture);
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
                Main.RunOnUIThread(() =>
                {
                    _ = new ErrorDialogue(DateTime.Now, 0);
                });
            }
        }

        public bool IsPartVaulted(string name)
        {
            if (name.IndexOf("Prime") < 0)
                return false;
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            return equipmentData[eqmt]["parts"][name]["vaulted"].ToObject<bool>();
        }

        public bool IsPartMastered(string name)
        {
            if (name.IndexOf("Prime") < 0)
                return false;
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            return equipmentData[eqmt]["mastered"].ToObject<bool>();
        }

        public string PartsOwned(string name)
        {
            if (name.IndexOf("Prime") < 0)
                return "0";
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            string owned = equipmentData[eqmt]["parts"][name]["owned"].ToString();
            if (owned == "0")
                return "0";
            return owned;
        }

        public string PartsCount(string name)
        {
            if (name.IndexOf("Prime") < 0)
                return "0";
            string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            string count = equipmentData[eqmt]["parts"][name]["count"].ToString();
            if (count == "0")
                return "0";
            return count;
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

        public int LevenshteinDistance(string s, string t)
        {
            switch (_settings.Locale)
            {
                case "ko":
                    // for korean
                    return LevenshteinDistanceKorean(s, t);
                default:
                    return LevenshteinDistanceDefault(s, t);
            }
        }

        public int LevenshteinDistanceDefault(string s, string t)
        {
            // Levenshtein Distance determines how many character changes it takes to form a known result
            // For example: Nuvo Prime is closer to Nova Prime (2) then Ash Prime (4)
            // For more info see: https://en.wikipedia.org/wiki/Levenshtein_distance
            s = s.ToLower(Main.culture);
            t = t.ToLower(Main.culture);
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

        public static bool isKorean(String str)
        {
            char c = str[0];
            if (0x1100 <= c && c <= 0x11FF) return true;
            if (0x3130 <= c && c <= 0x318F) return true;
            if (0xAC00 <= c && c <= 0xD7A3) return true;
            return false;
        }
        public string getLocaleNameData(string s)
        {
            bool saveDatabases = false;
            string localeName = "";
            foreach (var marketItem in marketItems)
            {
                if (marketItem.Key == "version")
                    continue;
                string[] split = marketItem.Value.ToString().Split('|');
                if (split[0] == s)
                {
                    if (split.Length == 3)
                    {
                        localeName = split[2];
                    }
                    else
                    {
                        localeName = split[0];
                    }
                    break;
                }
            }
            if (saveDatabases)
                SaveAllJSONs();
            return localeName;
        }
        private protected static string e = "A?s/,;j_<Z3Q4z&)";

        public int LevenshteinDistanceKorean(string s, string t)
        {
            // NameData s 를 한글명으로 가져옴
            s = getLocaleNameData(s);

            // i18n korean edit distance algorithm
            s = " " + s.Replace("설계도", "").Replace(" ", "");
            t = " " + t.Replace("설계도", "").Replace(" ", "");

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0 || m == 0)
                return n + m;
            int i, j;

            for (i = 1; i < s.Length; i++) d[i, 0] = i * 9;
            for (j = 1; j < t.Length; j++) d[0, j] = j * 9;

            int s1, s2;

            for (i = 1; i < s.Length; i++)
            {
                for (j = 1; j < t.Length; j++)
                {
                    s1 = 0;
                    s2 = 0;

                    char cha = s[i];
                    char chb = t[j];
                    int[] a = new int[3];
                    int[] b = new int[3];
                    a[0] = (((cha - 0xAC00) - (cha - 0xAC00) % 28) / 28) / 21;
                    a[1] = (((cha - 0xAC00) - (cha - 0xAC00) % 28) / 28) % 21;
                    a[2] = (cha - 0xAC00) % 28;

                    b[0] = (((chb - 0xAC00) - (chb - 0xAC00) % 28) / 28) / 21;
                    b[1] = (((chb - 0xAC00) - (chb - 0xAC00) % 28) / 28) % 21;
                    b[2] = (chb - 0xAC00) % 28;

                    if (a[0] != b[0] && a[1] != b[1] && a[2] != b[2])
                    {
                        s1 = 9;
                    }
                    else
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            if (a[k] != b[k])
                            {
                                if (GroupEquals(korean[k], a[k], b[k]))
                                {
                                    s2 += 1;
                                }
                                else
                                {
                                    s1 += 1;
                                }
                            }

                        }
                        s1 *= 3;
                        s2 *= 2;
                    }

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 9, d[i, j - 1] + 9), d[i - 1, j - 1] + s1 + s2);
                }
            }

            return d[s.Length - 1, t.Length - 1];
        }

        private bool GroupEquals(Dictionary<int, List<int>> group, int ak, int bk)
        {
            foreach (var entry in group)
            {
                if (entry.Value.Contains(ak) && entry.Value.Contains(bk))
                {
                    return true;
                }
            }
            return false;
        }

        public int LevenshteinDistanceSecond(string str1, string str2, int limit = -1)
        {
            int num;
            Boolean maxY;
            int temp;
            Boolean maxX;
            string s = str1.ToLower(Main.culture);
            string t = str2.ToLower(Main.culture);
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

        //public string ClosestAutoComplete(string searchQuery) {
        //	return GetPartNameHuman(searchQuery, out _);
        //}

        public string GetPartName(string name, out int low, bool suppressLogging, out bool multipleLowest)
        { // Checks the Levenshtein Distance of a string and returns the index in Names() of the closest part
            string lowest = null;
            string lowest_unfiltered = null;
            low = 9999;
            multipleLowest = false;
            foreach (KeyValuePair<string, JToken> prop in nameData)
            {
                int val = LevenshteinDistance(prop.Key, name);
                if (val < low)
                {
                    low = val;
                    lowest = prop.Value.ToObject<string>();
                    lowest_unfiltered = prop.Key;
                    multipleLowest = false;
                }
                else if (val == low)
                {
                    multipleLowest = true;
                }

                if (val == low && lowest.StartsWith("Gara") && prop.Key.StartsWith("Ivara")) //If both
                {
                    lowest = prop.Value.ToObject<string>();
                    lowest_unfiltered = prop.Key;
                }
            }

            if (!suppressLogging)
                Main.AddLog("Found part(" + low + "): \"" + lowest_unfiltered + "\" from \"" + name + "\"");
            return lowest;
        }

        public string GetPartNameHuman(string name, out int low)
        { // Checks the Levenshtein Distance of a string and returns the index in Names() of the closest part optimized for human searching
            string lowest = null;
            string lowest_unfiltered = null;
            low = 9999;
            foreach (KeyValuePair<string, JToken> prop in nameData)
            {
                if (prop.Value.ToString().ToLower(Main.culture).Contains(name.ToLower(Main.culture)))
                {
                    int val = LevenshteinDistance(prop.Value.ToString(), name);
                    if (val < low)
                    {
                        low = val;
                        lowest = prop.Value.ToObject<string>();
                        lowest_unfiltered = prop.Value.ToString();
                    }
                }
            }
            if (low > 10)
            {
                foreach (KeyValuePair<string, JToken> prop in nameData)
                {
                    int val = LevenshteinDistance(prop.Value.ToString(), name);
                    if (val < low)
                    {
                        low = val;
                        lowest = prop.Value.ToObject<string>();
                        lowest_unfiltered = prop.Value.ToString();
                    }

                }
            }
            Main.AddLog("Found part(" + low + "): \"" + lowest_unfiltered + "\" from \"" + name + "\"");
            return lowest;
        }

        public string GetSetName(string name)
        {
            string result = name.ToLower(Main.culture);

            if (result.Contains("kavasa"))
            {
                return "Kavasa Prime Kubrow Collar Set";
            }

            result = result.Replace("lower limb", "");
            result = result.Replace("upper limb", "");
            result = result.Replace("neuroptics", "");
            result = result.Replace("chassis", "");
            result = result.Replace("systems", "");
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
            result = result.Replace("link", "");
            result = result.TrimEnd();
            result = Main.culture.TextInfo.ToTitleCase(result);
            result += " Set";
            return result;
        }

        public string GetRelicName(string string1)
        {
            string lowest = null;
            int low = 999;
            int temp;
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

        private void LogChanged(object sender, string line)
        {
            if (autoThread != null && !autoThread.IsCompleted) return;
            if (autoThread != null)
            {
                autoThread.Dispose();
                autoThread = null;
            }

            if (line.Contains("Pause countdown done") || line.Contains("Got rewards"))
            {
                autoThread = Task.Factory.StartNew(AutoTriggered);
                Overlay.rewardsDisplaying = true;
            }

            //abort if autolist and autocsv disabled, or line doesn't contain end-of-session message or timer finished message
            if (!(line.Contains("MatchingService::EndSession") || line.Contains("Relic timer closed")) || ! (_settings.AutoList || _settings.AutoCSV || _settings.AutoCount)) return;

            if (Main.listingHelper.PrimeRewards == null || Main.listingHelper.PrimeRewards.Count == 0)
            {
                return;
            }

            Task.Run(async () =>
            {
                if (_settings.AutoList && inGameName.IsNullOrEmpty())
                    if (!await IsJWTvalid())
                    {
                        Disconnect();
                    }

                Overlay.rewardsDisplaying = false;
                string csv = "";
                Main.AddLog("Looping through rewards");
                Main.AddLog("AutoList: " + _settings.AutoList + ", AutoCSV: " + _settings.AutoCSV + ", AutoCount: " + _settings.AutoCount);
                foreach (var rewardscreen in Main.listingHelper.PrimeRewards)
                {
                    string rewards = "";
                    for (int i = 0; i < rewardscreen.Count; i++)
                    {
                        rewards += rewardscreen[i];
                        if (i + 1 < rewardscreen.Count)
                            rewards += " || ";
                    }
                    Main.AddLog(rewards + ", detected choice: " + Main.listingHelper.SelectedRewardIndex);


                    if (_settings.AutoCSV)
                    {
                        if (csv.Length == 0 && !File.Exists(applicationDirectory + @"\rewardExport.csv"))
                            csv += "Timestamp,ChosenIndex,Reward_0_Name,Reward_0_Plat,Reward_0_Ducats,Reward_1_Name,Reward_1_Plat,Reward_1_Ducats,Reward_2_Name,Reward_2_Plat,Reward_2_Ducats,Reward_3_Name,Reward_3_Plat,Reward_3_Ducats" + Environment.NewLine;
                        csv += DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ssff", Main.culture) + "," + Main.listingHelper.SelectedRewardIndex;
                        for (int i = 0; i < 4; i++)
                        {
                            if (i < rewardscreen.Count)
                            {
                                JObject job = Main.dataBase.marketData.GetValue(rewardscreen[i]).ToObject<JObject>();
                                string plat = job["plat"].ToObject<string>();
                                string ducats = job["ducats"].ToObject<string>();
                                csv += "," + rewardscreen[i] + "," + plat + "," + ducats;
                            }
                            else
                            {
                                csv += ",\"\",0,0"; //fill empty slots with "",0,0
                            }
                        }
                        csv += Environment.NewLine;
                    }

                    if (_settings.AutoCount)
                    {
                        Main.RunOnUIThread(() =>
                        {
                            Main.autoCount.viewModel.addItem(new AutoAddSingleItem(rewardscreen, Main.listingHelper.SelectedRewardIndex, Main.autoCount.viewModel));
                        });
                    }

                    if (_settings.AutoList)
                    {

                        var rewardCollection = Task.Run(() => Main.listingHelper.GetRewardCollection(rewardscreen)).Result;
                        if (rewardCollection.PrimeNames.Count == 0)
                            continue;

                        Main.listingHelper.ScreensList.Add(new KeyValuePair<string, RewardCollection>("", rewardCollection));
                    } else
                    {
                        Main.listingHelper.SelectedRewardIndex = 0; //otherwise done by GetRewardCollection, but that calls WFM API
                    }

                }

                if (_settings.AutoCount)
                {
                    Main.AddLog("Opening AutoCount interface");
                    Main.RunOnUIThread(() =>
                    {
                        AutoCount.ShowAutoCount();
                    });
                }

                if (_settings.AutoCSV)
                {
                    Main.AddLog("appending rewardExport.csv");
                    File.AppendAllText(applicationDirectory + @"\rewardExport.csv", csv);
                }

                if (_settings.AutoList)
                {
                    Main.AddLog("Opening AutoList interface");
                    Main.RunOnUIThread(() =>
                    {
                        if (Main.listingHelper.ScreensList.Count == 1)
                            Main.listingHelper.SetScreen(0);
                        Main.listingHelper.Show();
                        Main.listingHelper.Topmost = true;
                        Main.listingHelper.Topmost = false;
                    });
                }

                Main.AddLog("Clearing listingHelper.PrimeRewards");
                Main.RunOnUIThread(() =>
                {
                    Main.listingHelper.PrimeRewards.Clear();
                });

            });
        }

        public void AutoTriggered()
        {
            try
            {
                var watch = Stopwatch.StartNew();
                long stop = watch.ElapsedMilliseconds + 5000;
                long wait = watch.ElapsedMilliseconds;
                long fixedStop = watch.ElapsedMilliseconds + ApplicationSettings.GlobalReadonlySettings.FixedAutoDelay;

                _window.UpdateWindow();

                if (ApplicationSettings.GlobalReadonlySettings.ThemeSelection == WFtheme.AUTO)
                {
                    while (watch.ElapsedMilliseconds < stop)
                    {
                        if (watch.ElapsedMilliseconds <= wait) continue;
                        wait += ApplicationSettings.GlobalReadonlySettings.AutoDelay;
                        OCR.GetThemeWeighted(out double diff);
                        if (!(diff > 40)) continue;
                        while (watch.ElapsedMilliseconds < wait) ;
                        Main.AddLog("started auto processing");
                        OCR.ProcessRewardScreen();
                        break;
                    }
                } else
                {
                    while (watch.ElapsedMilliseconds < fixedStop) ;
                    Main.AddLog("started auto processing (fixed delay)");
                    OCR.ProcessRewardScreen();
                }
                watch.Stop();
            }
            catch (Exception ex)
            {
                Main.AddLog("AUTO FAILED");
                Main.AddLog(ex.ToString());
                Main.StatusUpdate("Auto Detection Failed", 0);
                Main.RunOnUIThread(() =>
                {
                    _ = new ErrorDialogue(DateTime.Now, 0);
                });
            }
        }

        /// <summary>
        ///	Get's the user's login JWT to authenticate future API calls.
        /// </summary>
        /// <param name="email">Users email</param>
        /// <param name="password">Users password</param>
        /// <exception cref="Exception">Connection exception JSON formated</exception>
        /// <returns>A task to be awaited</returns>
        public async Task GetUserLogin(string email, string password)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.warframe.market/v1/auth/signin"),
                Method = HttpMethod.Post,
            };
            var content = $"{{ \"email\":\"{email}\",\"password\":\"{password.Replace(@"\", @"\\")}\", \"auth_type\": \"header\"}}";
            request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", "JWT");
            request.Headers.Add("language", "en");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("platform", "pc");
            request.Headers.Add("auth_type", "header");
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            Regex rgxBody = new Regex("\"check_code\": \".*?\"");
            string censoredResponse = rgxBody.Replace(responseBody, "\"check_code\": \"REDACTED\"");
            Main.AddLog(censoredResponse);
            if (response.IsSuccessStatusCode)
            {
                SetJWT(response.Headers);
                await OpenWebSocket();
            }
            else
            {
                Regex rgxEmail = new Regex("[a-zA-Z0-9]");
                string censoredEmail = rgxEmail.Replace(email, "*");
                throw new Exception("GetUserLogin, " + responseBody + $"Email: {censoredEmail}, Pw length: {password.Length}");
            }
            request.Dispose();
        }

        /// <summary>
        /// Attempts to connect the user's account to the websocket
        /// </summary>
        /// <returns>A task to be awaited</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> OpenWebSocket()
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            Main.AddLog("Connecting to websocket");

            if (marketSocket.IsAlive)
            {
                return false;
            }

            marketSocket.OnMessage += (sender, e) =>
            {
                if (e.Data.Contains("@WS/ERROR")) // error checking, report back to main.status
                {
                    Main.AddLog(e.Data);
                    Disconnect();
                    Main.SignOut();
                }
            };

            marketSocket.OnMessage += (sender, e) =>
            {
                Debug.WriteLine("warframe.market: " + e.Data);
                if (!e.Data.Contains("SET_STATUS")) return;
                var message = JsonConvert.DeserializeObject<JObject>(e.Data);
                Main.RunOnUIThread(() =>
                {
                    Main.UpdateMarketStatus(message.GetValue("payload").ToString());
                });

            };

            marketSocket.OnOpen += (sender, e) =>
            {
                marketSocket.Send( (_process.IsRunning && !_process.GameIsStreamed)
                    ? "{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"ingame\"}"
                    : "{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"online\"}");
            };

            try
            {
                marketSocket.SetCookie(new WebSocketSharp.Net.Cookie("JWT", JWT));
                marketSocket.ConnectAsync();
            }
            catch (Exception e)
            {
                Main.AddLog("Unable to connect to socket: " + e.Message);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Sets the JWT to be used for future calls
        /// </summary>
        /// <param name="headers">Response headers from the original Login call</param>
        public void SetJWT(HttpResponseHeaders headers)
        {
            foreach (var item in headers)
            {
                if (!item.Key.ToLower(Main.culture).Contains("authorization")) continue;
                var temp = item.Value.First();
                JWT = temp.Substring(4);
                return;
            }
        }

        /// <summary>
        /// Lists an item under an account. Expected to be called after being logged in thus no login attempts.
        /// </summary>
        /// <param name="primeItem">Human friendly for prime item</param>
        /// <param name="platinum">The amount of platinum the user entered for the listing</param>
        /// <param name="quantity">The quantity of items the user listed.</param>
        /// <returns>The success of the method</returns>
        public async Task<bool> ListItem(string primeItem, int platinum, int quantity)
        {
            try
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v1/profile/orders"),
                    Method = HttpMethod.Post,
                })
                {
                    var itemId = PrimeItemToItemID(primeItem);
                    var json = $"{{\"order_type\":\"sell\",\"item_id\":\"{itemId}\",\"platinum\":{platinum},\"quantity\":{quantity}}}";
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", "JWT " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode) throw new Exception(responseBody);
                    SetJWT(response.Headers);
                    return true;
                }
            }
            catch (Exception e)
            {
                Main.AddLog($"ListItem: {e.Message} ");
                return false;
            }

        }

        /// <summary>
        /// Updates a listing with given variables
        /// </summary>
        /// <param name="listingId">The listingID of which the listing is going to be updated</param>
        /// <param name="platinum">The new platinum value</param>
        /// <param name="quantity">The new quantity</param>
        /// <returns>The success of the method</returns>
        public async Task<bool> UpdateListing(string listingId, int platinum, int quantity)
        {
            try
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v1/profile/orders/" + listingId),
                    Method = HttpMethod.Put,
                })
                {
                    var json = $"{{\"order_id\":\"{listingId}\", \"platinum\": {platinum}, \"quantity\":{quantity + 1}, \"visible\":true}}";
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", "JWT " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode) throw new Exception(responseBody);

                    SetJWT(response.Headers);
                    request.Dispose();
                }
                return true;
            }
            catch (Exception e)
            {
                Main.AddLog($"updateListing: {e.Message} ");
                return false;
            }
        }

        /// <summary>
        /// Converts the human friendly name to warframe.market's ID
        /// </summary>
        /// <param name="primeItem">Human friendly name of prime item</param>
        /// <returns>Warframe.market prime item ID</returns>
        public string PrimeItemToItemID(string primeItem)
        {
            foreach (var marketItem in marketItems)
            {
                if (marketItem.Value.ToString().Split('|').First().Equals(primeItem, StringComparison.OrdinalIgnoreCase))
                {
                    return marketItem.Key;
                }
            }
            throw new Exception($"PrimeItemToItemID, Prime item \"{primeItem}\" does not exist in marketItem");
        }

        /// <summary>
        /// Sets the status of WFM websocket. Will try to reconnect if it is not already connected.
        /// Accepts the following values:
        /// offline, set's player status to be hidden on the site.  
        /// online, set's player status to be online on the site.   
        /// in game, set's player status to be online and ingame on the site
        /// </summary>
        /// <param name="status">
        /// </param>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> SetWebsocketStatus(string status)
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            if (!IsJwtLoggedIn())
                return false;

            var message = "{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"";
            switch (status)
            {
                case "ingame":
                case "in game":
                    message += "ingame\"}";
                    break;
                case "online":
                    message += "online\"}";
                    break;
                default:
                    message += "invisible\"}";
                    break;
            }
            try
            {
                SendMessage(message);
            }
            catch (Exception e)
            {
                Main.AddLog("SetWebsocketStatus, Was unable to set status due to: " + e);
                throw;
            }
            return true;
        }

        /// <summary>
        /// Dummy method to make it so that you log send messages
        /// </summary>
        /// <param name="data">The JSON string of data being sent over websocket</param>
        private void SendMessage(string data)
        {
            Debug.WriteLine("Sending: " + data + " to websocket.");
            try
            {
                marketSocket.Send(data);
            }
            catch (InvalidOperationException e)
            {
                Debug.WriteLine($"Was unable to send message due to: {e}");
            }
        }
        /// <summary>
        /// Disconnects the user from websocket and sets JWT to null
        /// </summary>
        public void Disconnect()
        {
            if (marketSocket.ReadyState == WebSocketState.Open)
            { //only send disconnect message if the socket is connected
                SendMessage("{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"invisible\"}");
                JWT = null;
                rememberMe = false;
                inGameName = string.Empty;
                marketSocket.Close(1006);
            }
        }

        public string GetUrlName(string primeName)
        {
            foreach (var marketItem in marketItems)
            {
                string[] vals = marketItem.Value.ToString().Split('|');
                if (vals.Length > 2 && vals[0].Equals(primeName, StringComparison.OrdinalIgnoreCase))
                {
                    return vals[1];
                }
            }
            throw new Exception($"GetUrlName, Prime item \"{primeName}\" does not exist in marketItem");
        }

        /// <summary>
        /// Tries to get the top listings of a prime item
        /// </summary>
        /// <param name="primeName"></param>
        /// <returns></returns>
        public async Task<JObject> GetTopListings(string primeName) //https://api.warframe.market/v1/items/ prime_name /orders/top
        {
            var urlName = GetUrlName(primeName);

            try
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v1/items/" + urlName + "/orders/top"),
                    Method = HttpMethod.Get
                })
                {
                    request.Headers.Add("Authorization", "JWT " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");
                    var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    var payload = JsonConvert.DeserializeObject<JObject>(body);
                    if (body.Length < 3)
                        throw new Exception("No sell orders found: " + payload);
                    Debug.WriteLine(body);

                    return JsonConvert.DeserializeObject<JObject>(body);
                }
            }
            catch (Exception e)
            {
                Main.AddLog("GetTopListings: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Tries to get the profile page with the current JWT token
        /// </summary>
        /// <returns>bool of which answers the question "Is the user JWT valid?"</returns>
        public async Task<bool> IsJWTvalid()
        {
            if (JWT == null)
                return false;

            try
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v1/profile"),
                    Method = HttpMethod.Get,
                })
                {
                    request.Headers.Add("Authorization", "JWT " + JWT);
                    var response = await client.SendAsync(request);
                    SetJWT(response.Headers);
                    var profile = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
                    profile["profile"]["check_code"] = "REDACTED"; // remnove the code that can compromise an account.
                    Debug.WriteLine($"JWT check response: {profile["profile"]}");
                    return !(bool)profile["profile"]["anonymous"];
                }
            }
            catch (Exception e)
            {
                Main.AddLog($"IsJWTvalid: {e.Message} ");
                return false;
            }

        }

        /// <summary>
        /// Queries the current account for the amount of the CURRENT listed items
        /// To get the amount of a listing use:
        /// var listing = await Main.dataBase.GetCurrentListing(primeItem);
        /// var amount = (int) listing?["quantity"];
        /// To get the ID of a listing use:
        /// var listing = await Main.dataBase.GetCurrentListing(primeItem);
        /// var amount = (int) listing?["id"];
        /// </summary>
        /// <param name="primeName"></param>
        /// <returns>Quantity of prime named listed on the site</returns>
        public async Task<JToken> GetCurrentListing(string primeName)
        {
            try
            {
                if (inGameName.IsNullOrEmpty())
                {
                    await SetIngameName();
                }

                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v1/profile/" + inGameName + "/orders"),
                    Method = HttpMethod.Get
                })
                {
                    request.Headers.Add("Authorization", "JWT " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");
                    var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    var payload = JsonConvert.DeserializeObject<JObject>(body);
                    var sellOrders = (JArray)payload?["payload"]?["sell_orders"];
                    string itemID = PrimeItemToItemID(primeName);

                    if (sellOrders != null)
                    {
                        foreach (var listing in sellOrders)
                        {
                            if (itemID == (string)listing?["item"]?["id"])
                            {
                                request.Dispose();
                                return listing;
                            }
                        }

                        return null; //The requested item was not found, but don't throw
                    }
                    else
                    {
                        throw new Exception("No sell orders found: " + payload);
                    }
                }
            }
            catch (Exception e)
            {
                Main.AddLog("GetCurrentListing: " + e.Message);
                return null;
            }
        }


        public bool GetSocketAliveStatus()
        {
            return marketSocket.IsAlive;
        }

        /// <summary>
        /// Post a review on the developers page
        /// </summary>
        /// <param name="message">The content of the review</param>
        /// <returns></returns>
        public async Task<bool> PostReview(string message = "Thank you for WFinfo!")
        {
            var msg = $"{{\"text\":\"{message}\",\"review_type\":\"1\"}}";
            var developers = new List<string> { "dimon222", "Dapal003", "Kekasi" };
            foreach (var developer in developers)
            {
                try
                {
                    using (var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("https://api.warframe.market/v1/profile/" + developer + "/review"),
                        Method = HttpMethod.Post
                    })
                    {
                        request.Headers.Add("Authorization", "JWT " + JWT);
                        request.Headers.Add("language", "en");
                        request.Headers.Add("accept", "application/json");
                        request.Headers.Add("platform", "pc");
                        request.Headers.Add("auth_type", "header");
                        request.Content = new StringContent(msg, System.Text.Encoding.UTF8, "application/json");
                        var response = await client.SendAsync(request);
                        var body = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Body: {body}, Content: {msg}");
                    }
                }
                catch (Exception e)
                {
                    Main.AddLog("PostReview: " + e.Message);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the user's ingame name needed to make listings
        /// </summary>
        /// <returns></returns>
        public async Task SetIngameName()
        {
            using (var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.warframe.market/v1/profile"),
                Method = HttpMethod.Get
            })
            {
                request.Headers.Add("Authorization", "JWT " + JWT);
                request.Headers.Add("language", "en");
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("platform", "pc");
                request.Headers.Add("auth_type", "header");
                var response = await client.SendAsync(request);
                //setJWT(response.Headers);
                var profile = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
                inGameName = profile["profile"]?.Value<string>("ingame_name");
            }
        }

    }
}

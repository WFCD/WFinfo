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
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

        private static readonly List<Dictionary<int, List<int>>> korean = new List<Dictionary<int, List<int>>>() {
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
        public string JWT; // JWT is the security key, store this as email+pw combo'
        private ClientWebSocket marketSocket = new ClientWebSocket();
        private CancellationTokenSource marketSocketCancellation = new CancellationTokenSource();
        private readonly ManualResetEvent marketSocketOpenEvent = new ManualResetEvent(false);
        private TaskCompletionSource<bool> _authenticationCompletionSource;
        private bool _isWebSocketAuthenticated = false;
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

        public static WebClient CreateWfmClient()
        {
            WebClient webClient = CustomEntrypoint.CreateNewWebClient();
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
                Proxy = proxy,
                UseCookies = false
            };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WFInfo/" + Main.BuildVersion);
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

        private static void SaveDatabase(string path, object db)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(db, Formatting.Indented));
        }

        public bool IsJwtLoggedIn()
        {
            return JWT != null && JWT.Length > 300; //check if the token is of the right length
        }

        public int GetGithubVersion()
        {
            WebClient githubWebClient = CustomEntrypoint.CreateNewWebClient();
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
        public async void ReloadItems()
        {
            marketItems = new JObject();
            WebClient webClient = CreateWfmClient();
            JObject obj =
                JsonConvert.DeserializeObject<JObject>(
                    webClient.DownloadString("https://api.warframe.market/v2/items"));

            JArray items = JArray.FromObject(obj["data"]);
            foreach (var item in items)
            {
                string name = item["i18n"]["en"]["name"].ToString();
                if (name.Contains("Prime "))
                {
                    if ((name.Contains("Neuroptics") || name.Contains("Chassis") || name.Contains("Systems") || name.Contains("Harness") || name.Contains("Wings")))
                    {
                        name = name.Replace(" Blueprint", "");
                    }
                    marketItems[item["id"].ToString()] = name + "|" + item["slug"];
                }
            }

            try
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v2/items"),
                    Method = HttpMethod.Get
                })
                {
                    request.Headers.Add("language", _settings.Locale);
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var parsed = JsonConvert.DeserializeObject<JObject>(body);
                    items = JArray.FromObject(parsed["data"]);
                    foreach (var item in items)
                    {
                        string name = item["slug"].ToString();
                        if (name.Contains("prime") && marketItems.ContainsKey(item["id"].ToString()))
                            marketItems[item["id"].ToString()] = marketItems[item["id"].ToString()] + "|" + item["i18n"][_settings.Locale]["name"];
                    }
                }
            }
            catch (Exception e)
            {
                Main.AddLog("ReloadItems: " + e.Message);
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
            try
            {
                ReloadItems();
            }
            catch
            {
                Main.AddLog("Failed to refresh items from warframe.market, skipping WFM update for now. Some items might have incomplete info.");
            }
            marketData = new JObject();
            WebClient webClient = CustomEntrypoint.CreateNewWebClient();
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
            WebClient webClient = CreateWfmClient();
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
            webClient = CreateWfmClient();
            JObject responseJObject = JsonConvert.DeserializeObject<JObject>(
                webClient.DownloadString("https://api.warframe.market/v2/item/" + url)
            );
            string ducat;
            if (!responseJObject["data"].ToObject<JObject>().TryGetValue("ducats", out JToken temp))
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
            WebClient webClient = CustomEntrypoint.CreateNewWebClient();
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
                WebClient webClient = CustomEntrypoint.CreateNewWebClient();
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
                WebClient webClient = CustomEntrypoint.CreateNewWebClient();
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

        private static void AddElement(int[,] d, List<int> xList, List<int> yList, int x, int y)
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

        public static int LevenshteinDistanceDefault(string s, string t)
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

        // This isn't used anymore?!
        public static bool IsKorean(String str)
        {
            // Safeguard for empty strings that will give false positives and/or crashes
            if (string.IsNullOrEmpty(str)) return false;
            char c = str[0];
            if (0x1100 <= c && c <= 0x11FF) return true;
            if (0x3130 <= c && c <= 0x318F) return true;
            if (0xAC00 <= c && c <= 0xD7A3) return true;
            return false;
        }

        public string GetLocaleNameData(string s)
        {
            // Why is this here?! Might require review why its never saving json
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
            s = GetLocaleNameData(s);

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

        private static bool GroupEquals(Dictionary<int, List<int>> group, int ak, int bk)
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

        public static string GetSetName(string name)
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
                if (_settings.AutoList && string.IsNullOrEmpty(inGameName))
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
            var content = JsonConvert.SerializeObject(new
            {
                email,
                password,
                device_id = "wfinfo",
                auth_type = "header"
            });
            request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", "JWT");
            request.Headers.Add("language", "en");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("platform", "pc");
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


        // Some vibe-coded reflection modification for userAgent
        public static void SetUserAgent(ClientWebSocketOptions options, string userAgent)
        {
            try
            {
                options.SetRequestHeader("User-Agent", userAgent);
                return;
            }
            catch (System.ArgumentException ex)
            {
                //Debug.WriteLine(ex.ToString());
                // Fallback to reflection if User-Agent is not settable
                var field = options.GetType().GetField("_requestHeaders", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var headers = field.GetValue(options) as System.Collections.Specialized.NameValueCollection
                                  ?? new System.Collections.Specialized.NameValueCollection();
                    headers["User-Agent"] = userAgent;
                    field.SetValue(options, headers);
                }
            }
        }

        // Listener to track messages coming back
        private async Task StartWebSocketListener()
        {
            var buffer = new byte[8192];

            try
            {
                while (!marketSocketCancellation.Token.IsCancellationRequested)
                {
                    if (marketSocket.State != WebSocketState.Open) break;

                    var sb = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await marketSocket.ReceiveAsync(new ArraySegment<byte>(buffer), marketSocketCancellation.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Acknowledge and exit gracefully
                            await marketSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Ack close", CancellationToken.None);
                            return;
                        }
                        if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                        {
                            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        }
                    } while (!result.EndOfMessage && !marketSocketCancellation.Token.IsCancellationRequested) ;

                    if (sb.Length > 0)
                    {
                        await HandleWebSocketMessage(sb.ToString());
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is triggered
            }
            catch (Exception e)
            {
                var inner = e.InnerException?.ToString() ?? "None";
                Main.AddLog($"WebSocket listener error: {e.Message}\nInnerException: {inner}\nStackTrace: {e.StackTrace}");
            }
        }

        // Add this method to handle incoming websocket messages
        private static async Task HandleWebSocketMessage(string message)
        {
            try
            {
                // Make JSON parsing async by running it on a background thread
                var messageObj = await Task.Run(() =>
                    JsonConvert.DeserializeObject<JObject>(message)
                ).ConfigureAwait(false);

                // Check for authentication success response
                var route = messageObj["route"]?.ToString();
                var payload = messageObj["payload"];

                if (route == "@wfm|cmd/auth/signIn" || route?.Contains("auth") == true)
                {
                    // Check if authentication was successful
                    var success = payload?["success"]?.ToObject<bool>() ??
                                 (payload?["error"] == null); // No error means success

                    if (success)
                    {
                        Main.AddLog("WebSocket authentication successful");
                        Main.dataBase._isWebSocketAuthenticated = true;
                        Main.dataBase._authenticationCompletionSource?.SetResult(true);
                    }
                    else
                    {
                        var error = payload?["error"]?.ToString() ?? "Unknown authentication error";
                        Main.AddLog($"WebSocket authentication failed: {error}");
                        Main.dataBase._authenticationCompletionSource?.SetResult(false);
                    }
                }

                // Handle status change messages from the server (only if authenticated)
                if (Main.dataBase._isWebSocketAuthenticated)
                {
                    var statusPayload = messageObj["payload"]?["status"]?.ToString();
                    if (!string.IsNullOrEmpty(statusPayload))
                    {
                        await Main.UpdateMarketStatusAsync(statusPayload).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                // Async logging
                await Task.Run(() =>
                    Main.AddLog($"Error handling websocket message: {e.Message}")
                ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attempts to connect the user's account to the websocket
        /// </summary>
        /// <returns>A task to be awaited</returns>
        public async Task<bool> OpenWebSocket()
        {
            Main.AddLog("Connecting to websocket");

            // If already connected and authenticated, return true
            if (marketSocket != null && marketSocket.State == WebSocketState.Open && _isWebSocketAuthenticated)
            {
                return true;
            }

            // Clean up existing websocket if needed
            if (marketSocket != null)
            {
                try
                {
                    // Check state before attempting operations
                    var currentState = marketSocket.State;
                    if (currentState == WebSocketState.Open || currentState == WebSocketState.Connecting)
                    {
                        await marketSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Expected when websocket is not in correct state - ignore
                }
                catch (Exception ex)
                {
                    Main.AddLog($"Non-critical error closing existing websocket: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        marketSocket.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Main.AddLog($"Error disposing websocket: {ex.Message}");
                    }
                    marketSocket = null;
                }
            }

            // Create new websocket
            marketSocket = new ClientWebSocket();

            // Reset authentication state
            _isWebSocketAuthenticated = false;
            _authenticationCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                marketSocket.Options.AddSubProtocol("wfm");
                marketSocket.Options.SetRequestHeader("Authorization", "Bearer " + JWT);
                SetUserAgent(marketSocket.Options, "WFInfo/" + Main.BuildVersion);

                Uri marketSocketUri = new Uri("wss://warframe.market/socket-v2");
                marketSocketOpenEvent.Reset();

                await marketSocket.ConnectAsync(marketSocketUri, CancellationToken.None);
                marketSocketOpenEvent.Set();

                if (marketSocket.State == WebSocketState.Open)
                {
                    Debug.WriteLine("Opening reading socket");
                    _webSocketListenerTask = Task.Run(StartWebSocketListener);

                    // Send authentication
                    bool authSuccess = await AuthenticateWebSocket();

                    if (authSuccess)
                    {
                        Main.AddLog("WebSocket connected and authenticated successfully");

                        // Send initial status update after a small delay to let things settle
                        _ = Task.Delay(500).ContinueWith(async _ =>
                        {
                            if (_process.IsRunning && !_process.GameIsStreamed)
                            {
                                await SetWebsocketStatus("ingame");
                            }
                            else
                            {
                                await SetWebsocketStatus("online");
                            }
                        });
                    }

                    return authSuccess;
                }
            }
            catch (ArgumentException ex)
            {
                Main.AddLog($"WebSocket argument error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Main.AddLog($"Error connecting to websocket: {ex.Message}");
                marketSocket?.Dispose();
                marketSocket = null;
                return false;
            }

            return false;
        }

        private async Task<bool> AuthenticateWebSocket()
        {
            try
            {
                bool authMessageSent = await SendMessage(
                    JsonConvert.SerializeObject(new
                    {
                        route = "@wfm|cmd/auth/signIn",
                        payload = new
                        {
                            token = JWT,
                            deviceId = "wfinfo"
                        }
                    })
                );

                if (!authMessageSent)
                {
                    Main.AddLog("Failed to send authentication message");
                    return false;
                }

                // Wait for authentication completion
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var completedTask = await Task.WhenAny(_authenticationCompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Main.AddLog("WebSocket authentication timed out");
                    return false;
                }

                return await _authenticationCompletionSource.Task;
            }
            catch (Exception ex)
            {
                Main.AddLog($"WebSocket authentication error: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Sets the JWT to be used for future calls
        /// </summary>
        /// <param name="headers">Response headers from the original Login call</param>
        public void SetJWT(HttpResponseHeaders headers)
        {
            foreach (var item in headers)
            {
                //Debug.WriteLine(item.Key);
                if (!item.Key.ToLower(Main.culture).Contains("authorization")) continue;
                var temp = item.Value.First();
                // Split the second part of expression ("JWT ..." or "Bearer ...")
                JWT = temp.Split(' ')[1];
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
                    RequestUri = new Uri("https://api.warframe.market/v2/order"),
                    Method = HttpMethod.Post,
                })
                {
                    var itemId = PrimeItemToItemID(primeItem);
                    var json = JsonConvert.SerializeObject(new
                    {
                        type = "sell",
                        itemId,
                        platinum,
                        quantity
                    });
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", "Bearer " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode) throw new Exception(responseBody);
                    //SetJWT(response.Headers);
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
                    RequestUri = new Uri("https://api.warframe.market/v2/order/" + listingId),
                    Method = HttpMethod.Put,
                })
                {
                    var json = JsonConvert.SerializeObject(new
                    {
                        platinum,
                        quantity,
                        visible = true
                    });
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", "Bearer " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode) throw new Exception(responseBody);

                    //SetJWT(response.Headers);
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

        private readonly object _statusUpdateLock = new object();
        private DateTime _lastStatusUpdate = DateTime.MinValue;
        private string _lastStatusSent = "";
        private volatile bool _statusUpdateInProgress = false;
        public async Task SetWebsocketStatus(string status)
        {
            if (!_isWebSocketAuthenticated)
            {
                Debug.WriteLine("Cannot set websocket status: Not authenticated");
                return;
            }

            // Prevent simultaneous calls
            lock (_statusUpdateLock)
            {
                if (_statusUpdateInProgress)
                {
                    Debug.WriteLine($"Status update already in progress, skipping: {status}");
                    return;
                }

                // Prevent duplicate status within short timeframe
                var now = DateTime.UtcNow;
                if (_lastStatusSent == status && (now - _lastStatusUpdate).TotalMilliseconds < 500)
                {
                    Debug.WriteLine($"Skipping duplicate status update: {status}");
                    return;
                }

                _statusUpdateInProgress = true;
                _lastStatusUpdate = now;
                _lastStatusSent = status;
            }

            try
            {
                var payload = new { route = "@wfm|cmd/status/set", payload = new { status = status } };
                string message = JsonConvert.SerializeObject(payload);

                bool success = await SendMessage(message);
                if (success)
                {
                    Debug.WriteLine($"WebSocket status set to: {status}");
                }
                else
                {
                    Main.AddLog($"Failed to set websocket status to: {status}");
                }
            }
            finally
            {
                lock (_statusUpdateLock)
                {
                    _statusUpdateInProgress = false;
                }
            }
        }

        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

        // Update your SendMessage method to use the semaphore
        private async Task<bool> SendMessage(string message)
        {
            if (marketSocket == null)
            {
                Main.AddLog("Cannot send message: WebSocket is null");
                return false;
            }

            if (marketSocket.State != WebSocketState.Open)
            {
                Main.AddLog($"Cannot send message: WebSocket state is {marketSocket.State}");
                return false;
            }

            bool acquired = false;
            try
            {
                // Acquire semaphore with timeout to prevent indefinite blocking
                acquired = await _sendSemaphore.WaitAsync(TimeSpan.FromSeconds(10));
                if (!acquired)
                {
                    Main.AddLog("Failed to acquire send semaphore within timeout");
                    return false;
                }

                // Double-check websocket state after acquiring semaphore
                if (marketSocket == null || marketSocket.State != WebSocketState.Open)
                {
                    Main.AddLog("WebSocket state changed while waiting for semaphore");
                    return false;
                }

                var messageBytes = Encoding.UTF8.GetBytes(message);
                var buffer = new ArraySegment<byte>(messageBytes);

                // Send with timeout using CancellationToken
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    await marketSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
                }

                Debug.WriteLine($"WebSocket message sent successfully: {message}");
                return true;
            }
            catch (OperationCanceledException)
            {
                Main.AddLog("WebSocket send operation timed out");
                return false;
            }
            catch (WebSocketException wsEx)
            {
                Main.AddLog($"WebSocket error while sending message: {wsEx.Message}");
                return false;
            }
            catch (ObjectDisposedException)
            {
                Main.AddLog("Cannot send message: WebSocket has been disposed");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Main.AddLog($"Invalid WebSocket operation: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Main.AddLog($"Unexpected error while sending WebSocket message: {ex.Message}");
                return false;
            }
            finally
            {
                if (acquired)
                {
                    _sendSemaphore.Release();
                }
            }
        }
        /// <summary>
        /// Disconnects the user from websocket and sets JWT to null
        /// </summary>
        // Add this field to track the listener task
        private Task _webSocketListenerTask;

        public void Disconnect()
        {
            try
            {
                // Send invisible status first while everything is still operational
                if (marketSocket != null && marketSocket.State == WebSocketState.Open && _isWebSocketAuthenticated && IsJwtLoggedIn())
                {
                    try
                    {
                        var task = SetWebsocketStatus("invisible");
                        task.Wait(2000);
                    }
                    catch (Exception ex)
                    {
                        Main.AddLog($"Could not send invisible status: {ex.Message}");
                    }
                }

                // Reset authentication state
                _isWebSocketAuthenticated = false;

                // Complete the authentication task safely
                if (_authenticationCompletionSource != null && !_authenticationCompletionSource.Task.IsCompleted)
                {
                    _authenticationCompletionSource.TrySetResult(false);
                }
                _authenticationCompletionSource = null;

                // Cancel background operations FIRST
                marketSocketCancellation?.Cancel();

                // Wait for the listener task to complete before disposing websocket
                if (_webSocketListenerTask != null && !_webSocketListenerTask.IsCompleted)
                {
                    try
                    {
                        // Give the listener task time to respond to cancellation
                        _webSocketListenerTask.Wait(3000); // Wait up to 3 seconds
                    }
                    catch (AggregateException ex)
                    {
                        // These exceptions are expected during shutdown
                        foreach (var innerEx in ex.InnerExceptions)
                        {
                            if (!(innerEx is OperationCanceledException ||
                                  innerEx is WebSocketException ||
                                  innerEx is ObjectDisposedException))
                            {
                                Main.AddLog($"Unexpected listener shutdown exception: {innerEx.Message}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                }

                // Now safely dispose the websocket
                if (marketSocket != null)
                {
                    try
                    {
                        // Only attempt close if still in open state
                        if (marketSocket.State == WebSocketState.Open)
                        {
                            // Use a short timeout for graceful close
                            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                            {
                                var closeTask = marketSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", cts.Token);
                                closeTask.Wait(1500);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // These are expected during forceful shutdown - only log unexpected ones
                        if (!(ex is ObjectDisposedException || ex is WebSocketException ||
                              ex is OperationCanceledException || ex is IOException))
                        {
                            Main.AddLog($"Unexpected websocket close exception: {ex.Message}");
                        }
                    }
                    finally
                    {
                        try
                        {
                            marketSocket.Dispose();
                        }
                        catch
                        {
                            // Suppress all disposal exceptions
                        }
                        marketSocket = null;
                        _webSocketListenerTask = null; // Clear the task reference
                    }
                }

                // Clear user data
                JWT = null;
                rememberMe = false;
                inGameName = string.Empty;

                // Clean up other resources
                try
                {
                    marketSocketOpenEvent?.Reset();
                }
                catch
                {
                    // Suppress exceptions
                }

                try
                {
                    marketSocketCancellation?.Dispose();
                    marketSocketCancellation = new CancellationTokenSource();
                }
                catch
                {
                    // Suppress exceptions
                }

                Main.AddLog("WebSocket disconnected successfully");
            }
            catch (Exception ex)
            {
                Main.AddLog($"Error during disconnect: {ex.Message}");
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
        public async Task<JObject> GetTopListings(string primeName)
        {
            var urlName = GetUrlName(primeName);

            try
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v2/orders/item/" + urlName + "/top"),
                    Method = HttpMethod.Get
                })
                {
                    request.Headers.Add("Authorization", "Bearer " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");
                    var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    var payload = JsonConvert.DeserializeObject<JObject>(body);
                    if (body.Length < 3)
                        throw new Exception("No sell orders found: " + payload);
                    //Debug.WriteLine(body);

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
                    RequestUri = new Uri("https://api.warframe.market/v2/me"),
                    Method = HttpMethod.Get,
                })
                {
                    request.Headers.Add("Authorization", "Bearer " + JWT);
                    var response = await client.SendAsync(request);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Main.AddLog($"JWT is invalidated or expired");
                        return false;
                    } else
                    {
                        //SetJWT(response.Headers);
                        var profile = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
                        profile["data"]["checkCode"] = "REDACTED"; // remove the code that can compromise an account.
                        Debug.WriteLine($"JWT check response: {profile["data"]}");
                        return true;
                    }
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
                if (string.IsNullOrEmpty(inGameName))
                {
                    await SetIngameName();
                }

                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("https://api.warframe.market/v2/orders/my"),
                    Method = HttpMethod.Get
                })
                {
                    request.Headers.Add("Authorization", "Bearer " + JWT);
                    request.Headers.Add("language", "en");
                    request.Headers.Add("accept", "application/json");
                    request.Headers.Add("platform", "pc");
                    request.Headers.Add("auth_type", "header");
                    var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    var payload = JsonConvert.DeserializeObject<JObject>(body);
                    var allOrders = (JArray)payload?["data"];
                    string itemID = PrimeItemToItemID(primeName);

                    if (allOrders != null)
                    {
                        foreach (var listing in allOrders)
                        {
                            if ((string)listing["type"] == "sell" && itemID == (string)listing?["itemId"])
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
            return marketSocket.State == WebSocketState.Open;
        }

        /// <summary>
        /// Post a review on the developers page
        /// </summary>
        /// <param name="message">The content of the review</param>
        /// <returns></returns>
        public async Task<bool> PostReview(string message = "Thank you for WFinfo!")
        {
            var msg = $"{{\"text\":\"{message}\",\"review_type\":\"1\"}}";
            var developers = new List<string> { "dimon222", "Dapal003", "Kekasi", "D1firehail" };
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
                RequestUri = new Uri("https://api.warframe.market/v2/me"),
                Method = HttpMethod.Get
            })
            {
                request.Headers.Add("Authorization", "Bearer " + JWT);
                request.Headers.Add("language", "en");
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("platform", "pc");
                request.Headers.Add("auth_type", "header");
                var response = await client.SendAsync(request);
                //setJWT(response.Headers);
                var profile = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
                inGameName = profile["data"]?.Value<string>("ingameName");
            }
        }

    }
}

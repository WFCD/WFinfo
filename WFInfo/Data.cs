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
using System.Threading.Tasks;
using WebSocketSharp;

namespace WFInfo {

	class Data {
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
		public string JWT; // JWT is the securty key, store this as email+pw combo
		private WebSocket marketSocket = new WebSocket("wss://warframe.market/socket?platform=pc");
		private readonly string filterAllJSON = "https://docs.google.com/uc?id=1zqI55GqcXMfbvZgBjASC34ad71GDTkta&export=download";

		static readonly HttpClient client = new HttpClient();
		readonly WebClient WebClient;
		private readonly Sheets sheetsApi;
		private string githubVersion;

		private LogCapture EElogWatcher;

		public Data() {
			Main.AddLog("Initializing Databases");
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
		}

		public void EnableLogcapture() {
			if (EElogWatcher == null) {
				try {
					EElogWatcher = new LogCapture();
					EElogWatcher.TextChanged += LogChanged;
				}
				catch (Exception ex) {
					Main.AddLog("Failed to start logcapture, exception: " + ex);
					Main.StatusUpdate("Failed to start capturing log", 1);
				}
			}
		}

		public void DisableLogCapture() {
			if (EElogWatcher != null) {
				EElogWatcher.TextChanged -= LogChanged;
				EElogWatcher.Dispose();
				EElogWatcher = null;
			}
		}

		private void SaveDatabase(string path, object db) {
			File.WriteAllText(path, JsonConvert.SerializeObject(db, Formatting.Indented));
		}

		public bool IsJwtAvailable() {
			return JWT != null;
		}

		public int GetGithubVersion() {
			WebClient.Headers.Add("User-Agent", "WFCD");
			JObject github =
				JsonConvert.DeserializeObject<JObject>(
					WebClient.DownloadString("https://api.github.com/repos/WFCD/WFInfo/releases/latest"));
			if (github.ContainsKey("tag_name")) {
				githubVersion = github["tag_name"].ToObject<string>();
				return Main.VersionToInteger(githubVersion);
			}
			return Main.VersionToInteger(Main.BuildVersion);
		}

		// Load item list from Sheets
		private void ReloadItems() {
			marketItems = new JObject();

			IList<IList<object>> sheet = sheetsApi.GetSheet("items!A:C");
			foreach (IList<object> row in sheet) {
				string name = row[1].ToString();
				if (name.Contains("Prime "))
					marketItems[row[0].ToString()] = name + "|" + row[2].ToString();
			}

			marketItems["version"] = Main.BuildVersion;
			Main.AddLog("Item database has been downloaded");
		}

		// Load market data from Sheets
		private bool LoadMarket(JObject allFiltered, bool force = false) {
			if (!force && File.Exists(marketDataPath) && File.Exists(marketItemsPath)) {
				if (marketData == null)
					marketData = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(marketDataPath));
				if (marketItems == null)
					marketItems = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(marketItemsPath));

				if (marketData.TryGetValue("version", out JToken version) && (marketData["version"].ToObject<string>() == Main.BuildVersion)) {
					DateTime timestamp = marketData["timestamp"].ToObject<DateTime>();
					if (timestamp > DateTime.Now.AddHours(-12)) {
						Main.AddLog("Market Databases are up to date");
						return false;
					}
				}
			}
			ReloadItems();
			marketData = new JObject();

			IList<IList<object>> sheet = sheetsApi.GetSheet("prices!A:I");
			foreach (IList<object> row in sheet) {
				string name = row[0].ToString();
				if (name.Contains("Prime ")) {
					marketData[name] = new JObject
					{
						{"plat", double.Parse(row[8].ToString(), Main.culture)},
						{"ducats", 0},
						{"volume", int.Parse(row[4].ToString()) + int.Parse(row[6].ToString())}
					};
				}
			}

			// Add default values for ignored items
			foreach (KeyValuePair<string, JToken> ignored in allFiltered["ignored_items"].ToObject<JObject>()) {
				marketData[ignored.Key] = ignored.Value;
			}

			marketData["timestamp"] = DateTime.Now;
			marketData["version"] = Main.BuildVersion;

			Main.AddLog("Plat database has been downloaded");

			return true;
		}

		private void LoadMarketItem(string item_name, string url) {
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
			if (!ducats.TryGetValue("ducats", out JToken temp)) {
				ducat = "0";
			} else {
				ducat = temp.ToObject<string>();
			}

			marketData[item_name] = new JObject
			{
				{ "ducats", ducat },
				{ "plat", stats["avg_price"] }
			};
		}

		private bool LoadEqmtData(JObject allFiltered, bool force = false) {
			if (equipmentData == null)
				equipmentData = File.Exists(eqmtDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(eqmtDataPath)) : new JObject();
			if (relicData == null)
				relicData = File.Exists(relicDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(relicDataPath)) : new JObject();
			if (nameData == null)
				nameData = File.Exists(nameDataPath) ? JsonConvert.DeserializeObject<JObject>(File.ReadAllText(nameDataPath)) : new JObject();

			// fill in equipmentData (NO OVERWRITE)
			// fill in nameData
			// fill in relicData

			DateTime filteredDate = allFiltered["timestamp"].ToObject<DateTime>().ToLocalTime().AddHours(-1);
			DateTime eqmtDate = equipmentData.TryGetValue("timestamp", out _) ? equipmentData["timestamp"].ToObject<DateTime>() : filteredDate;

			if (force || eqmtDate.CompareTo(filteredDate) <= 0) {
				equipmentData["timestamp"] = DateTime.Now;
				relicData = new JObject();
				relicData["timestamp"] = DateTime.Now;
				nameData = new JObject();

				foreach (KeyValuePair<string, JToken> era in allFiltered["relics"].ToObject<JObject>()) {
					relicData[era.Key] = new JObject();
					foreach (KeyValuePair<string, JToken> relic in era.Value.ToObject<JObject>())
						relicData[era.Key][relic.Key] = relic.Value;
				}

				foreach (KeyValuePair<string, JToken> prime in allFiltered["eqmt"].ToObject<JObject>()) {
					string primeName = prime.Key.Substring(0, prime.Key.IndexOf("Prime") + 5);
					if (!equipmentData.TryGetValue(primeName, out _))
						equipmentData[primeName] = new JObject();
					equipmentData[primeName]["vaulted"] = prime.Value["vaulted"];
					equipmentData[primeName]["type"] = prime.Value["type"];

					if (!equipmentData[primeName].ToObject<JObject>().TryGetValue("parts", out _))
						equipmentData[primeName]["parts"] = new JObject();


					foreach (KeyValuePair<string, JToken> part in prime.Value["parts"].ToObject<JObject>()) {
						string partName = part.Key;
						if (prime.Key.Contains("Collar")) {
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
						if (prime.Value["type"].ToString() == "Archwing" && (part.Key.Contains("Systems") || part.Key.Contains("Harness") || part.Key.Contains("Wings"))) {
							gameName += " Blueprint";
						} else if (prime.Value["type"].ToString() == "Warframes" && (part.Key.Contains("Systems") || part.Key.Contains("Neuroptics") || part.Key.Contains("Chassis"))) {
							gameName += " Blueprint";
						}
						if (marketData.TryGetValue(partName, out _)) {
							nameData[gameName] = partName;
							marketData[partName]["ducats"] = Convert.ToInt32(part.Value["ducats"].ToString());
						}
					}
				}

				// Add default values for ignored items
				foreach (KeyValuePair<string, JToken> ignored in allFiltered["ignored_items"].ToObject<JObject>()) {
					nameData[ignored.Key] = ignored.Key;
				}

				Main.AddLog("Prime Database has been downloaded");
				return true;
			}
			Main.AddLog("Prime Database is up to date");
			return false;
		}

		private void RefreshMarketDucats() {
			//equipmentData[primeName]["parts"][partName]["ducats"]
			foreach (KeyValuePair<string, JToken> prime in equipmentData)
				if (prime.Key != "timestamp")
					foreach (KeyValuePair<string, JToken> part in equipmentData[prime.Key]["parts"].ToObject<JObject>())
						if (marketData.TryGetValue(part.Key, out _))
							marketData[part.Key]["ducats"] = Convert.ToInt32(part.Value["ducats"].ToString());
		}

		private void MarkAllEquipmentVaulted() {
			foreach (KeyValuePair<string, JToken> kvp in equipmentData) {
				if (kvp.Key.Contains("Prime")) {
					equipmentData[kvp.Key]["vaulted"] = true;
					foreach (KeyValuePair<string, JToken> part in kvp.Value["parts"].ToObject<JObject>()) {
						equipmentData[kvp.Key]["parts"][part.Key]["vaulted"] = true;
					}
				}
			}
		}

		private void GetSetVaultStatus() {
			foreach (KeyValuePair<string, JToken> keyValuePair in equipmentData) {
				if (keyValuePair.Key.Contains("Prime")) {
					bool vaulted = false;
					foreach (KeyValuePair<string, JToken> part in keyValuePair.Value["parts"].ToObject<JObject>()) {
						if (part.Value["vaulted"].ToObject<bool>()) {
							vaulted = true;
							break;
						}
					}

					equipmentData[keyValuePair.Key]["vaulted"] = vaulted;
				}
			}
		}

		private void MarkEquipmentUnvaulted(string era, string name) {
			JObject job = relicData[era][name].ToObject<JObject>();
			foreach (KeyValuePair<string, JToken> keyValuePair in job) {
				string str = keyValuePair.Value.ToObject<string>();
				if (str.IndexOf("Prime") != -1) {
					// Cut the name of actual part without Prime prefix ??
					string eqmt = str.Substring(0, str.IndexOf("Prime") + 5);
					if (equipmentData.TryGetValue(eqmt, out JToken temp)) {
						equipmentData[eqmt]["parts"][str]["vaulted"] = false;
					} else {
						Main.AddLog("Cannot find: " + eqmt + " in equipmentData");
					}
				}
			}
		}

		public bool Update() {
			Main.AddLog("Checking for Updates to Databases");
			JObject allFiltered = JsonConvert.DeserializeObject<JObject>(WebClient.DownloadString(filterAllJSON));
			bool saveDatabases = LoadMarket(allFiltered);

			foreach (KeyValuePair<string, JToken> elem in marketItems) {
				if (elem.Key != "version") {
					string[] split = elem.Value.ToString().Split('|');
					string itemName = split[0];
					string itemUrl = split[1];
					if (!itemName.Contains(" Set") && !marketData.TryGetValue(itemName, out _)) {
						LoadMarketItem(itemName, itemUrl);
						saveDatabases = true;
					}
				}
			}
			Main.RunOnUIThread(() => { MainWindow.INSTANCE.Market_Data.Content = marketData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm"); });

			saveDatabases = LoadEqmtData(allFiltered, saveDatabases);
			Main.RunOnUIThread(() => { MainWindow.INSTANCE.Drop_Data.Content = equipmentData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm"); });

			if (saveDatabases)
				SaveAllJSONs();

			return saveDatabases;
		}

		public void ForceMarketUpdate() {
			try {
				Main.AddLog("Forcing market update");
				JObject allFiltered = JsonConvert.DeserializeObject<JObject>(WebClient.DownloadString(filterAllJSON));
				LoadMarket(allFiltered, true);

				foreach (KeyValuePair<string, JToken> elem in marketItems) {
					if (elem.Key != "version") {
						string[] split = elem.Value.ToString().Split('|');
						string itemName = split[0];
						string itemUrl = split[1];
						if (!itemName.Contains(" Set") && !marketData.TryGetValue(itemName, out _)) {
							LoadMarketItem(itemName, itemUrl);
						}
					}
				}

				RefreshMarketDucats();

				SaveDatabase(marketItemsPath, marketItems);
				SaveDatabase(marketDataPath, marketData);
				Main.RunOnUIThread(() => {
					MainWindow.INSTANCE.Market_Data.Content = marketData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm");
					Main.StatusUpdate("Market Update Complete", 0);
					MainWindow.INSTANCE.ReloadDrop.IsEnabled = true;
					MainWindow.INSTANCE.ReloadMarket.IsEnabled = true;
				});
			}
			catch (Exception ex) {
				Main.AddLog("Market Update Failed");
				Main.AddLog(ex.ToString());
				Main.StatusUpdate("Market Update Failed", 0);
				new ErrorDialogue(DateTime.Now, 0);
			}
		}

		public void SaveAllJSONs() {
			SaveDatabase(eqmtDataPath, equipmentData);
			SaveDatabase(relicDataPath, relicData);
			SaveDatabase(nameDataPath, nameData);
			SaveDatabase(marketItemsPath, marketItems);
			SaveDatabase(marketDataPath, marketData);
		}

		public void ForceEquipmentUpdate() {
			try {
				Main.AddLog("Forcing equipment update");
				JObject allFiltered = JsonConvert.DeserializeObject<JObject>(WebClient.DownloadString(filterAllJSON));
				LoadEqmtData(allFiltered, true);
				SaveAllJSONs();
				Main.RunOnUIThread(() => {
					MainWindow.INSTANCE.Drop_Data.Content = equipmentData["timestamp"].ToObject<DateTime>().ToString("MMM dd - HH:mm");
					Main.StatusUpdate("Prime Update Complete", 0);

					MainWindow.INSTANCE.ReloadDrop.IsEnabled = true;
					MainWindow.INSTANCE.ReloadMarket.IsEnabled = true;
				});
			}
			catch (Exception ex) {
				Main.AddLog("Prime Update Failed");
				Main.AddLog(ex.ToString());
				Main.StatusUpdate("Prime Update Failed", 0);
				new ErrorDialogue(DateTime.Now, 0);
			}
		}

		public bool IsPartVaulted(string name) {
			if (name.IndexOf("Prime") < 0)
				return false;
			string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
			return equipmentData[eqmt]["parts"][name]["vaulted"].ToObject<bool>();
		}

		public string PartsOwned(string name) {
			if (name.IndexOf("Prime") < 0)
				return "0";
			string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
			string owned = equipmentData[eqmt]["parts"][name]["owned"].ToString();
			if (owned == "0")
				return "0";
			return owned;
		}

		public string PartsCount(string name) {
			if (name.IndexOf("Prime") < 0)
				return "0";
			string eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
			string count = equipmentData[eqmt]["parts"][name]["count"].ToString();
			if (count == "0")
				return "0";
			return count;
		}

		private void AddElement(int[,] d, List<int> xList, List<int> yList, int x, int y) {
			int loc = 0;
			int temp = d[x, y];
			while (loc < xList.Count && temp > d[xList[loc], yList[loc]]) {
				loc += 1;
			}

			if (loc == xList.Count) {
				xList.Add(x);
				yList.Add(y);
				return;
			}

			xList.Insert(loc, x);
			yList.Insert(loc, y);
		}

		readonly char[,] ReplacementList = null;

		public int GetDifference(char c1, char c2) {
			if (c1 == c2 || c1 == '?' || c2 == '?') {
				return 0;
			}

			for (int i = 0; i < ReplacementList.GetLength(0) - 1; i++) {
				if ((c1 == ReplacementList[i, 0] || c2 == ReplacementList[i, 0]) &&
					(c1 == ReplacementList[i, 1] || c2 == ReplacementList[i, 1])) {
					return 0;
				}
			}

			return 1;
		}

		public static int LevenshteinDistance(string s, string t) {
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
				for (int j = 1; j <= m; j++) {
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

		public int LevenshteinDistanceSecond(string str1, string str2, int limit = -1) {
			int num;
			Boolean maxY;
			int temp;
			Boolean maxX;
			string s = str1.ToLower();
			string t = str2.ToLower();
			int n = s.Length;
			int m = t.Length;
			if (!(n == 0 || m == 0)) {
				int[,] d = new int[n + 1 + 1 - 1, m + 1 + 1 - 1];
				List<int> activeX = new List<int>();
				List<int> activeY = new List<int>();
				d[0, 0] = 1;
				activeX.Add(0);
				activeY.Add(0);
				do {
					int currX = activeX[0];
					activeX.RemoveAt(0);
					int currY = activeY[0];
					activeY.RemoveAt(0);

					temp = d[currX, currY];
					if (limit != -1 && temp > limit) {
						return temp;
					}

					maxX = currX == n;
					maxY = currY == m;
					if (!maxX) {
						temp = d[currX, currY] + 1;
						if (temp < d[currX + 1, currY] || d[currX + 1, currY] == 0) {
							d[currX + 1, currY] = temp;
							AddElement(d, activeX, activeY, currX + 1, currY);
						}
					}

					if (!maxY) {
						temp = d[currX, currY] + 1;
						if (temp < d[currX, currY + 1] || d[currX, currY + 1] == 0) {
							d[currX, currY + 1] = temp;
							AddElement(d, activeX, activeY, currX, currY + 1);
						}
					}

					if (!maxX && !maxY) {
						temp = d[currX, currY] + GetDifference(s[currX], t[currY]);
						if (temp < d[currX + 1, currY + 1] || d[currX + 1, currY + 1] == 0) {
							d[currX + 1, currY + 1] = temp;
							AddElement(d, activeX, activeY, currX + 1, currY + 1);
						}
					}
				} while (!(maxX && maxY));

				num = d[n, m] - 1;
			} else {
				num = n + m;
			}

			return num;
		}

		public string ClosestAutoComplete(string searchQuery, int maxResults) {
			return GetPartNameHuman(searchQuery, out _);
		}

		public string GetPartName(string name, out int low) { // Checks the Levenshtein Distance of a string and returns the index in Names() of the closest part
			string lowest = null;
			string lowest_unfiltered = null;
			low = 9999;
			foreach (KeyValuePair<string, JToken> prop in nameData) {
				int val = LevenshteinDistance(prop.Key, name);
				if (val < low) {
					low = val;
					lowest = prop.Value.ToObject<string>();
					lowest_unfiltered = prop.Key;
				}
			}


			Main.AddLog("Found part(" + low + "): \"" + lowest_unfiltered + "\" from \"" + name + "\"");
			return lowest;
		}

		public string GetPartNameHuman(string name, out int low) { // Checks the Levenshtein Distance of a string and returns the index in Names() of the closest part optimized for human searching
			string lowest = null;
			string lowest_unfiltered = null;
			low = 9999;
			foreach (KeyValuePair<string, JToken> prop in nameData) {
				if (prop.Value.ToString().ToLower().Contains(name.ToLower())) {
					int val = LevenshteinDistance(prop.Value.ToString(), name);
					if (val < low) {
						low = val;
						lowest = prop.Value.ToObject<string>();
						lowest_unfiltered = prop.Value.ToString();
					}
				}
			}
			if (low > 10) {
				foreach (KeyValuePair<string, JToken> prop in nameData) {
					int val = LevenshteinDistance(prop.Value.ToString(), name);
					if (val < low) {
						low = val;
						lowest = prop.Value.ToObject<string>();
						lowest_unfiltered = prop.Value.ToString();
					}

				}
			}
			Main.AddLog("Found part(" + low + "): \"" + lowest_unfiltered + "\" from \"" + name + "\"");
			return lowest;
		}

		public string GetSetName(string name) {
			string result = name.ToLower();
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
			result = result.TrimEnd();
			result = Main.culture.TextInfo.ToTitleCase(result);
			return result;
		}

		public string GetRelicName(string string1) {
			string lowest = null;
			int low = 999;
			int temp = 0;
			string eraName = null;
			JObject job = null;

			foreach (KeyValuePair<string, JToken> era in relicData) {
				if (!era.Key.Contains("timestamp")) {
					temp = LevenshteinDistanceSecond(string1, era.Key + "??RELIC", low);
					if (temp < low) {
						job = era.Value.ToObject<JObject>();
						eraName = era.Key;
						low = temp;
					}
				}
			}

			low = 999;
			foreach (KeyValuePair<string, JToken> relic in job) {
				temp = LevenshteinDistanceSecond(string1, eraName + relic.Key + "RELIC", low);
				if (temp < low) {
					lowest = eraName + " " + relic.Key;
					low = temp;
				}
			}

			return lowest;
		}

		private Task autoThread;

		private void LogChanged(object sender, string line) {
			if (autoThread == null || autoThread.IsCompleted) {
				if (autoThread != null) {
					autoThread.Dispose();
					autoThread = null;
				}

				if (line.Contains("Pause countdown done") || line.Contains("Got rewards"))
					autoThread = Task.Factory.StartNew(AutoTriggered);
			}
		}

		public static void AutoTriggered() {
			try {
				var watch = Stopwatch.StartNew();
				long stop = watch.ElapsedMilliseconds + 5000;
				long wait = watch.ElapsedMilliseconds;

				OCR.UpdateWindow();

				while (watch.ElapsedMilliseconds < stop) {
					if (watch.ElapsedMilliseconds > wait) {
						wait += Settings.autoDelay;
						OCR.GetThemeWeighted(out double diff);
						if (diff > 100) {
							while (watch.ElapsedMilliseconds < wait) ;
							Main.AddLog("started auto processing");
							OCR.ProcessRewardScreen();
							break;
						}
					}
				}
				watch.Stop();
			}
			catch (Exception ex) {
				Main.AddLog("AUTO FAILED");
				Main.AddLog(ex.ToString());
				Main.StatusUpdate("Auto Detection Failed", 0);
				new ErrorDialogue(DateTime.Now, 0);
			}
		}

		/// <summary>
		///	Get's the user's login JWT to authenticate future API calls.
		/// </summary>
		/// <param name="email">Users email</param>
		/// <param name="password">Users password</param>
		/// <exception cref="Exception">Connection exception JSON formated</exception>
		/// <returns>A task to be awaited</returns>
		public async Task GetUserLogin(string email, string password) {
			var request = new HttpRequestMessage() {
				RequestUri = new Uri("https://api.warframe.market/v1/auth/signin"),
				Method = HttpMethod.Post,
			};
			string content = "{ \"email\":\"" + email + "\",\"password\":\"" + password + "\"}";
			request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
			request.Headers.Add("Authorization", "JWT");
			request.Headers.Add("language", "en");
			request.Headers.Add("accept", "application/json");
			request.Headers.Add("platform", "pc");
			request.Headers.Add("auth_type", "header");


			HttpResponseMessage response = await client.SendAsync(request);
			string responseBody = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode) {
				setJWT(response.Headers);
				await openSocket();
			} else {
				throw new Exception(responseBody);
			}


		}

		/// <summary>
		/// Attempts to connect the user's account to the websocket
		/// </summary>
		/// <returns>A task to be awaited</returns>
		public async Task openSocket() {

			if (marketSocket.IsAlive) {
				return;
			}

			marketSocket.OnMessage += (sender, e) =>
				Console.WriteLine("warframe.market: " + e.Data);
			marketSocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
			try {
				marketSocket.SetCookie(new WebSocketSharp.Net.Cookie("JWT", JWT));
				marketSocket.ConnectAsync();
			}
			catch (Exception e) {
				Console.WriteLine(e.Data);
				throw;
			}

			marketSocket.OnMessage += (sender, e) => {
				if (e.Data.Contains("@WS/ERROR")) // error checking, report back to main.status
				{
					Main.AddLog(e.Data);
					JWT = null; //assume authentication is invalid
					Main.signOut();
				}
			};

			marketSocket.OnOpen += (sender, e) => {
				marketSocket.Send("{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"online\"}"); //
			};
		}
		/// <summary>
		/// Sets the JWT to be used for future calls
		/// </summary>
		/// <param name="headers">Response headers from the original Login call</param>
		public void setJWT(HttpResponseHeaders headers) {
			foreach (var item in headers.GetValues("Set-Cookie")) {
				if (item.Contains("JWT=")) {
					int a = item.LastIndexOf("JWT=") + "JWT=".Length;
					int b = item.LastIndexOf("; Domain=.warframe.market;");
					JWT = item.Substring(a, (b - a));
					if (Settings.settingsObj["JWT"].ToString().Length > 10)  //update the cashed JWT if it exists
						Settings.settingsObj["JWT"] = JWT;
				}
			}
		}

		/// <summary>
		/// Lists an item under an account. Expected to be called after being logged in thus no login attempts.
		/// </summary>
		/// <param name="itemID">Warframe.market's ID for the item</param>
		/// <param name="platinum">The amount of platinum the user entered for the listing</param>
		/// <param name="quantity">The quantity of items the user listed.</param>
		public async void ListItem(string itemID, int platinum, int quantity) {
			try {
				var request = new HttpRequestMessage() {
					RequestUri = new Uri("https://api.warframe.market/v1/profile/orders"),
					Method = HttpMethod.Post,
				};
				var json = "{\"order_type\":\"sell\",\"item_id\":\"" + itemID + "\",\"platinum\":" + platinum + ",\"quantity\":" + quantity + "}";
				Console.WriteLine(json);
				request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
				request.Headers.Add("Authorization", "JWT " + JWT);
				request.Headers.Add("language", "en");
				request.Headers.Add("accept", "application/json");
				request.Headers.Add("platform", "pc");
				request.Headers.Add("auth_type", "header");

				HttpResponseMessage response = await client.SendAsync(request);
				string responseBody = await response.Content.ReadAsStringAsync();
				setJWT(response.Headers);

			}
			catch (HttpRequestException e) {
				Console.WriteLine("\nException Caught!");
				Console.WriteLine("Message :{0} ", e.Message);
			}
		}


		/// <summary>
		/// Sets the status of WFM websocket. Will try to reconnect if it is not already connected.
		/// </summary>
		/// <param name="status">Accepts the following values:
		/// offline, set's player status to be hidden on the site.  
		/// online, set's player status to be online on the site.   
		/// in game, set's player status to be online and ingame on the site
		/// </param>
		public async Task SetStatus(string status) {
			if (!IsJwtAvailable())
				return;

			var message = "{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"";
			switch (status) {
				case "offline":
				message += "invisible\"}";
				break;
				case "online":
				message += "online\"}";
				break;
				case "in game":
				message += "ingame\"}";
				break;
				default:
				throw new Exception("Tried setting status to something else");
			}
			try {
				SendMessage(message);
			}
			catch (Exception e) {
				Main.AddLog("Was unable to set status due to: " + e);
				throw;
			}
		}

		/// <summary>
		/// Dummy method to make it so that you log send messages
		/// </summary>
		/// <param name="data">The JSON string of data being sent over websocket</param>
		private void SendMessage(string data) {
			Main.AddLog("Sending: " + data + " to websocket.");
			marketSocket.Send(data);
		}
		/// <summary>
		/// Disconnects the user from websocket and sets JWT to null
		/// </summary>
		public void Disconnect() {
			SendMessage("{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"invisible\"}");
			JWT = null;
			Settings.settingsObj["JWT"] = null;
			Settings.Save();
			marketSocket.Close(1006);
		}

		public string getUrlName(string primeName) {
			return primeName.ToLower().Replace(' ', '_'); //seems to work for now but might need to be changed.
		}

		/// <summary>
		/// Tries to get the top listings of a prime item
		/// </summary>
		/// <param name="primeName"></param>
		/// <returns></returns>
		public JObject getTopListings(string primeName) //https://api.warframe.market/v1/items/ prime_name /orders/top
		{
			var urlName = getUrlName(primeName);
			var RequestUri = new Uri("https://api.warframe.market/v1/items/" + urlName + "/orders/top");

			try {
				JObject topListings = JsonConvert.DeserializeObject<JObject>(
						WebClient.DownloadString(RequestUri));
				return topListings;
			}
			catch (Exception e) {
				Console.WriteLine("\nException Caught!");
				Console.WriteLine("Message :{0} ", e.Message);
			}

			return null;
		}
		/// <summary>
		/// Untested, might not work
		/// </summary>
		/// <returns>bool of which answers the question "Is the user JWT valid?"</returns>
		public async Task<bool> checkIfJWTisValid()
		{
			try {
				var request = new HttpRequestMessage() {
					RequestUri = new Uri("https://api.warframe.market/v1/profile"),
					Method = HttpMethod.Get,
				};
				request.Headers.Add("Authorization", "JWT " + JWT);
				var response = await client.SendAsync(request);
				setJWT(response.Headers);
				var profile = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
				Console.WriteLine(profile.GetValue("role"));
				return (string)profile.GetValue("role") != "anonymous";
			}
			catch (Exception e) {
				Console.WriteLine("\nException Caught!");
				Console.WriteLine("Message :{0} ", e.Message);
				return false;
			}

		}

		public async Task<int> GetCurrentListedAmount(string primeName) {               //todo get the current user name and then probe  https://api.warframe.market/v1/profile/ <-username-> /orders to get quantaties
			int amount = 0;
			var inGameName = string.Empty;
			try {
				var request = new HttpRequestMessage() {
					RequestUri = new Uri("https://api.warframe.market/v1/profile"),
					Method = HttpMethod.Get
				};
				request.Headers.Add("Authorization", "JWT " + JWT);
				request.Headers.Add("language", "en");
				request.Headers.Add("accept", "application/json");
				request.Headers.Add("platform", "pc");
				request.Headers.Add("auth_type", "header");
				var response = await client.SendAsync(request);
				//setJWT(response.Headers);
				var profile = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
				Console.WriteLine(profile["profile"]);
				inGameName = profile["profile"].Value<string>("ingame_name");
				request = new HttpRequestMessage() {
					RequestUri = new Uri("https://api.warframe.market/v1/profile/" + inGameName + "/orders"),
					Method = HttpMethod.Get
				};
				request.Headers.Add("Authorization", "JWT " + JWT);
				request.Headers.Add("language", "en");
				request.Headers.Add("accept", "application/json");
				request.Headers.Add("platform", "pc");
				request.Headers.Add("auth_type", "header");
				var secondResponse = await client.SendAsync(request);
				var body = await secondResponse.Content.ReadAsStringAsync();
				Console.WriteLine(body);

			}
			catch (Exception e) {
				amount = 0;
				Console.WriteLine("\nException Caught!");
				Console.WriteLine("Message :{0} ", e.Message);
			}
			return amount;
		}
	}
}

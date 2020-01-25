using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
		public JObject name_data; // Contains relic to market name translation          {<relic_name>: <market_name>}

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
		private string githubVersion;

		private Sheets _sheetsApi;
		private NLua.Lua _lua;
		private string _currentDirectory = Directory.GetCurrentDirectory();

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

			string warframePictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Warframe"; //outdated? Was old work around for user who couldn't activate the program
			Directory.CreateDirectory(warframePictures);
			_screenshotWatcher.Path = warframePictures;
			_screenshotWatcher.EnableRaisingEvents = true;

			_lua = new NLua.Lua();
			if (false) //My.Settings.Auto) // WIP
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

		private int Get_Current_Version()
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

		private void download(string url)
		{
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse response = (HttpWebResponse)req.GetResponse();
			string resUri = response.ResponseUri.AbsoluteUri;
			Console.WriteLine("Downloading to " + _currentDirectory + "/WFinfo" + githubVersion + ".exe");
			try
			{
				WebClient.DownloadFile(resUri, _currentDirectory + "/WFInfonew.exe");
				FileStream fs = File.Create(_currentDirectory + "/update.bat");
				byte[] info = new UTF8Encoding(true).GetBytes(
					"timeout 2" + Environment.NewLine + "del WFInfo.exe" + Environment.NewLine + "rename WFInfonew.exe WFInfo.exe" +
					Environment.NewLine + "start WFInfo.exe");
				fs.Write(info, 0, info.Length);
				fs.Close();
				Process.Start(_currentDirectory + "/update.bat");
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				// No handling? We don't care? should we print something on main window?
			}
		}

		private Boolean Load_Items(Boolean force = false)
		{
			if (!force && File.Exists(_marketItemsPath))
			{
				if (market_items == null)
				{
					market_items = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_marketItemsPath));
				}

				string version;
				if (market_items.TryGetValue("version", out version) && market_items["version"] == Main.buildVersion)
				{
					Main.AddLog("ITEM DATABASE: GOOD");
					return false;
				}
			}

			Main.AddLog("ITEM DATABASE: LOADING NEW");
			market_items = new Dictionary<string, string>();

			var sheet = _sheetsApi.GetSheet("items!A:C");
			foreach (var row in sheet)
			{
				string name = row[1].ToString();
				if (name.Contains("Prime "))
				{
					market_items[row[0].ToString()] = name + "|" + row[2].ToString();
				}
			}

			market_items["version"] = Main.BuildVersion;
			Main.AddLog("ITEM DATABASE: GOOD");
			return true;
		}

		private Boolean Load_Market(Boolean force = false)
		{
			if (!force && File.Exists(_marketDataPath))
			{
				if (market_data == null)
				{
					market_data = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(_marketDataPath));
				}

				JToken version;
				if (market_data.TryGetValue("version", out version) && (market_data["version"].ToObject<string>() == Main.BuildVersion) && IsUpdated(market_data))
				{
					DateTime timestamp = market_data["timestamp"].ToObject<DateTime>();
					DateTime dayAgo = DateTime.Now.AddDays(-1);
					if (timestamp > dayAgo)
					{
						Main.AddLog("PLAT DATABASE: GOOD");
						return false;
					}
				}
			}

			Main.AddLog("PLAT DATABASE: LOADING NEW");
			market_data = new JObject();

			var sheet = _sheetsApi.GetSheet("prices!A:I");
			foreach (var row in sheet)
			{
				string name = row[0].ToString();
				if (name.Contains("Prime "))
				{
					market_data[name] = new JObject
					{
						{"plat", Double.Parse(row[8].ToString(), Main.culture)},
						{"ducats", 0},
						{"volume", Int32.Parse(row[4].ToString()) + Int32.Parse(row[6].ToString())}
					};
				}
			}

			market_data["Forma Blueprint"] = new JObject
			{
				{ "ducats", 0 },
				{ "plat", 0 },
				{ "volume", 0 },
			};

			market_data["timestamp"] = DateTime.Now.ToString("R");
			market_data["version"] = Main.BuildVersion;

			Main.AddLog("PLAT DATABASE: GOOD");
			Load_Ducats();
			if (force && relic_data != null)
			{
				Check_Ducats();
			}

			return true;
		}

		private Boolean IsUpdated(JObject market_data)
		{
			// What is so special about loki prime bp volume?
			JToken job;
			if (market_data.TryGetValue("Loki Prime Blueprint", out job))
			{
				JToken volume;
				if (job.ToObject<JObject>().TryGetValue("volume", out volume))
				{
					return true;
				}
			}

			return false;
		}

		private void Load_Ducats()
		{
			Main.AddLog("DUCAT DATABASE: LOADING NEW");
			JObject market_temp =
				JsonConvert.DeserializeObject<JObject>(
					WebClient.DownloadString("https://api.warframe.market/v1/tools/ducats"));
			foreach (var elem in market_temp["payload"]["previous_day"])
			{
				string item_name = "";
				if (!market_items.TryGetValue(elem["item"].ToObject<string>(), out item_name))
				{
					Main.AddLog("UNKNOWN MARKET ID: " + elem["item"].ToObject<String>());
				}
				else
				{
					item_name = item_name.Split('|')[0];
					JToken temp;
					if (!market_data.TryGetValue("item_name", out temp))
					{
						Main.AddLog("MISSING ITEM IN market_data:" + item_name);
					}

					if (item_name.Contains(" Set"))
					{
						Load_Items(true);
					}
					else
					{
						market_data[item_name]["ducats"] = elem["ducats"];
					}
				}
			}

			Main.AddLog("DUCAT DATABASE: GOOD");
		}

		public void Check_Ducats()
		{
			JObject job;
			List<string> needDucats = new List<string>();

			foreach (var elem in market_data)
			{
				if (elem.Key.Contains("Prime"))
				{
					job = elem.Value.ToObject<JObject>();
					if (job["ducats"].ToObject<int>() == 0)
					{
						Console.WriteLine("FOUND A ZERO: " + elem.Key);
						needDucats.Add(elem.Key);
					}
				}
			}

			foreach (var era in relic_data)
			{
				if (era.Key.Length < 5)
				{
					foreach (var relic in era.Value.ToObject<JObject>())
					{
						foreach (var rarity in relic.Value.ToObject<JObject>())
						{
							String name = rarity.Value.ToObject<string>();
							if (needDucats.Contains(name))
							{
								if (rarity.Key.Contains("rare"))
								{
									market_data[name]["ducats"] = 100;
								} else if (rarity.Key.Contains("un"))
								{
									market_data[name]["ducats"] = 45;
								}
								else
								{
									market_data[name]["ducats"] = 15;
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

		private Double GetSetPlat(JObject job, Boolean unowned = false)
		{
			JToken temp;
			Double ret = 0;
			foreach (var kvp in job["parts"].ToObject<JObject>())
			{
				int count = kvp.Value["count"].ToObject<int>();
				int owned = kvp.Value["owned"].ToObject<int>();
				if (unowned)
				{
					count -= owned;
				}

				if (db.market_data.TryGetValue(kvp.Key, out temp))
				{
					ret += count * temp["plat"].ToObject<Double>();
				} else if (db.eqmt_data.TryGetValue(kvp.Key, out temp))
				{
					// Need to confirm that this adjusted logic won't cause recursive bomb
					Double plat = GetSetPlat(temp.ToObject<JObject>());
					db.market_data[kvp.Key] = new JObject
					{
						{ "ducats", 0 },
						{ "plat", plat },
					};
					Save_Market();

					ret += count * plat;
				}
			}

			return ret;
		}

		private void Load_Market_Item(string item_name, string url)
		{
			Main.AddLog("LOADING MISSING MARKET ITEM -- " + item_name);

			JObject stats =
				JsonConvert.DeserializeObject<JObject>(
					WebClient.DownloadString("https://api.warframe.market/v1/items/" + url + "/statistics"));
			stats = stats["payload"]["statistics_closed"]["90days"].Last.ToObject<JObject>();

			JObject ducats = JsonConvert.DeserializeObject<JObject>(
				WebClient.DownloadString("https://api.warframe.market/v1/items/" + url));
			ducats = ducats["payload"]["item"].ToObject<JObject>();
			string id = ducats["id"].ToObject<string>();
			ducats = ducats["items_in_set"].AsParallel().First(part => (string) part["id"] == id).ToObject<JObject>();
			JToken temp;
			string ducat;
			if (!ducats.TryGetValue("ducats", out temp))
			{
				ducat = "0";
			}
			else
			{
				ducat = temp.ToObject<string>();
			}

			market_data[item_name] = new JObject
			{
				{ "ducats", ducat },
				{ "plat", stats["avg_price"] }
			};
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
				//Task.Factory.StartNew(Main.DoDelayWork()); // WIP
			}
		}
	}
}

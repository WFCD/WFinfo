using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using Google.Apis.Auth.OAuth2;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLua;
using Xamarin.Forms.PlatformConfiguration;
using Application = System.Windows.Application;

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

		private string officialItemStateUrl = "https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html";
		private string weaponRequirementWikiURL = "https://warframe.fandom.com/wiki/Special:Export/Module:Weapons/data";

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

				if (market_data.TryGetValue(kvp.Key, out temp))
				{
					ret += count * temp["plat"].ToObject<Double>();
				} else if (eqmt_data.TryGetValue(kvp.Key, out temp))
				{
					// Need to confirm that this adjusted logic won't cause recursive bomb
					Double plat = GetSetPlat(temp.ToObject<JObject>());
					market_data[kvp.Key] = new JObject
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

        private Boolean Load_Drop_Data(Boolean force = false)
        {
            // Temp variable for using with TryGetValue
            JToken temp = null;

            Main.AddLog("LOADING DROP DATABASE");
            WebRequest request;
			if (eqmt_data == null)
            {
                if (!File.Exists(_eqpmtDataPath))
                {
                    eqmt_data = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(_eqpmtDataPath));
                }
                else
                {
                    eqmt_data = new JObject();
                }
            }

			if ((!force) && File.Exists(_relicDataPath) && File.Exists(_eqpmtDataPath) && eqmt_data.TryGetValue("version", out temp) && eqmt_data["version"].ToObject<string>() == Main.BuildVersion)
            {
				request = WebRequest.Create(officialItemStateUrl);
                request.Method = "HEAD";
                using (WebResponse resp = request.GetResponse())
                {
                    // Move last_mod back one hour, so that it doesn't equal timestamp
					DateTime last_modified = DateTime.Parse(resp.Headers.Get("Last-Modified")).AddHours(-1);

                    if (relic_data == null)
                    {
                        relic_data = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(_relicDataPath));
                    }

                    if (name_data == null)
                    {
                        name_data =
                            JsonConvert.DeserializeObject<JObject>(File.ReadAllText(_nameDataPath));
                    }

					if ((relic_data.TryGetValue("timestamp", out temp)) && eqmt_data.TryGetValue("timestamp", out temp) && eqmt_data["timestamp"].ToObject<String>() == relic_data["timestamp"].ToObject<String>() && last_modified < relic_data["timestamp"].ToObject<DateTime>())
                    {
                        return false;
                    }
                }
            }

			Main.AddLog("LOADING NEW DROP DATABASE");

            relic_data = new JObject();
            name_data = new JObject();

            String drop_data;
            request = WebRequest.Create(officialItemStateUrl);
            using (WebResponse response = request.GetResponse())
            {
                relic_data["timestamp"] = response.Headers.Get("Last-Modified");
                eqmt_data["timestamp"] = response.Headers.Get("Last-Modified");

                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    drop_data = reader.ReadToEnd();
                }
			}

            // Load Relic Info
            // Get table start + end locations
			// There's a bit of rocket science involved. Perhaps we should consider using native HTML parsing libraries like people use XML?
			int first = drop_data.IndexOf("id=\"relicRewards\"");
            first = drop_data.IndexOf("<table>", first);
            int last = drop_data.IndexOf("</table>", first);

            // Loop through each row
            // Get start > while not at end > get last > parse > get start > goto while
            int index = drop_data.IndexOf("<tr>", first);
            int tr_stop = 0;
            while (index < last && index != -1)
            {
                tr_stop = drop_data.IndexOf("</tr>", index);
                String sub_str = drop_data.Substring(index, tr_stop - index);

                if (sub_str.Contains("Relic") && sub_str.Contains("Intact"))
                {
                    sub_str = Regex.Replace(sub_str, "<[^>]+>|\\([^\\)]+\\)", "");
                    String[] split = sub_str.Split(' ');
                    String era = split[0];
                    String relic = split[1];
                    if (!relic_data.TryGetValue(era, out temp))
                    {
                        relic_data[era] = new JObject();
                    }

                    // Will check if not vaulted in future
                    relic_data[era][relic] = new JObject
                    {
                        {"vaulted", true}
                    };

                    int cmnNum = 1;
                    int uncNum = 1;
                    index = drop_data.IndexOf("<tr", tr_stop);
                    tr_stop = drop_data.IndexOf("</tr>", index);
                    sub_str = drop_data.Substring(index, tr_stop - index);
                    while (!sub_str.Contains("blank-row"))
                    {
                        sub_str = sub_str.Replace("<tr><td>", "").Replace("</td>", "").Replace("td>", "");
                        split = sub_str.Split('<');
						String name = split[0];
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
                        } else if (!name.Contains("Prime Blueprint") && !name.Contains("Forma"))
                        {
                            name = name.Replace(" Blueprint", "");
                        }

						if (split[1].Contains("2."))
                        {
                            relic_data[era][relic]["rare1"] = name;
                        } else if (split[1].Contains("11"))
                        {
                            relic_data[era][relic]["uncommon" + uncNum.ToString()] = name;
                            uncNum += 1;
                        }
                        else
                        {
                            relic_data[era][relic]["common" + cmnNum.ToString()] = name;
                            cmnNum += 1;
                        }

                        String prime = name;
                        if (prime.IndexOf("Prime") != -1)
                        {
                            prime = prime.Substring(0, prime.IndexOf("Prime") + 5);
                            if (!eqmt_data.TryGetValue(prime, out temp))
                            {
                                eqmt_data[prime] = new JObject
                                {
                                    {"parts", new JObject()},
                                    {"type", ""},
                                    {"vaulted", true}
                                };
                            }

                            JObject job = eqmt_data[prime]["parts"].ToObject<JObject>();

                            if (!job.TryGetValue(name, out temp))
                            {
                                job = new JObject
                                {
                                    {"count", 1},
                                    {"owned", 0},
                                    {"vaulted", true}
                                };
                                eqmt_data[prime]["parts"][name] = job;
                            }

                            if (name.Contains("Harness"))
                            {
                                eqmt_data[prime]["type"] = "Archwing";
                            } else if (name.Contains("Chassis"))
                            {
                                eqmt_data[prime]["type"] = "Warframe";
                            } else if (name.Contains("Carapace") || name.Contains("Collar Blueprint"))
                            {
                                eqmt_data[prime]["type"] = "Companion";
                            }
                        }

                        if (!name_data.ContainsKey(split[0]))
                        {
                            name_data[split[0]] = name;
                        }

                        index = drop_data.IndexOf("<tr", tr_stop);
                        tr_stop = drop_data.IndexOf("</tr>", index);
                        sub_str = drop_data.Substring(index, tr_stop - index);
                    }
                }

                index = drop_data.IndexOf("<tr>", tr_stop);
			}

            Mark_All_Eqmt_Vaulted();

			// Find NOT Vauled Relics in Missions
            last = drop_data.IndexOf("id=\"relicRewards\"");
            index = drop_data.IndexOf("<tr>");
            while (index < last && index != -1)
            {
                tr_stop = drop_data.IndexOf("</tr>", index);
                String sub_str = drop_data.Substring(index, tr_stop - index);
                index = sub_str.IndexOf("Relic");
                if (index != -1)
                {
                    sub_str = sub_str.Substring(0, index - 1);
                    index = sub_str.LastIndexOf(">") + 1;
                    sub_str = sub_str.Substring(index);
                    String[] split = sub_str.Split(' ');
                    String era = split[0];
                    String relic = split[1];

					if (relic_data.TryGetValue(era, out temp))
                    {
                        relic_data[era][relic]["vaulted"] = false;
                        Mark_Eqmt_Unvaulted(era, relic);
                    }
                }

                index = drop_data.IndexOf("<tr>", tr_stop);
            }

            Get_Set_Vault_Status();
            eqmt_data["version"] = Main.BuildVersion;
            return true;
        }

        private void Mark_All_Eqmt_Vaulted()
        {
            foreach (var kvp in eqmt_data)
            {
                if (kvp.Key.Contains("Prime"))
                {
                    eqmt_data[kvp.Key]["vaulted"] = true;
                    foreach (var part in kvp.Value["parts"].ToObject<JObject>())
                    {
                        eqmt_data[kvp.Key]["parts"][part.Key]["vaulted"] = true;
                    }
                }
            }
        }

        private void Get_Set_Vault_Status()
        {
            foreach (var kvp in eqmt_data)
            {
                if (kvp.Key.Contains("Prime"))
                {
                    Boolean vaulted = false;
                    foreach (var part in kvp.Value["parts"].ToObject<JObject>())
                    {
                        if (part.Value["vaulted"].ToObject<Boolean>())
                        {
                            vaulted = true;
                            break;
                        }
                    }

                    eqmt_data[kvp.Key]["vaulted"] = vaulted;
                }
            }
        }

        private void Mark_Eqmt_Unvaulted(String era, String name)
        {
            JObject job = relic_data[era][name].ToObject<JObject>();
            JToken temp;
            foreach (var kvp in job)
            {
                String str = kvp.Value.ToObject<String>();
                if (str.IndexOf("Prime") != -1)
                {
                    // Cut the name of actual part without Prime prefix ??
                    String eqmt = str.Substring(0, str.IndexOf("Prime") + 5);
                    if (eqmt_data.TryGetValue(eqmt, out temp))
                    {
                        eqmt_data[eqmt]["parts"][str]["vaulted"] = false;
                    }
                    else
                    {
						Console.WriteLine("CANNOT FIND \"" + eqmt + "\" IN eqmt_data");
                    }
                }
            }
        }

        private Boolean Load_Eqmt_Rqmts(Boolean force = false)
        {
            // Load wiki data on prime eqmt requirements
            // Mainly weapons
			// WIP - perhaps should be rewritten to use warframe-items Github jsons

            if (!force)
            {
				DateTime timestamp = eqmt_data["rqmts_timestamp"].ToObject<DateTime>();
                DateTime dayAgo = DateTime.Now.AddDays(-1);
                if (timestamp > dayAgo)
                {
                    Main.AddLog("WIKI DATABASE: GOOD");
                    return false;
                }
            }

			Main.AddLog("LOADING NEW WIKI DATABASE");
            String data = WebClient.DownloadString(weaponRequirementWikiURL);
            int start = data.IndexOf("<timestamp>") + 11;
            int last = data.IndexOf("<", start);
            eqmt_data["rqmts_timestamp"] = DateTime.Now.ToString("R");
            data = data.Substring(data.IndexOf("{"), data.IndexOf("<text"));
            data = data.Substring(0, data.LastIndexOf("}") + 1);
            data = Regex.Replace(data, "&quot;", "\"");
            data = Regex.Replace(data, "&amp;", "&");

            NLua.LuaTable tempLua = (LuaTable)((LuaTable)_lua.DoString("return " + data)[0])["Weapons"];
            Dictionary<Object, Object> dataDict = _lua.GetTableDict(tempLua);

            JToken temp_out;

            foreach (var kvp in eqmt_data)
            {
                if (!kvp.Key.Contains("timestamp") && dataDict.ContainsKey(kvp.Key))
                {
                    eqmt_data[kvp.Key]["type"] = JToken.FromObject(((LuaTable)tempLua[kvp.Key])["Type"]);
					Dictionary<String, int> temp = new Dictionary<String, int>();

                    foreach (LuaTable part in (LuaTable)((LuaTable)((LuaTable)((LuaTable)tempLua[kvp.Key])["Cost"])["Parts"]).Values)
                    {
                        if (part["Type"].ToString() == "PrimePart")
                        {
                            foreach (var relic_part in kvp.Value["Parts"].ToObject<JObject>())
                            {
                                if (relic_part.Key.Contains(part["Name"].ToString()))
                                {
                                    eqmt_data[kvp.Key]["parts"][relic_part.Key]["count"] =
                                        JToken.FromObject(part["Count"]);
                                    break;
                                }
                            }
                        } else if (part["Type"].ToString() == "Weapon" && part["Name"].ToString().Contains("Prime"))
                        {
                            if (!temp.ContainsKey(part["Name"].ToString()))
                            {
                                temp[part["Name"].ToString()] = 0;
                            }

                            temp[part["Name"].ToString()] += (int)part["Count"];
                        }

                        if (temp.Count() > 0)
                        {
                            foreach (var entry in temp)
                            {
                                JObject job = eqmt_data[kvp.Key]["parts"].ToObject<JObject>();
                                if (!job.TryGetValue(entry.Key, out temp_out))
                                {
									eqmt_data[kvp.Key]["parts"][entry.Key] = new JObject
                                    {
                                        {"owned", 0},
                                        {"vaulted", false}
                                    };
                                }

                                eqmt_data[kvp.Key]["parts"][entry.Key]["count"] = entry.Value;
                            }
                        }
                    }

                }
            }

            return true;
        }

        private Boolean Update()
        {
            Main.AddLog("UPDATING DATABASES");
            Boolean save_market = Load_Items();
            JToken temp_out;

            foreach (var elem in market_items)
            {
                if (elem.Key != "version")
                {
                    String[] split = elem.Value.Split('|');
                    String item_name = split[0];
                    String item_url = split[1];
					if (!item_name.Contains(" Set") && !market_data.TryGetValue(item_name, out temp_out))
                    {
                        Load_Market_Item(item_name, item_url);
                        save_market = true;
                    }
                }
            }

            Boolean save_drop = Load_Drop_Data();
            save_drop = Load_Eqmt_Rqmts(save_drop);
            if (save_drop)
            {
				Save_Eqmt();
                Save_Relics();
				Save_Names();
            }

            if (save_market || save_drop)
            {
				Check_Ducats();
				Save_Market();
            }

            if (save_market || save_drop)
            {
				Main.AddLog("DATABASES NEEDED UPDATES");
            }
            else
            {
				Main.AddLog("DATABASES DID NOT NEED UPDATES");
            }

            return save_market || save_drop;
        }

        public void ForceMarketUpdate()
        {
            Main.AddLog("FORCING MARKET UPDATE");
            Load_Items(true);
            Load_Market(true);

            JToken temp_out;
            foreach (var elem in market_items)
            {
                if (elem.Key != "version")
                {
                    String[] split = elem.Value.Split('|');
                    String item_name = split[0];
                    String item_url = split[1];
                    if (!item_name.Contains(" Set") && !market_data.TryGetValue(item_name, out temp_out))
                    {
						Load_Market_Item(item_name, item_url);
                    }
                }
            }
			Save_Market();
        }

        public void ForceEqmtUpdate()
        {
            Main.AddLog("FORCING EQMT UPDATE");
            Load_Drop_Data(true);
            Load_Eqmt_Rqmts(true);
			Save_Eqmt();
			Save_Relics();
			Save_Names();
        }

        public void ForceWikiUpdate()
        {
            Main.AddLog("FORCING WIKI UPDATE");
            Load_Eqmt_Rqmts(true);
			Save_Eqmt();
        }

        public JArray GetPlatLive(String item_url)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            JObject stats = JsonConvert.DeserializeObject<JObject>(
                WebClient.DownloadString("https://api.warframe.market/v1/items/" + item_url + "/orders"));
			stopWatch.Stop();
			Console.WriteLine("Time taken to download all listings: " + stopWatch.ElapsedMilliseconds);
			
            stopWatch.Start();
            JArray sellers = new JArray();
            foreach (var listing in stats["payload"]["orders"])
            {
                if (listing["order_type"].ToObject<String>() == "buy" ||
                    listing["user"]["status"].ToObject<String>() == "offline")
                {
                    continue;
                }

                sellers.Add(listing);
            }
			stopWatch.Stop();
            Console.WriteLine("Time taken to process sell and online listings: " + stopWatch.ElapsedMilliseconds);
            Console.WriteLine(sellers);
            return sellers;
        }

        public Boolean IsPartVaulted(String name)
        {
            String eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            return eqmt_data[eqmt][name]["vaulted"].ToObject<Boolean>();
        }

		// To be refactored into different name - PartsOwned?
        public String IsPartOwned(String name)
        {
            String eqmt = name.Substring(0, name.IndexOf("Prime") + 5);
            return eqmt_data[eqmt]["parts"][name]["owned"].ToString() + "/" + eqmt_data[eqmt]["parts"][name]["count"].ToString();
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

        Char[,] ReplacementList = null;

        private int GetDifference(Char c1, Char c2)
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

		private int LevDist(String s, String t)
		{
            //_________________________________________________________________________
            // LevDist determines how "close" a jumbled name is to an actual name
            // For example: Nuvo Prime is closer to Nova Prime (2) then Ash Prime (4)
            //     https://en.wikipedia.org/wiki/Levenshtein_distance
            // _________________________________________________________________________
            s = s.Replace("*", "").ToLower();
            t = t.Replace("*", "").ToLower();
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n+1, m+1];

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

        private int LevDist2(String str1, String str2, int limit = -1)
        {
            int num;
            Boolean maxY;
            int temp;
            Boolean maxX;
            String s = str1.ToLower();
            String t = str2.ToLower();
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
                int currX = 0;
                int currY = 0;

                do
                {
                    currX = activeX[0];
                    activeX.RemoveAt(0);
                    currY = activeY[0];
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

        public String GetPartName(string name)
        {
			// Checks the levDist of a string and returns the index in Names() of the closest part
            String lowest = null;
            int low = 9999;
            foreach (var prop in name_data)
            {
                int val = LevDist(prop.Key, name);
                if (val < low)
                {
                    low = val;
                    lowest = prop.Value.ToObject<String>();
                }
            }

            Main.AddLog("FOUND PART: " + lowest + Environment.NewLine + Environment.NewLine + "FROM: " + name);
            return lowest;
        }

        public String GetSetName(String name)
        {
            name = name.ToLower();
            name = name.Replace("*", "");
            String rStr = null;
            int low = 9999;

            foreach (var prop in market_data)
            {
                String str = prop.Key.ToLower();
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
                int val = LevDist(str, name);
                if (val < low)
                {
                    low = val;
                    rStr = prop.Key;
                }
            }

            rStr = rStr.ToLower();
            rStr = rStr.Replace("neuroptics", "");
            rStr = rStr.Replace("chassis", "");
            rStr = rStr.Replace("sytems", "");
            rStr = rStr.Replace("carapace", "");
            rStr = rStr.Replace("cerebrum", "");
            rStr = rStr.Replace("blueprint", "");
            rStr = rStr.Replace("harness", "");
            rStr = rStr.Replace("blade", "");
            rStr = rStr.Replace("pouch", "");
            rStr = rStr.Replace("head", "");
            rStr = rStr.Replace("barrel", "");
            rStr = rStr.Replace("receiver", "");
            rStr = rStr.Replace("stock", "");
            rStr = rStr.Replace("disc", "");
            rStr = rStr.Replace("grip", "");
            rStr = rStr.Replace("string", "");
            rStr = rStr.Replace("handle", "");
            rStr = rStr.Replace("ornament", "");
            rStr = rStr.Replace("wings", "");
            rStr = rStr.Replace("blades", "");
            rStr = rStr.Replace("hilt", "");
            rStr = rStr.TrimEnd() + " set";
            rStr = Main.culture.TextInfo.ToTitleCase(rStr);
            return rStr;
        }

        public String GetRelicName(String string1)
        {
            String lowest = null;
            int low = 999;
            int temp = 0;
            String era_name = null;
            JObject job = null;

            foreach (var era in relic_data)
            {
                if (!era.Key.Contains("timestamp"))
                {
                    temp = LevDist2(string1, era.Key + "??RELIC", low);
                    if (temp < low)
                    {
                        job = era.Value.ToObject<JObject>();
                        era_name = era.Key;
                        low = temp;
                    }
                }
            }

            low = 999;
            foreach (var relic in job)
            {
                temp = LevDist2(string1, era_name + relic.Key + "RELIC", low);
                if (temp < low)
                {
                    lowest = era_name + " " + relic.Key;
                    low = temp;
                }
            }

            return lowest;
        }

        private Boolean waiting = false;
        private void watcher_Created(Object sender, FileSystemEventArgs e)
        {
            waiting = true;
        }

        private void watcher_Changed(Object sender, FileSystemEventArgs e)
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

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AllFilteredGenerator
{

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
#if DEBUG
                var processPath = Environment.ProcessPath;
                var processDir = Path.GetDirectoryName(processPath) ?? throw new Exception("processDir unknown");
                // inject file path arguments for same folder as binary, for debug purposes
                args = [
                    Path.Combine(processDir, "All.json"),
                    Path.Combine(processDir, "output.json"),
                ];
#else
                Console.WriteLine("Must provide path to All.json and output as arguments");
                Environment.Exit(1);
#endif
            }

            var allJsonPath = args[0];

            var savePath = args[1];

            if (!File.Exists(allJsonPath))
            {
                Console.WriteLine("All.json file not found at path:" + allJsonPath);
                Environment.Exit(1);
            }

            using var allJsonFileStream = new FileStream(allJsonPath, FileMode.Open, FileAccess.Read);
            var json = JsonNode.Parse(allJsonFileStream);

            if (json is null || json.GetValueKind() is not JsonValueKind.Array)
            {
                throw new ArgumentException("All.json not a json array");
            }

            var jsonArray = json.AsArray();

            var time = DateTime.UtcNow;

            // parse data
            var (primes, relics, formaSources, errors) = Parse(jsonArray);

            // combine data
            Relic.SyncRelicsWithParts(primes, relics, errors);

            Relic.SyncRelicsWithForma(formaSources, relics, errors);

            PrimeEquipment.SyncIndependentParts(primes, errors);

            var relicsByEra = Relic.DivideByEra(relics, errors);

            // Produce output

            var relicsObj = Relic.ToJson(relicsByEra);

            var eqmtObj = PrimeEquipment.ToJson(primes);

            var ignoredItems = new List<string>
            {
                "Forma Blueprint",
                "Exilus Weapon Adapter Blueprint",
                "Kuva",
                "Riven Sliver",
                "Ayatan Amber Star"
            };

            var ignoredItemsObj = IgnoredItemsToJson(ignoredItems);

            var errorsArr = ErrorsToJson(errors);

            var outputObj = new JsonObject
            {
                { "errors", errorsArr },
                { "timestamp", time.ToString("O", CultureInfo.InvariantCulture) },
                { "relics", relicsObj },
                { "eqmt", eqmtObj },
                { "ignored_items", ignoredItemsObj }
            };

            var serializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = false
            };

            var outputString = outputObj.ToJsonString(serializerOptions);

            File.WriteAllText(savePath, outputString);
        }

        private static JsonArray ErrorsToJson(List<string> errors)
        {
            var errorsArr = new JsonArray();

            foreach (var error in errors)
            {
                JsonNode node = JsonValue.Create(error, null);
                errorsArr.Add(node);
            }

            return errorsArr;
        }

        private static JsonObject IgnoredItemsToJson(List<string> ignoredItems)
        {
            var allIgnoredItemsObj = new JsonObject();
            foreach (var ignoredItem in ignoredItems)
            {
                var ignoredItemObj = new JsonObject
                {
                    { "plat", 0 },
                    { "ducats", 0 },
                    { "volume", 0 }
                };

                allIgnoredItemsObj.Add(ignoredItem, ignoredItemObj);
            }

            return allIgnoredItemsObj;
        }

        private static (List<PrimeEquipment> Primes, List<Relic> Relics, List<ItemRelicInfo> FormaRelicInfo, List<string> Errors) Parse(JsonArray jsonArray)
        {
            var errors = new List<string>();
            var relics = new List<Relic>();
            var primes = new List<PrimeEquipment>();
            var formaRelicInfo = new List<ItemRelicInfo>();

            foreach (var elem in jsonArray)
            {
                if (elem is null || elem.GetValueKind() is not JsonValueKind.Object)
                {
                    continue;
                }

                var elemObj = elem.AsObject();


                var name = elemObj.GetStringProperty("name");

                if (name is null)
                {
                    continue;
                }

                var type = elemObj.GetStringProperty("type") ?? "None";

                if (name.Contains("Prime") && !name.Contains("Primed"))
                {
                    var prime = PrimeEquipment.TryParse(name, elemObj, errors);

                    if (prime != null)
                    {
                        primes.Add(prime);
                    }
                }
                else if (type.Equals("Relic", StringComparison.InvariantCultureIgnoreCase) && name.Contains("Intact") && !name.Contains("Requiem"))
                {
                    var relic = Relic.TryParse(name, elemObj, out var formaEntries);

                    if (relic != null)
                    {
                        relics.Add(relic);
                    }

                    formaRelicInfo.AddRange(formaEntries);
                }
                else
                {
                    // old code, doesn't handle 2X Forma Blueprint
                    //var category = elemObj.GetStringProperty("category");
                    //if (category is not null && category.Equals("Misc", StringComparison.InvariantCultureIgnoreCase) && name.Equals("Forma"))
                    //{
                    //    var formaSources = ItemRelicInfo.TryParseDrops(elemObj);
                    //    if (formaSources.Count > 0)
                    //    {
                    //        formaRelicInfo.AddRange(formaSources);
                    //    }
                    //}
                }
            }

            return (primes, relics, formaRelicInfo, errors);
        }
    }
}
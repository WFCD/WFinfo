

using System.Text.Json;
using System.Text.Json.Nodes;

namespace AllFilteredGenerator
{
    /// <summary>
    /// A relic
    /// </summary>
    public class Relic
    {
        private Relic(string eraName, string nameInEra, bool vaulted)
        {
            EraName = eraName;
            NameInEra = nameInEra;
            Vaulted = vaulted;
        }

        public string EraName { get; }
        public string NameInEra { get; }
        public bool Vaulted { get; }

        public Dictionary<string, List<string>> DropsByRarity { get; } = [];

        public static void SyncRelicsWithParts(List<PrimeEquipment> primes, List<Relic> relics, List<string> errors)
        {
            foreach (var prime in primes)
            {
                foreach (var part in prime.Parts)
                {
                    var partVaulted = true;
                    foreach (var relicInfo in part.DroppingRelics)
                    {
                        var matchingRelic = relics.FirstOrDefault(x => x.EraName.Equals(relicInfo.EraName, StringComparison.InvariantCultureIgnoreCase) && x.NameInEra.Equals(relicInfo.NameInEra, StringComparison.InvariantCultureIgnoreCase));

                        if (matchingRelic is null)
                        {
                            errors.Add("MISSING: " + relicInfo.EraName + " " + relicInfo.NameInEra + " Relic");
                        }
                        else
                        {
                            if (!matchingRelic.DropsByRarity.TryGetValue(relicInfo.Rarity, out var rarityList))
                            {
                                rarityList = [];
                                matchingRelic.DropsByRarity.Add(relicInfo.Rarity, rarityList);
                            }

                            if (!matchingRelic.Vaulted)
                            {
                                partVaulted = false;
                            }

                            rarityList.Add(part.NameToSave);
                        }
                    }
                    part.Vaulted = partVaulted;
                    prime.Vaulted |= partVaulted;
                }
            }
        }

        public static void SyncRelicsWithForma(List<ItemRelicInfo> formaSources, List<Relic> relics, List<string> errors)
        {
            foreach (var relicInfo in formaSources)
            {
                var matchingRelic = relics.FirstOrDefault(x => x.EraName.Equals(relicInfo.EraName, StringComparison.InvariantCultureIgnoreCase) && x.NameInEra.Equals(relicInfo.NameInEra, StringComparison.InvariantCultureIgnoreCase));

                if (matchingRelic is null)
                {
                    errors.Add("MISSING: " + relicInfo.EraName + " " + relicInfo.NameInEra + " Relic");
                }
                else
                {
                    if (!matchingRelic.DropsByRarity.TryGetValue(relicInfo.Rarity, out var rarityList))
                    {
                        rarityList = [];
                        matchingRelic.DropsByRarity.Add(relicInfo.Rarity, rarityList);
                    }

                    rarityList.Add("Forma Blueprint");
                }
            }
        }

        public static Dictionary<string, List<Relic>> DivideByEra(List<Relic> relics, List<string> errors)
        {
            var relicsByEra = new Dictionary<string, List<Relic>>();

            foreach (var relic in relics)
            {
                if (!relic.DropsByRarity.TryGetValue("Common", out var commonList))
                {
                    commonList = [];
                }

                if (!relic.DropsByRarity.TryGetValue("Uncommon", out var uncommonList))
                {
                    uncommonList = [];
                }

                if (!relic.DropsByRarity.TryGetValue("Rare", out var rareList))
                {
                    rareList = [];
                }

                if (commonList.Count < 3 || uncommonList.Count < 2 || rareList.Count < 1)
                {
                    errors.Add(relic.EraName + " " + relic.NameInEra + " IS MISSING DROPS");
                }

                if (commonList.Count > 3 || uncommonList.Count > 2 || rareList.Count > 1)
                {
                    errors.Add(relic.EraName + " " + relic.NameInEra + " HAS EXTRA DROPS");
                }

                if (!relicsByEra.TryGetValue(relic.EraName, out var eraList))
                {
                    eraList = [];
                    relicsByEra.Add(relic.EraName, eraList);
                }

                eraList.Add(relic);
            }

            return relicsByEra;
        }

        public static JsonObject ToJson(Dictionary<string, List<Relic>> relicsByEra)
        {
            var allRelicsObj = new JsonObject();

            var rarityOrder = new List<string>
                    {
                        "Rare",
                        "Uncommon",
                        "Common"
                    };

            foreach (var relicEra in relicsByEra)
            {
                var eraObj = new JsonObject();

                foreach (var relic in relicEra.Value)
                {
                    var relicObj = new JsonObject
                    {
                        { "vaulted", relic.Vaulted }
                    };

                    foreach (var rarity in rarityOrder)
                    {
                        if (relic.DropsByRarity.TryGetValue(rarity, out var rarityEntry))
                        {
                            for (int i = 0; i < rarityEntry.Count; i++)
                            {
                                relicObj.Add(rarity.ToLowerInvariant() + (i + 1), rarityEntry[i]);
                            }
                        }
                    }

                    eraObj.Add(relic.NameInEra, relicObj);
                }

                allRelicsObj.Add(relicEra.Key, eraObj);
            }

            return allRelicsObj;
        }

        public static Relic? TryParse(string name, JsonObject elemObj, out List<ItemRelicInfo> formaEntries)
        {
            formaEntries = [];
            var names = name.Split(' ');
            if (names.Length < 2)
            {
                return null;
            }

            var eraName = names[0];
            var nameInEra = names[1];

            var vaulted = !elemObj.TryGetPropertyValue("drops", out var _);

            var rewards = elemObj.GetArrayProperty("rewards");
            if (rewards is not null)
            {
                foreach (var rewardNode in rewards)
                {
                    if (rewardNode is null || rewardNode.GetValueKind() is not JsonValueKind.Object)
                    {
                        continue;
                    }

                    var rewardObj = rewardNode.AsObject();

                    var itemObj = rewardObj.GetObjectProperty("item");

                    if (itemObj is null)
                    {
                        continue;
                    }

                    var itemName = itemObj.GetStringProperty("name");


                    if (itemName is null)
                    {
                        continue;
                    }

                    if (!itemName.Contains("Forma", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var rarity = rewardObj.GetRarity();

                    if (rarity is null)
                    {
                        continue;
                    }

                    formaEntries.Add(new ItemRelicInfo(eraName, nameInEra, rarity));
                }
            }

            return new Relic(eraName, nameInEra, vaulted);
        }
    }
}

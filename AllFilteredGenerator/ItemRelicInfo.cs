

using System.Text.Json;
using System.Text.Json.Nodes;

namespace AllFilteredGenerator
{
    /// <summary>
    /// Info about a relic that an item appears in, and at which rarity
    /// </summary>
    public class ItemRelicInfo
    {
        public ItemRelicInfo(string eraName, string nameInEra, string rarity)
        {
            EraName = eraName;
            NameInEra = nameInEra;
            Rarity = rarity;
        }

        public string EraName { get; }
        public string NameInEra { get; }
        public string Rarity { get; }

        public static List<ItemRelicInfo> TryParseDrops(JsonObject obj)
        {
            var result = new List<ItemRelicInfo>();
            var drops = obj.GetArrayProperty("drops");

            if (drops is not null)
            {
                foreach (var drop in drops)
                {
                    if (drop is null || drop.GetValueKind() is not JsonValueKind.Object)
                    {
                        continue;
                    }

                    var dropObj = drop.AsObject();

                    var location = dropObj.GetStringProperty("location");

                    if (location is null)
                    {
                        continue;
                    }

                    if (location.Contains("Relic") && !location.Contains('('))
                    {
                        var rarity = dropObj.GetRarity();

                        if (rarity is null)
                        {
                            continue;
                        }

                        var splitLocationName = location.Split(' ');
                        if (splitLocationName.Length < 2)
                        {
                            continue;
                        }

                        var eraName = splitLocationName[0];
                        var nameInEra = splitLocationName[1];

                        result.Add(new ItemRelicInfo(eraName, nameInEra, rarity));

                    }
                    else
                    {
                        continue;
                    }

                }
            }
            return result;
        }
    }
}

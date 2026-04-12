using System.Text.Json.Nodes;

namespace AllFilteredGenerator
{
    /// <summary>
    /// Standard kind of component to a prime
    /// </summary>
    public class PrimeComponent
    {
        private PrimeComponent(string nameToSave, int count, int savedDucatCount, bool vaulted, List<ItemRelicInfo> relics)
        {
            NameToSave = nameToSave;
            Count = count;
            SavedDucatCount = savedDucatCount;
            Vaulted = vaulted;
            DroppingRelics = relics;
        }

        public string NameToSave { get; }
        public int Count { get; }
        public int SavedDucatCount { get; }
        public bool Vaulted { get; set; }
        public List<ItemRelicInfo> DroppingRelics { get; }

        public static PrimeComponent? TryParse(string objectName, string componentName, JsonObject componentObj, List<string> errors)
        {

            var count = componentObj.GetIntProperty("itemCount") ?? 0;

            int savedDucatCount;

            var parsedDucats = componentObj.GetIntProperty("ducats");

            if (parsedDucats.HasValue)
            {
                savedDucatCount = parsedDucats.Value;
            }
            else
            {
                var parsedPrimeSellingPrice = componentObj.GetIntProperty("primeSellingPrice");

                if (parsedPrimeSellingPrice.HasValue)
                {
                    savedDucatCount = parsedPrimeSellingPrice.Value;
                }
                else
                {
                    savedDucatCount = 0;
                    errors.Add(objectName + " " + componentName + " IS MISSING ducats/primeSellingPrice");
                }
            }

            var vaulted = true;

            var relics = ItemRelicInfo.TryParseDrops(componentObj);

            var nameToSave = componentName.Contains("Prime")
                ? componentName
                : (objectName + " " + componentName);

            return new PrimeComponent(nameToSave, count, savedDucatCount, vaulted, relics);
        }
    }
}

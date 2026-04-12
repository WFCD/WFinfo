using System.Text.Json;
using System.Text.Json.Nodes;

namespace AllFilteredGenerator
{
    /// <summary>
    /// The complete prime
    /// </summary>
    public class PrimeEquipment
    {
        private PrimeEquipment(string name, string itemTpye, List<PrimeComponent> parts, List<IndependentPrimeComponent> independentParts)
        {
            Name = name;
            ItemType = itemTpye;
            Parts = parts;
            IndependentParts = independentParts;
        }

        public string Name { get; }

        public string ItemType { get; }

        public bool Vaulted { get; set; } = false;

        public List<PrimeComponent> Parts { get; }

        public List<IndependentPrimeComponent> IndependentParts { get; }

        public static void SyncIndependentParts(List<PrimeEquipment> primes, List<string> errors)
        {
            foreach (var prime in primes)
            {
                foreach (var part in prime.IndependentParts)
                {
                    var matchingItem = primes.FirstOrDefault(x => x.Name.Equals(part.Name, StringComparison.InvariantCultureIgnoreCase));

                    if (matchingItem is null)
                    {
                        errors.Add(part.Name + " is non-recipe and missing");
                    }
                    else
                    {
                        part.Vaulted = matchingItem.Vaulted;
                        prime.Vaulted |= matchingItem.Vaulted;
                    }
                }

                if (prime.IndependentParts.Any(x => x.Vaulted != prime.Vaulted) || prime.Parts.Any(x => x.Vaulted != prime.Vaulted))
                {
                    errors.Add(prime.Name + " is partially vaulted");
                }
            }
        }

        public static JsonObject ToJson(List<PrimeEquipment> primes)
        {
            var allPrimesObj = new JsonObject();
            foreach (var prime in primes)
            {
                var primeObj = new JsonObject
                {
                    { "type", prime.ItemType },
                    { "vaulted", prime.Vaulted }
                };

                var partsObj = new JsonObject();

                foreach (var part in prime.Parts)
                {
                    var partObj = new JsonObject
                    {
                        { "count", part.Count },
                        { "ducats", part.SavedDucatCount },
                        { "vaulted", part.Vaulted }
                    };

                    partsObj.Add(part.NameToSave, partObj);
                }

                foreach (var part in prime.IndependentParts)
                {
                    var partObj = new JsonObject
                    {
                        { "count", part.Count },
                        { "vaulted", part.Vaulted }
                    };

                    partsObj.Add(part.Name, partObj);
                }

                primeObj.Add("parts", partsObj);

                allPrimesObj.Add(prime.Name, primeObj);
            }

            return allPrimesObj;
        }

        public static PrimeEquipment? TryParse(string name, JsonObject elemObj, List<string> errors)
        {
            var parts = new List<PrimeComponent>();
            var independentParts = new List<IndependentPrimeComponent>();

            var itemType = elemObj.GetStringProperty("category") ?? "Unknown";

            var components = elemObj.GetArrayProperty("components");

            if (components is null)
            {
                return null;
            }

            foreach (var component in components)
            {
                if (component is null || component.GetValueKind() is not JsonValueKind.Object)
                {
                    continue;
                }

                var componentObj = component.AsObject();


                var componentName = componentObj.GetStringProperty("name");

                if (componentName is null)
                {
                    continue;
                }

                var componentUniqueName = componentObj.GetStringProperty("uniqueName");

                if (componentUniqueName is null)
                {
                    continue;
                }

                if (!componentUniqueName.Contains("Prime"))
                {
                    continue;
                }

                if (componentUniqueName.Contains("Recipes"))
                {
                    var parsed = PrimeComponent.TryParse(name, componentName, componentObj, errors);
                    if (parsed != null)
                    {
                        parts.Add(parsed);
                    }
                }
                else
                {
                    var parsed = IndependentPrimeComponent.TryParse(componentName, componentObj);
                    var alreadyParsed = independentParts.FirstOrDefault(x => x.Name.Equals(parsed.Name, StringComparison.OrdinalIgnoreCase));

                    if (alreadyParsed is not null)
                    {
                        alreadyParsed.Count += parsed.Count;
                    }
                    else
                    {
                        independentParts.Add(parsed);
                    }
                }
            }

            if (parts.Count > 0 || independentParts.Count > 0)
            {
                return new PrimeEquipment(name, itemType, parts, independentParts);
            }

            return null;
        }
    }
}

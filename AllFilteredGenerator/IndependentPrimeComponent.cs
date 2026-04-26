using System.Text.Json.Nodes;

namespace AllFilteredGenerator
{
    /// <summary>
    /// Typically a one hand variant used as recipe for dual-wield. For example, Bronco Prime used for Akbronco Prime
    /// </summary>
    public class IndependentPrimeComponent
    {
        private IndependentPrimeComponent(string name, int count, bool vaulted)
        {
            Name = name;
            Count = count;
            Vaulted = vaulted;
        }

        public string Name { get; }
        public int Count { get; set; }
        public bool Vaulted { get; set; }

        public static IndependentPrimeComponent TryParse(string componentName, JsonObject componentObj)
        {
            var vaulted = true;

            var count = componentObj.GetIntProperty("itemCount") ?? 0;

            return new IndependentPrimeComponent(componentName, count, vaulted);
        }
    }
}

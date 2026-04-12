
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AllFilteredGenerator
{
    public static class JsonExtensions
    {
        public static JsonArray? GetArrayProperty(this JsonObject jsonObject, string propertyName)
        {
            if (!jsonObject.TryGetPropertyValue(propertyName, out var propNode) || propNode is null || propNode.GetValueKind() is not JsonValueKind.Array)
            {
                return null;
            }

            return propNode.AsArray();
        }

        public static JsonObject? GetObjectProperty(this JsonObject jsonObject, string propertyName)
        {
            if (!jsonObject.TryGetPropertyValue(propertyName, out var propNode) || propNode is null || propNode.GetValueKind() is not JsonValueKind.Object)
            {
                return null;
            }

            return propNode.AsObject();
        }

        public static string? GetStringProperty(this JsonObject jsonObject, string propertyName)
        {
            if (!jsonObject.TryGetPropertyValue(propertyName, out var propNode) || propNode is null || propNode.GetValueKind() is not JsonValueKind.String)
            {
                return null;
            }

            return propNode.ToString();
        }

        public static int? GetIntProperty(this JsonObject jsonObject, string propertyName)
        {
            if (!jsonObject.TryGetPropertyValue(propertyName, out var propNode) || propNode is null || propNode.GetValueKind() is not (JsonValueKind.String or JsonValueKind.Number))
            {
                return null;
            }

            if (!int.TryParse(propNode.ToString(), out var value))
            {
                return null;
            }

            return value;
        }

        public static double? GetDoubleProperty(this JsonObject jsonObject, string propertyName)
        {
            if (!jsonObject.TryGetPropertyValue(propertyName, out var propNode) || propNode is null || propNode.GetValueKind() is not (JsonValueKind.String or JsonValueKind.Number))
            {
                return null;
            }

            if (!double.TryParse(propNode.ToString(), out var value))
            {
                return null;
            }

            return value;
        }

        public static string? GetRarity(this JsonObject jsonObject)
        {
            var rarity = jsonObject.GetStringProperty("rarity");

            if (rarity is null)
            {
                return null;
            }

            var chance = jsonObject.GetDoubleProperty("chance");
            if (chance.HasValue)
            {
                int chancePercent;
                if (chance < 1)
                {
                    chancePercent = (int)Math.Round(chance.Value * 100);
                }
                else
                {
                    chancePercent = (int)Math.Round(chance.Value);
                }

                if (rarity.Equals("Uncommon", StringComparison.InvariantCultureIgnoreCase) && chancePercent > 11)
                {
                    rarity = "Common";
                }
            }

            return rarity;
        }
    }
}

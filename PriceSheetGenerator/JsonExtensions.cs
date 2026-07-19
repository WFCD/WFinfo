
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PriceSheetGenerator
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

            if (!int.TryParse(propNode.ToString(), CultureInfo.InvariantCulture, out var value))
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

            if (!double.TryParse(propNode.ToString(), CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            return value;
        }

        public static void WriteToFile(this JsonNode jsonObject, string filePath, bool makeBackup)
        {
            if (makeBackup)
            {
                var bakPath = filePath + ".bak";

                if (File.Exists(filePath))
                {
                    File.Move(filePath, bakPath, true);
                }
            }           

            var serializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = false
            };

            var outputString = jsonObject.ToJsonString(serializerOptions);

            File.WriteAllText(filePath, outputString);
        }
    }
}

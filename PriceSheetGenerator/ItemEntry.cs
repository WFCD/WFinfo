using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PriceSheetGenerator
{
    class ItemEntry
    {
        private record StatisticsEntry(DateTime Time, int Volume, double WaPrice, double? MovingAveragePrice);

        private const string IdSaveName = "id";
        private const string SlugSaveName = "slug";
        private const string NameSaveName = "name";
        private const string YesterdayVolSaveName = "yesterday_volume";
        private const string TodayVolSaveName = "today_volume";
        private const string CustomAvgSaveName = "custom_avg";

        public ItemEntry(string id, string slug, string name)
        {
            Id = id;
            Slug = slug;
            Name = name;
        }

        public bool IsDeleted { get; private set; }
        public string Id { get; }
        public string Slug { get; private set; }
        public string Name { get; private set; }
        public bool StatsValid { get; private set; }
        public int YesterdayVol { get; private set; }
        public int TodayVol { get; private set; }
        public double CustomAvg { get; private set; }

        public void MarkAsDeleted(bool isDeleted) => IsDeleted = isDeleted;

        public void SetNameAndSlug(string name, string slug)
        {
            Name = name;
            Slug = slug;
        }

        public void WriteStats(int yesterdayVol, int todayVol, double customAvg)
        {
            YesterdayVol = yesterdayVol;
            TodayVol = todayVol;
            CustomAvg = customAvg;
            StatsValid = true;
        }

        public void WipeStats()
        {
            StatsValid = false;
        }

        public static bool TryLoad(JsonObject obj, [NotNullWhen(true)] out ItemEntry? result)
        {
            result = null;

            var id = obj.GetStringProperty(IdSaveName);
            var slug = obj.GetStringProperty(SlugSaveName);
            var name = obj.GetStringProperty(NameSaveName);

            if (id is null || slug is null || name is null)
            {
                return false;
            }

            result = new ItemEntry(id, slug, name);

            // optional
            var yesterdayVolume = obj.GetIntProperty(YesterdayVolSaveName);
            var todayVolume = obj.GetIntProperty(TodayVolSaveName);
            var customAvg = obj.GetDoubleProperty(CustomAvgSaveName);

            if (yesterdayVolume is not null && todayVolume is not null && customAvg is not null)
            {
                result.WriteStats(yesterdayVolume.Value, todayVolume.Value, customAvg.Value);
            }

            return true;
        }

        public JsonNode SaveInternal()
        {
            if (IsDeleted)
            {
                throw new Exception("Deleted ItemEntry should not be saved");
            }

            var obj = new JsonObject
            {
                [IdSaveName] = Id,
                [SlugSaveName] = Slug,
                [NameSaveName] = Name
            };

            if (StatsValid)
            {
                // only save stats if they're actually valid, otherwise only save item existence
                obj[YesterdayVolSaveName] = YesterdayVol;
                obj[TodayVolSaveName] = TodayVol;
                obj[CustomAvgSaveName] = CustomAvg;
            }

            return obj;
        }

        public JsonNode? ExportSave()
        {
            if (IsDeleted || !StatsValid)
            {
                return null;
            }
            var obj = new JsonObject
            {
                ["name"] = Name,
                ["yesterday_vol"] = YesterdayVol.ToString(CultureInfo.InvariantCulture),
                ["today_vol"] = TodayVol.ToString(CultureInfo.InvariantCulture),
                ["custom_avg"] = CustomAvg.ToString("F1", CultureInfo.InvariantCulture),
            };

            return obj;
        }

        public void ParseWfmStatistics(JsonObject obj)
        {
            var payloadObj = obj.GetObjectProperty("payload") ?? throw new Exception("Unexpected json format. Property \"payload\" missing or wrong format on object: " + obj);
            var closedObj = payloadObj.GetObjectProperty("statistics_closed") ?? throw new Exception("Unexpected json format. Property \"payload\" -> \"statistics_closed\" missing or wrong format on object: " + obj);
            var hoursArr = closedObj.GetArrayProperty("48hours") ?? throw new Exception("Unexpected json format. Property \"payload\" -> \"statistics_closed\" -> \"48hours\" missing or wrong format on object: " + obj);
            var dataPoints = new List<StatisticsEntry>();

            foreach (var dataPoint in hoursArr)
            {
                if (dataPoint is null || 
                    dataPoint.GetValueKind() is not JsonValueKind.Object)
                {
                    throw new Exception("Unexpected json format. Statistics entry on item " + Slug + " missing or wrong format. Object: " + dataPoint);
                }

                var dataPointObj = dataPoint.AsObject();

                var timeString = dataPointObj.GetStringProperty("datetime") ?? throw new Exception("Unexpected json format. Property \"datetime\" on statistics entry on item " + Slug + " missing or wrong format. Object: " + dataPoint);

                if (!DateTime.TryParse(timeString, CultureInfo.InvariantCulture, out var time))
                {
                    throw new Exception("Unexpected json format. Property \"datetime\" on statistics entry on item " + Slug + " could not be parsed to DateTime type. Object: " + dataPoint);
                }

                var volume = dataPointObj.GetIntProperty("volume") ?? throw new Exception("Unexpected json format. Property \"volume\" on statistics entry on item " + Slug + " missing or wrong format. Object: " + dataPoint);
                var waPrice = dataPointObj.GetDoubleProperty("wa_price") ?? throw new Exception("Unexpected json format. Property \"wa_price\" on statistics entry on item " + Slug + " missing or wrong format. Object: " + dataPoint);
                var movingAvgPrice = dataPointObj.GetDoubleProperty("moving_avg");

                // moving average allowed to be missing (because it can be)

                dataPoints.Add(new StatisticsEntry(time, volume, waPrice, movingAvgPrice));
            }

            if (dataPoints.Count == 0)
            {
                WipeStats();
                return;
            }

            var mostRecent = dataPoints.First();

            var oneDayAgo = DateTime.UtcNow.AddDays(-1);

            // not questioning why it works like this, just copying the old script with minor code simplifications
            var priceSum = 0d;
            var todayVolume = 0;
            var yesterdayVolume = 0;

            foreach (var dataPoint in dataPoints)
            {
                if (dataPoint.Time > mostRecent.Time)
                {
                    mostRecent = dataPoint;
                }
                if (dataPoint.Time >= oneDayAgo)
                {
                    todayVolume += dataPoint.Volume;
                }
                else
                {
                    yesterdayVolume += dataPoint.Volume;
                }

                priceSum += dataPoint.Volume * dataPoint.WaPrice;
            }

            var movingAvgInfluence = 0;

            if (mostRecent.MovingAveragePrice.HasValue)
            {
                movingAvgInfluence = 10;
                priceSum += movingAvgInfluence * mostRecent.MovingAveragePrice.Value;
            }

            var customVolume = todayVolume + yesterdayVolume + movingAvgInfluence;

            if (customVolume == 0)
            {
                WipeStats();
            }
            else
            {
                var customPrice = priceSum / customVolume;
                WriteStats(yesterdayVolume, todayVolume, customPrice);
            }
        }

        public static void ParseFromWfmItem(JsonObject obj, out string id, out string slug, out string name)
        {
            var parsedId = obj.GetStringProperty("id") ?? throw new Exception("Unexpected json format. Property \"id\" missing or wrong format on object: " + obj);
            id = parsedId;

            var parsedSlug = obj.GetStringProperty("slug") ?? throw new Exception("Unexpected json format. Property \"slug\" missing or wrong format on object: " + obj);
            slug = parsedSlug;

            var i18nObj = obj.GetObjectProperty("i18n");

            var enObj = i18nObj?.GetObjectProperty("en");

            var parsedName = (enObj?.GetStringProperty("name")) ?? throw new Exception("Unexpected json format. Property \"i18n\" -> \"en\" -> \"name\" missing or wrong format on object: " + obj);
            name = parsedName;
        }
    }
}

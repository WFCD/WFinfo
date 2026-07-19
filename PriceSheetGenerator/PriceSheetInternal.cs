using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PriceSheetGenerator
{
    class PriceSheetInternal
    {
        private const string FormatVersionSaveName = "formatVersion";
        private const string LatestSyncTimeSaveName = "latestSyncTime";
        private const string NextIndexSaveName = "nextIndex";
        private const string EntriesSaveName = "entries";

        private const string InternalFormatVersion = "1.0";
        private const string FileName = "price_sheet_internal.json";
        private readonly List<ItemEntry> m_Entries;
        private int m_NextIndex;

        private static readonly TimeSpan RepopulateInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan RepopulateIntervalIfEmpty = TimeSpan.FromMinutes(5);
        public PriceSheetInternal()
        {
            Sema = new SemaphoreSlim(1);
            m_Entries = [];
            m_NextIndex = 0;
            LatestSyncTime = DateTime.MinValue;
        }

        public SemaphoreSlim Sema { get; }

        public DateTime LatestSyncTime { get; private set; }
        public bool KeepBackup { get; set; }

        public void GetItemStates(out int totalCount, out int deletedCount, out int missingStatsCount)
        {
            totalCount = m_Entries.Count;
            deletedCount = m_Entries.Count(x => x.IsDeleted);
            missingStatsCount = m_Entries.Count(x => !x.StatsValid);
        }


       /// <summary>
       /// Attempts to get the next item
       /// </summary>
       /// <param name="entry"></param>
       /// <returns></returns>
        public bool TryStep([NotNullWhen(true)] out ItemEntry? entry)
        {
            if (m_Entries.Count == 0)
            {
                // result impossible, no need to check
                entry = null;
                return false;
            }

            var startIndex = m_NextIndex % m_Entries.Count;
            var startItem = startIndex < m_Entries.Count ? m_Entries[startIndex] : null;

            if (startItem is not null && !startItem.IsDeleted)
            {
                // index valid, next index should be after
                m_NextIndex = startIndex + 1;
                entry = startItem;
                return true;
            }

            // pick next index to check
            var currIndex = (startIndex + 1) % m_Entries.Count;

            while (currIndex != startIndex && currIndex < m_Entries.Count)
            {
                var currItem = currIndex < m_Entries.Count ? m_Entries[currIndex] : null;

                if (currItem is not null && !currItem.IsDeleted)
                {
                    // valid index found, next index should be after
                    m_NextIndex = currIndex + 1;
                    entry = currItem;
                    return true;
                }

                // pick next index to check
                currIndex = (currIndex + 1) % m_Entries.Count;
            }

            // nothing valid found, next index to check should be either newly added or loop to start
            m_NextIndex = m_Entries.Count; // outside current list
            entry = null;
            return false;
        }

        public void TrimDeleted()
        {
            for (int i = m_Entries.Count - 1; i >= 0; i--)
            {
                var currItem = m_Entries[i];

                if (currItem.IsDeleted)
                {
                    if (i < m_NextIndex)
                    {
                        m_NextIndex--;
                    }
                    m_Entries.RemoveAt(i);
                }
            }
        }

        public bool ShouldRepopulate()
        {
            var now = DateTime.UtcNow;

            if (m_Entries.Count == 0)
            {
                return (now - LatestSyncTime) > RepopulateIntervalIfEmpty;
            }

            return (now - LatestSyncTime) > RepopulateInterval;
        }

        public void PopulateFromWfm(JsonObject responseData)
        {
            var dataArr = responseData.GetArrayProperty("data") ?? throw new Exception("Unexpected json format. Property \"data\" missing or wrong format");

            // Temporarily mark all all entries as deleted, so only the ones in data are kept
            foreach (var item in m_Entries)
            {
                item.MarkAsDeleted(true);
            }

            foreach( var item in dataArr )
            {
                if (item is null || 
                    item.GetValueKind() is not JsonValueKind.Object)
                {
                    throw new Exception("Unexpected json format. An item in data is null or wrong format. Item: " + item?.ToString());
                }

                var itemObj = item.AsObject();

                ItemEntry.ParseFromWfmItem(itemObj, out var id, out var slug, out var name);

                if (!name.Contains(" Prime "))
                {
                    continue;
                }

                var existingItem = m_Entries.FirstOrDefault(x => x.Id == id);

                if (existingItem is not null)
                {
                    existingItem.MarkAsDeleted(false); // item found, so not deleted
                    existingItem.SetNameAndSlug(name, slug); // keep name up to date
                }
                else
                {
                    var newItem = new ItemEntry(id, slug, name);
                    m_Entries.Add(newItem);
                }
            }

            LatestSyncTime = DateTime.UtcNow;
            TrimDeleted();
        }

        public static bool TryLoad(string directoryPath, [NotNullWhen(true)]out PriceSheetInternal? sheet)
        {
            sheet = null;

            var filePath = Path.Combine(directoryPath, FileName);

            if (!File.Exists(filePath))
            {
                return false;
            }

            using var fs = new FileStream(filePath, FileMode.Open);

            var json = JsonNode.Parse(fs);

            if (json is null || json.GetValueKind() is not JsonValueKind.Object)
            {
                return false;
            }

            var jsonObj = json.AsObject();

            var version = jsonObj.GetStringProperty(FormatVersionSaveName);

            if (version is null || !version.Equals(InternalFormatVersion))
            {
                return false;
            }

            var latestSyncTimeString = jsonObj.GetStringProperty(LatestSyncTimeSaveName);

            if (latestSyncTimeString is null || !DateTime.TryParseExact(latestSyncTimeString, 
                "O", 
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var latestSyncTime))
            {
                return false;
            }

            var nextIndex = jsonObj.GetIntProperty(NextIndexSaveName);

            var entriesArr = jsonObj.GetArrayProperty(EntriesSaveName);

            if (nextIndex is null || entriesArr is null)
            {
                return false;
            }

            var entries = new List<ItemEntry>();

            foreach (var entry in entriesArr)
            {
                if (entry is null || entry.GetValueKind() is not JsonValueKind.Object)
                {
                    return false;
                }

                var entryObj = entry.AsObject();

                if (!ItemEntry.TryLoad(entryObj, out var parsedEntry))
                {
                    return false;
                }

                entries.Add(parsedEntry);
            }

            sheet = new PriceSheetInternal();
            sheet.m_Entries.AddRange(entries);
            sheet.LatestSyncTime = latestSyncTime;
            sheet.m_NextIndex = nextIndex.Value;

            return true;
        }

        public void Save(string directoryPath, string outputPath)
        {
            var internalFilePath = Path.Combine(directoryPath, FileName);
            SaveInternal(internalFilePath);

            ExportSave(outputPath);
        }

        private void ExportSave(string filePath)
        {
            var obj = new JsonArray();

            foreach (var entry in m_Entries)
            {
                var entryObj = entry.ExportSave();

                if (entryObj is not null)
                {
                    obj.Add(entryObj);
                }
            }

            obj.WriteToFile(filePath, KeepBackup);
        }

        private void SaveInternal(string filePath)
        {
            var entries = new JsonArray();

            foreach (var entry in m_Entries)
            {
                var entryObj = entry.SaveInternal();
                entries.Add(entryObj);
            }

            var obj = new JsonObject
            {
                [FormatVersionSaveName] = InternalFormatVersion,
                [LatestSyncTimeSaveName] = LatestSyncTime.ToString("O", CultureInfo.InvariantCulture), // TODO better
                [NextIndexSaveName] = m_NextIndex.ToString(CultureInfo.InvariantCulture),
                [EntriesSaveName] = entries
            };

            obj.WriteToFile(filePath, KeepBackup);
        }
        
    }
}

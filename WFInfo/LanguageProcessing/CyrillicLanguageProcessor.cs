using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Russian language processor for OCR text processing
    /// Handles Russian Cyrillic characters with Latin transliteration
    /// </summary>
    public class RussianLanguageProcessor : LanguageProcessor
    {
        public RussianLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "ru";

        public override string[] BlueprintRemovals => new[] { "чертёж", "чертеж", "(чертёж)", "(чертеж)" };

        public override Dictionary<string, string> IgnoredItemNames => new Dictionary<string, string>
        {
            ["Forma Blueprint"] = "Чертёж: Форма",
            ["Exilus Weapon Adapter Blueprint"] = "Чертёж: Эксилус адаптер оружия",
            ["Kuva"] = "Кува",
            ["Riven Sliver"] = "Осколок Ривена",
            ["Ayatan Amber Star"] = "Янтарная звезда Аятана",
            ["Galariak Prime Blueprint"] = "Чертёж: Галариак Прайм",
            ["Galariak Prime Blade"] = "Чертёж: Галариак Прайм клинок",
            ["Galariak Prime Handle"] = "Чертёж: Галариак Прайм рукоять",
            ["Sagek Prime Blueprint"] = "Чертёж: Сагек Прайм",
            ["Sagek Prime Barrel"] = "Чертёж: Сагек Прайм ствол",
            ["Sagek Prime Receiver"] = "Чертёж: Сагек Прайм приёмник"
        };

        public override string CharacterWhitelist => GenerateCharacterRange(0x0400, 0x04FF) + GenerateCharacterRange(0x0500, 0x052F) + ": "; // Cyrillic + Cyrillic Supplement

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            // For Russian, don't normalize Cyrillic to Latin - we want to match Russian to Russian
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, null);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Russian
            string normalized = input.ToLower(_culture).Trim();

            // Handle Russian blueprint format: "Чертёж: <item_name>" -> "<item_name> (чертеж)"
            if (normalized.StartsWith("чертёж:") || normalized.StartsWith("чертеж:"))
            {
                // Extract item name after "чертёж:" / "чертеж:" with optional whitespace
                string itemName = Regex.Replace(normalized, @"^черт[её]ж:\s*", "");
                normalized = itemName + " (чертеж)";
            }

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Russian requires minimum of 6 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 6;
        }

        public override bool ShouldFilterWord(string word)
        {
            // Russian filters very short words (less than 2 characters)
            return !string.IsNullOrEmpty(word) && word.Length < 2;
        }
    }

    /// <summary>
    /// Ukrainian language processor for OCR text processing
    /// Handles Ukrainian Cyrillic characters with Latin transliteration
    /// </summary>
    public class UkrainianLanguageProcessor : LanguageProcessor
    {
        public UkrainianLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "uk";

        public override string[] BlueprintRemovals => new[] { "Кресленник" };

        public override Dictionary<string, string> IgnoredItemNames => new Dictionary<string, string>
        {
            ["Forma Blueprint"] = "Кресленник: Форма",
            ["Exilus Weapon Adapter Blueprint"] = "Кресленник: Екзилус адаптер зброї",
            ["Kuva"] = "Кува",
            ["Riven Sliver"] = "Уламок Рівена",
            ["Ayatan Amber Star"] = "Янтарна зірка Аятана",
            ["Galariak Prime Blueprint"] = "Кресленник: Галарак Прайм",
            ["Galariak Prime Blade"] = "Кресленник: Галарак Прайм лезо",
            ["Galariak Prime Handle"] = "Кресленник: Галарак Прайм рукоять",
            ["Sagek Prime Blueprint"] = "Кресленник: Сагек Прайм",
            ["Sagek Prime Barrel"] = "Кресленник: Сагек Прайм ствол",
            ["Sagek Prime Receiver"] = "Кресленник: Сагек Прайм приймач"
        };

        public override string CharacterWhitelist => GenerateCharacterRange(0x0400, 0x04FF) + GenerateCharacterRange(0x0500, 0x052F) + ": -()"; // Cyrillic + Cyrillic Supplement

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            // For Ukrainian, don't normalize Cyrillic to Latin - we want to match Ukrainian to Ukrainian
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, null);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Ukrainian
            string normalized = input.ToLower(_culture).Trim();

            // Remove accents (not typically needed for Ukrainian)
            //normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Ukrainian requires minimum of 6 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 6;
        }

        public override bool ShouldFilterWord(string word)
        {
            // Ukrainian filters very short words (less than 2 characters)
            return !string.IsNullOrEmpty(word) && word.Length < 2;
        }
    }
}

using System;
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

        public override string[] BlueprintRemovals => new string[0]; // No blueprint removals - handled in NormalizeForPatternMatching

        public override string CharacterWhitelist => GenerateCharacterRange(0x0400, 0x04FF) + GenerateCharacterRange(0x0500, 0x052F) + "0123456789:"; // Cyrillic + Cyrillic Supplement

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

        
        /// <summary>
        /// Generates a string containing all characters in the specified Unicode range
        /// </summary>
        /// <param name="start">Starting Unicode code point</param>
        /// <param name="end">Ending Unicode code point</param>
        /// <returns>String containing all characters in the range</returns>
        private static string GenerateCharacterRange(int start, int end)
        {
            var chars = new char[end - start + 1];
            for (int i = 0; i <= end - start; i++)
            {
                chars[i] = (char)(start + i);
            }
            return new string(chars);
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

        public override string CharacterWhitelist => GenerateCharacterRange(0x0400, 0x04FF) + GenerateCharacterRange(0x0500, 0x052F) + GenerateCharacterRange(0x0490, 0x0491) + GenerateCharacterRange(0x0406, 0x0407) + GenerateCharacterRange(0x0456, 0x0457) + GenerateCharacterRange(0x0492, 0x0493) + "0123456789:-()"; // Cyrillic + Ukrainian specific

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

            // In Ukrainian on WFM the (blueprint) part is in lowercase
            normalized = normalized.Replace("(Кресленник)", "(кресленник)");

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

        
        /// <summary>
        /// Generates a string containing all characters in the specified Unicode range
        /// </summary>
        /// <param name="start">Starting Unicode code point</param>
        /// <param name="end">Ending Unicode code point</param>
        /// <returns>String containing all characters in the range</returns>
        private static string GenerateCharacterRange(int start, int end)
        {
            var chars = new char[end - start + 1];
            for (int i = 0; i <= end - start; i++)
            {
                chars[i] = (char)(start + i);
            }
            return new string(chars);
        }
    }
}

using System;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Simplified Chinese language processor for OCR text processing
    /// Handles Simplified Chinese characters
    /// </summary>
    public class SimplifiedChineseLanguageProcessor : LanguageProcessor
    {
        public SimplifiedChineseLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "zh-hans";

        public override string[] BlueprintRemovals => new[] { "蓝图", "设计图" };

        public override string CharacterWhitelist => GenerateCharacterRange(0x4E00, 0x9FFF) + GenerateCharacterRange(0x3400, 0x4DBF) + GenerateCharacterRange(0xF900, 0xFAFF) + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz "; // Full CJK ideographs

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeChineseCharacters);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Simplified Chinese
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Chinese)
            normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Chinese requires minimum of 4 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 4;
        }

        public override bool ShouldFilterWord(string word)
        {
            // Chinese filtering: don't filter short Chinese words as single characters can be meaningful
            // Only filter out actual garbage (null/empty)
            return string.IsNullOrEmpty(word);
        }

        
        /// <summary>
        /// Normalizes Chinese characters for comparison
        /// </summary>
        private static string NormalizeChineseCharacters(string input)
        {
            return NormalizeFullWidthCharacters(input).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Traditional Chinese language processor for OCR text processing
    /// Handles Traditional Chinese characters
    /// </summary>
    public class TraditionalChineseLanguageProcessor : LanguageProcessor
    {
        public TraditionalChineseLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "zh-hant";

        public override string[] BlueprintRemovals => new[] { "藍圖", "設計圖" };

        public override string CharacterWhitelist => GenerateCharacterRange(0x4E00, 0x9FFF) + GenerateCharacterRange(0x3400, 0x4DBF) + GenerateCharacterRange(0xF900, 0xFAFF) + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz "; // Full CJK ideographs

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeChineseCharacters);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Traditional Chinese
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Chinese)
            normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Chinese requires minimum of 4 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 4;
        }

        public override bool ShouldFilterWord(string word)
        {
            // Chinese filtering: don't filter short Chinese words as single characters can be meaningful
            // Only filter out actual garbage (null/empty)
            return string.IsNullOrEmpty(word);
        }

        
        /// <summary>
        /// Normalizes Chinese characters for comparison
        /// </summary>
        private static string NormalizeChineseCharacters(string input)
        {
            return NormalizeFullWidthCharacters(input).ToLowerInvariant();
        }
    }
}

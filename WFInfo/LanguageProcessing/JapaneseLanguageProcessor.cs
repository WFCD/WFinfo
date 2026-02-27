using System;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Japanese language processor for OCR text processing
    /// Handles Japanese Hiragana, Katakana, and Kanji characters
    /// </summary>
    public class JapaneseLanguageProcessor : LanguageProcessor
    {
        public JapaneseLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "ja";

        public override string[] BlueprintRemovals => new[] { "設計図", "青図" };

        public override string CharacterWhitelist => GenerateCharacterRange(0x3040, 0x309F) + GenerateCharacterRange(0x30A0, 0x30FF) + GenerateCharacterRange(0x4E00, 0x9FAF) + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz "; // Japanese Hiragana, Katakana, Kanji

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeJapaneseCharacters);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Apply Japanese-specific normalization first
            string normalized = NormalizeJapaneseCharacters(input);

            // Basic cleanup for Japanese
            normalized = normalized.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Japanese - preserve combining marks)
            // normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Japanese requires minimum of 4 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 4;
        }

        
        /// <summary>
        /// Normalizes Japanese characters for comparison
        /// </summary>
        private static string NormalizeJapaneseCharacters(string input)
        {
            string result = NormalizeFullWidthCharacters(input);
            
            // Normalize katakana/hiragana variations (basic approach)
            result = result.Replace('ヶ', 'ケ').Replace('ヵ', 'カ');
            
            return result.ToLowerInvariant();
        }
    }
}

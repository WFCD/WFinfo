using System;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Turkish language processor for OCR text processing
    /// Handles Turkish characters with special diacritics
    /// </summary>
    public class TurkishLanguageProcessor : LanguageProcessor
    {
        public TurkishLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "tr";

        public override string[] BlueprintRemovals => new[] { "Plan", "Şema" };

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + "ÇçĞğİıÖöŞşÜü"; // Turkish-specific characters

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeTurkishCharacters, callBaseDefault: true);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Turkish
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Turkish as it has specific diacritics)
            normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Turkish requires minimum of 6 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 6;
        }

        
        /// <summary>
        /// Normalizes Turkish characters to standard equivalents for comparison
        /// </summary>
        private static readonly System.Globalization.CultureInfo _turkishCulture = new System.Globalization.CultureInfo("tr-TR");

        private static string NormalizeTurkishCharacters(string input)
        {
            // Handle Turkish dotted/dotless I explicitly before any casing to avoid Unicode edge cases
            string result = input
                .Replace('İ', 'i') // U+0130 LATIN CAPITAL LETTER I WITH DOT ABOVE → i
                .Replace('I', 'ı'); // U+0049 LATIN CAPITAL LETTER I → ı (Turkish dotless)
            // Lowercase with Turkish culture so ş/ğ/ç/ö/ü fold correctly
            result = result.ToLower(_turkishCulture);
            // ASCII-fold Turkish diacritics
            result = result
                .Replace('ğ', 'g')
                .Replace('ş', 's')
                .Replace('ç', 'c')
                .Replace('ö', 'o')
                .Replace('ü', 'u')
                .Replace('ı', 'i'); // dotless i → i
            return result;
        }
    }
}

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

        public override string CharacterWhitelist => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" + GenerateCharacterRange(0x00C7, 0x00C7) + GenerateCharacterRange(0x011F, 0x011F) + GenerateCharacterRange(0x0130, 0x0130) + GenerateCharacterRange(0x0150, 0x0150) + GenerateCharacterRange(0x0170, 0x0170) + GenerateCharacterRange(0x0131, 0x0131); // Turkish with ranges

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

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeTurkishCharacters);
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
        private static string NormalizeTurkishCharacters(string input)
        {
            // Convert Turkish characters to standard equivalents for comparison
            return input.ToLowerInvariant()
                .Replace('ğ', 'g')
                .Replace('Ğ', 'G')
                .Replace('ş', 's')
                .Replace('Ş', 'S')
                .Replace('ç', 'c')
                .Replace('Ç', 'C')
                .Replace('ö', 'o')
                .Replace('Ö', 'O')
                .Replace('ü', 'u')
                .Replace('Ü', 'U')
                .Replace('ı', 'i')
                .Replace('İ', 'I');
        }
    }
}

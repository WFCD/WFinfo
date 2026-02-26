using System;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Polish language processor for OCR text processing
    /// Handles Polish characters with specific diacritics
    /// </summary>
    public class PolishLanguageProcessor : LanguageProcessor
    {
        public PolishLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "pl";

        public override string[] BlueprintRemovals => new[] { "Plan", "Schemat" };

        public override string CharacterWhitelist => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" + GenerateCharacterRange(0x0104, 0x0107) + GenerateCharacterRange(0x0118, 0x0119) + GenerateCharacterRange(0x0141, 0x0144) + GenerateCharacterRange(0x015A, 0x015A); // Polish with ranges

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
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizePolishCharacters);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Polish
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Polish as it has specific diacritics)
            normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Polish requires minimum of 8 characters
            return !string.IsNullOrEmpty(partName) && partName.Length >= 8;
        }

        
        /// <summary>
        /// Normalizes Polish characters to standard equivalents for comparison
        /// </summary>
        private static string NormalizePolishCharacters(string input)
        {
            // Convert Polish characters to standard equivalents for comparison
            return input.ToLowerInvariant()
                .Replace('ą', 'a')
                .Replace('Ą', 'A')
                .Replace('ę', 'e')
                .Replace('Ę', 'E')
                .Replace('ć', 'c')
                .Replace('Ć', 'C')
                .Replace('ł', 'l')
                .Replace('Ł', 'L')
                .Replace('ś', 's')
                .Replace('Ś', 'S')
                .Replace('ź', 'z')
                .Replace('Ź', 'Z')
                .Replace('ż', 'z')
                .Replace('Ż', 'Z')
                .Replace('ó', 'o')
                .Replace('Ó', 'O');
        }
    }
}

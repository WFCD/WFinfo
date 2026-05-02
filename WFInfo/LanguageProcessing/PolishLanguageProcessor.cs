using System;
using System.Collections.Generic;
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

        private static readonly IReadOnlyDictionary<string, string> _ignoredItemNames = new Dictionary<string, string>
        {
            ["Forma Blueprint"] = "Forma - Plan",
            ["Exilus Weapon Adapter Blueprint"] = "Adapter Exilus dla Broni - Plan",
            ["Kuva"] = "Kuva",
            ["Riven Sliver"] = "Odłamek Rivena",
            ["Ayatan Amber Star"] = "Bursztynowa Gwiazda Ayatan",
            ["Galariak Prime Blueprint"] = "Galariak Prime - Plan",
            ["Galariak Prime Blade"] = "Galariak Prime: Ostrze - Plan",
            ["Galariak Prime Handle"] = "Galariak Prime: Rękojeść - Plan",
            ["Sagek Prime Blueprint"] = "Sagek Prime - Plan",
            ["Sagek Prime Barrel"] = "Sagek Prime: Lufa - Plan",
            ["Sagek Prime Receiver"] = "Sagek Prime: Korpus - Plan"
        };

        public override IReadOnlyDictionary<string, string> IgnoredItemNames => _ignoredItemNames;

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + GenerateCharacterRange(0x0104, 0x0107) + GenerateCharacterRange(0x0118, 0x0119) + GenerateCharacterRange(0x0141, 0x0144) + GenerateCharacterRange(0x015A, 0x015A) + "\u00d3\u00f3\u015a\u015b\u0179\u017a\u017b\u017c"; // Polish with ranges + missing letters

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizePolishCharacters, callBaseDefault: true);
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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// English language processor for OCR text processing
    /// Handles standard English text with basic normalization
    /// </summary>
    public class EnglishLanguageProcessor : LanguageProcessor
    {
        public EnglishLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "en";

        public override string[] BlueprintRemovals => new[] { "Blueprint" };

        public override Dictionary<string, string> IgnoredItemNames => new Dictionary<string, string>
        {
            ["Forma Blueprint"] = "Forma Blueprint",
            ["Exilus Weapon Adapter Blueprint"] = "Exilus Weapon Adapter Blueprint",
            ["Kuva"] = "Kuva",
            ["Riven Sliver"] = "Riven Sliver",
            ["Ayatan Amber Star"] = "Ayatan Amber Star",
            ["Galariak Prime Blueprint"] = "Galariak Prime Blueprint",
            ["Galariak Prime Blade"] = "Galariak Prime Blade",
            ["Galariak Prime Handle"] = "Galariak Prime Handle",
            ["Sagek Prime Blueprint"] = "Sagek Prime Blueprint",
            ["Sagek Prime Barrel"] = "Sagek Prime Barrel",
            ["Sagek Prime Receiver"] = "Sagek Prime Receiver"
        };

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return DefaultLevenshteinDistance(s, t);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for English
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // English requires minimum length of 13 characters
            return !string.IsNullOrEmpty(partName) && partName.Length >= 13;
        }

        public override bool ShouldFilterWord(string word)
        {
            // English filters very short words (less than 2 characters)
            return !string.IsNullOrEmpty(word) && word.Length < 2;
        }
    }
}

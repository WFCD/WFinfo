using System;
using System.Text;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Thai language processor for OCR text processing
    /// Handles Thai characters with tone mark normalization
    /// </summary>
    public class ThaiLanguageProcessor : LanguageProcessor
    {
        public ThaiLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "th";

        public override string[] BlueprintRemovals => new[] { "แบบแปลน", "ภาพวาด" };

        public override string CharacterWhitelist => GenerateCharacterRange(0x0E00, 0x0E7F) + " "; // Thai characters

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeThaiCharacters);
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Thai
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Thai)
            normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Thai requires minimum of 4 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 4;
        }

        
        /// <summary>
        /// Normalizes Thai characters for comparison
        /// </summary>
        private static string NormalizeThaiCharacters(string input)
        {
            string result = NormalizeFullWidthCharacters(input);
            
            // Basic Thai tone mark normalization (simplified approach)
            result = result.Normalize(System.Text.NormalizationForm.FormC);
            
            return result.ToLowerInvariant();
        }
    }
}

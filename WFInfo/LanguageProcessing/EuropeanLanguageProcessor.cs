using System;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Base class for European language processors with common diacritic handling
    /// </summary>
    public abstract class EuropeanLanguageProcessorBase : LanguageProcessor
    {
        protected EuropeanLanguageProcessorBase(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for European languages
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Don't remove accents for European languages since database has accented characters
            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // European languages require minimum of 8 characters
            return !string.IsNullOrEmpty(partName) && partName.Length >= 8;
        }

        public override bool ShouldFilterWord(string word)
        {
            // European languages filter very short words (less than 2 characters)
            return !string.IsNullOrEmpty(word) && word.Length < 2;
        }

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return DefaultLevenshteinDistance(s, t);
        }
        
        protected override int DefaultLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, input => NormalizeEuropeanCharacters(input), callBaseDefault: true);
        }

        /// <summary>
        /// Normalizes European characters for comparison
        /// </summary>
        protected static string NormalizeEuropeanCharacters(string input)
        {
            // Convert common European diacritics to standard equivalents for comparison
            return input.ToLowerInvariant()
                .Replace('à', 'a').Replace('á', 'a').Replace('â', 'a').Replace('ã', 'a').Replace('ä', 'a').Replace('å', 'a')
                .Replace('è', 'e').Replace('é', 'e').Replace('ê', 'e').Replace('ë', 'e')
                .Replace('ì', 'i').Replace('í', 'i').Replace('î', 'i').Replace('ï', 'i')
                .Replace('ò', 'o').Replace('ó', 'o').Replace('ô', 'o').Replace('õ', 'o').Replace('ö', 'o')
                .Replace('ù', 'u').Replace('ú', 'u').Replace('û', 'u').Replace('ü', 'u')
                .Replace('ñ', 'n')
                .Replace('ç', 'c')
                .Replace('ÿ', 'y')
                .Replace('À', 'A').Replace('Á', 'A').Replace('Â', 'A').Replace('Ã', 'A').Replace('Ä', 'A').Replace('Å', 'A')
                .Replace('È', 'E').Replace('É', 'E').Replace('Ê', 'E').Replace('Ë', 'E')
                .Replace('Ì', 'I').Replace('Í', 'I').Replace('Î', 'I').Replace('Ï', 'I')
                .Replace('Ò', 'O').Replace('Ó', 'O').Replace('Ô', 'O').Replace('Õ', 'O').Replace('Ö', 'O')
                .Replace('Ù', 'U').Replace('Ú', 'U').Replace('Û', 'U').Replace('Ü', 'U')
                .Replace('Ñ', 'N')
                .Replace('Ç', 'C')
                .Replace('Ÿ', 'Y');
        }
    }

    /// <summary>
    /// German language processor for OCR text processing
    /// Handles German characters with umlauts
    /// </summary>
    public class GermanLanguageProcessor : EuropeanLanguageProcessorBase
    {
        public GermanLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "de";

        public override string[] BlueprintRemovals => new[] { "Blaupause", "Plan" };

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + GenerateCharacterRange(0x00C4, 0x00C4) + GenerateCharacterRange(0x00D6, 0x00D6) + GenerateCharacterRange(0x00DC, 0x00DC) + GenerateCharacterRange(0x00DF, 0x00DF) + GenerateCharacterRange(0x00E4, 0x00E4) + GenerateCharacterRange(0x00F6, 0x00F6) + GenerateCharacterRange(0x00FC, 0x00FC); // German with umlauts
    }

    /// <summary>
    /// Spanish language processor for OCR text processing
    /// Handles Spanish characters with accents and special characters
    /// </summary>
    public class SpanishLanguageProcessor : EuropeanLanguageProcessorBase
    {
        public SpanishLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "es";

        public override string[] BlueprintRemovals => new[] { "Plano", "Diseño" };

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + GenerateCharacterRange(0x00C0, 0x00FF); // Spanish with accents
    }

    /// <summary>
    /// Portuguese language processor for OCR text processing
    /// Handles Portuguese characters with accents and special characters
    /// </summary>
    public class PortugueseLanguageProcessor : EuropeanLanguageProcessorBase
    {
        public PortugueseLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "pt";

        public override string[] BlueprintRemovals => new[] { "Planta", "Projeto" };

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + GenerateCharacterRange(0x00C0, 0x00FF); // Portuguese with accents
    }

    /// <summary>
    /// French language processor for OCR text processing
    /// Handles French characters with accents and special localization logic
    /// </summary>
    public class FrenchLanguageProcessor : EuropeanLanguageProcessorBase
    {
        public FrenchLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "fr";

        public override string[] BlueprintRemovals => new[] { "Schéma", "Plan" };

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + GenerateCharacterRange(0x00C0, 0x00FF); // French with Latin-1 supplement
    }

    /// <summary>
    /// Italian language processor for OCR text processing
    /// Handles Italian characters with accents
    /// </summary>
    public class ItalianLanguageProcessor : EuropeanLanguageProcessorBase
    {
        public ItalianLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "it";

        public override string[] BlueprintRemovals => new[] { "Progetto", "Piano" };

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-()" + GenerateCharacterRange(0x00C0, 0x00FF); // Italian with accents
    }
}

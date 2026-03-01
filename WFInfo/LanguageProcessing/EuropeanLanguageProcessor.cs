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

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + 
            GenerateCharacterRange(0x00C1, 0x00C1) + // Á
            GenerateCharacterRange(0x00C9, 0x00C9) + // É
            GenerateCharacterRange(0x00CD, 0x00CD) + // Í
            GenerateCharacterRange(0x00D1, 0x00D1) + // Ñ
            GenerateCharacterRange(0x00D3, 0x00D3) + // Ó
            GenerateCharacterRange(0x00DA, 0x00DA) + // Ú
            GenerateCharacterRange(0x00DC, 0x00DC) + // Ü
            GenerateCharacterRange(0x00E1, 0x00E1) + // á
            GenerateCharacterRange(0x00E9, 0x00E9) + // é
            GenerateCharacterRange(0x00ED, 0x00ED) + // í
            GenerateCharacterRange(0x00F1, 0x00F1) + // ñ
            GenerateCharacterRange(0x00F3, 0x00F3) + // ó
            GenerateCharacterRange(0x00FA, 0x00FA) + // ú
            GenerateCharacterRange(0x00FC, 0x00FC);   // ü
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

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + 
            GenerateCharacterRange(0x00C0, 0x00C0) + // À
            GenerateCharacterRange(0x00C1, 0x00C1) + // Á
            GenerateCharacterRange(0x00C2, 0x00C2) + // Â
            GenerateCharacterRange(0x00C3, 0x00C3) + // Ã
            GenerateCharacterRange(0x00C7, 0x00C7) + // Ç
            GenerateCharacterRange(0x00C9, 0x00C9) + // É
            GenerateCharacterRange(0x00CA, 0x00CA) + // Ê
            GenerateCharacterRange(0x00CD, 0x00CD) + // Í
            GenerateCharacterRange(0x00D3, 0x00D3) + // Ó
            GenerateCharacterRange(0x00D4, 0x00D4) + // Ô
            GenerateCharacterRange(0x00D5, 0x00D5) + // Õ
            GenerateCharacterRange(0x00DA, 0x00DA) + // Ú
            GenerateCharacterRange(0x00DC, 0x00DC) + // Ü
            GenerateCharacterRange(0x00E0, 0x00E0) + // à
            GenerateCharacterRange(0x00E1, 0x00E1) + // á
            GenerateCharacterRange(0x00E2, 0x00E2) + // â
            GenerateCharacterRange(0x00E3, 0x00E3) + // ã
            GenerateCharacterRange(0x00E7, 0x00E7) + // ç
            GenerateCharacterRange(0x00E9, 0x00E9) + // é
            GenerateCharacterRange(0x00EA, 0x00EA) + // ê
            GenerateCharacterRange(0x00ED, 0x00ED) + // í
            GenerateCharacterRange(0x00F3, 0x00F3) + // ó
            GenerateCharacterRange(0x00F4, 0x00F4) + // ô
            GenerateCharacterRange(0x00F5, 0x00F5) + // õ
            GenerateCharacterRange(0x00FA, 0x00FA) + // ú
            GenerateCharacterRange(0x00FC, 0x00FC);   // ü
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

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz " + 
            GenerateCharacterRange(0x00C0, 0x00C0) + // À
            GenerateCharacterRange(0x00C2, 0x00C2) + // Â
            GenerateCharacterRange(0x00C6, 0x00C6) + // Æ
            GenerateCharacterRange(0x00C7, 0x00C7) + // Ç
            GenerateCharacterRange(0x00C8, 0x00C8) + // È
            GenerateCharacterRange(0x00C9, 0x00C9) + // É
            GenerateCharacterRange(0x00CA, 0x00CA) + // Ê
            GenerateCharacterRange(0x00CB, 0x00CB) + // Ë
            GenerateCharacterRange(0x00CE, 0x00CE) + // Î
            GenerateCharacterRange(0x00CF, 0x00CF) + // Ï
            GenerateCharacterRange(0x00D4, 0x00D4) + // Ô
            GenerateCharacterRange(0x00D6, 0x00D6) + // Ö
            GenerateCharacterRange(0x00D9, 0x00D9) + // Ù
            GenerateCharacterRange(0x00DB, 0x00DB) + // Û
            GenerateCharacterRange(0x00DC, 0x00DC) + // Ü
            GenerateCharacterRange(0x00E0, 0x00E0) + // à
            GenerateCharacterRange(0x00E2, 0x00E2) + // â
            GenerateCharacterRange(0x00E6, 0x00E6) + // æ
            GenerateCharacterRange(0x00E7, 0x00E7) + // ç
            GenerateCharacterRange(0x00E8, 0x00E8) + // è
            GenerateCharacterRange(0x00E9, 0x00E9) + // é
            GenerateCharacterRange(0x00EA, 0x00EA) + // ê
            GenerateCharacterRange(0x00EB, 0x00EB) + // ë
            GenerateCharacterRange(0x00EE, 0x00EE) + // î
            GenerateCharacterRange(0x00EF, 0x00EF) + // ï
            GenerateCharacterRange(0x00F4, 0x00F4) + // ô
            GenerateCharacterRange(0x00F6, 0x00F6) + // ö
            GenerateCharacterRange(0x00F9, 0x00F9) + // ù
            GenerateCharacterRange(0x00FB, 0x00FB) + // û
            GenerateCharacterRange(0x00FC, 0x00FC);   // ü
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

        public override string CharacterWhitelist => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-()" + 
            GenerateCharacterRange(0x00C0, 0x00C0) + // À
            GenerateCharacterRange(0x00C8, 0x00C8) + // È
            GenerateCharacterRange(0x00C9, 0x00C9) + // É
            GenerateCharacterRange(0x00CC, 0x00CC) + // Ì
            GenerateCharacterRange(0x00CD, 0x00CD) + // Í
            GenerateCharacterRange(0x00D2, 0x00D2) + // Ò
            GenerateCharacterRange(0x00D3, 0x00D3) + // Ó
            GenerateCharacterRange(0x00D9, 0x00D9) + // Ù
            GenerateCharacterRange(0x00E0, 0x00E0) + // à
            GenerateCharacterRange(0x00E8, 0x00E8) + // è
            GenerateCharacterRange(0x00E9, 0x00E9) + // é
            GenerateCharacterRange(0x00EC, 0x00EC) + // ì
            GenerateCharacterRange(0x00ED, 0x00ED) + // í
            GenerateCharacterRange(0x00F2, 0x00F2) + // ò
            GenerateCharacterRange(0x00F3, 0x00F3) + // ó
            GenerateCharacterRange(0x00F9, 0x00F9);   // ù
    }
}

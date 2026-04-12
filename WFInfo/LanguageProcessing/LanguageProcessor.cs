using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Abstract base class for language-specific OCR text processing
    /// Defines the contract that all language processors must implement
    /// </summary>
    public abstract class LanguageProcessor
    {
        // Per-type normalized blueprint removals to avoid recomputing on every call
        private static readonly ConcurrentDictionary<Type, string[]> _normalizedBlueprintRemovalsCache = new ConcurrentDictionary<Type, string[]>();
        
        protected readonly IReadOnlyApplicationSettings _settings;
        protected readonly CultureInfo _culture;

        protected LanguageProcessor(IReadOnlyApplicationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _culture = GetCultureInfo(settings.Locale);
        }

        private string[] GetNormalizedBlueprintRemovals()
        {
            return _normalizedBlueprintRemovalsCache.GetOrAdd(GetType(), type =>
            {
                var blueprintRemovals = BlueprintRemovals ?? Array.Empty<string>();
                var normalized = new string[blueprintRemovals.Length];
                for (int i = 0; i < blueprintRemovals.Length; i++)
                {
                    normalized[i] = (blueprintRemovals[i] ?? string.Empty).ToLowerInvariant();
                }
                return normalized;
            });
        }

        /// <summary>
        /// Gets the appropriate CultureInfo for the locale
        /// </summary>
        /// <param name="locale">Locale code</param>
        /// <returns>CultureInfo instance</returns>
        private static CultureInfo GetCultureInfo(string locale)
        {
            try
            {
                return new CultureInfo(locale, false);
            }
            catch (Exception e)
            {
                // Log the failure and offending locale before falling back
                System.Diagnostics.Debug.WriteLine($"Failed to create CultureInfo for locale '{locale}': {e.Message}");
                // Fallback to invariant culture for unsupported locales
                return CultureInfo.InvariantCulture;
            }
        }

        /// <summary>
        /// Gets the CultureInfo for this language processor
        /// </summary>
        public CultureInfo Culture => _culture;

        /// <summary>
        /// Gets the locale code this processor handles (e.g., "en", "ko", "ja")
        /// </summary>
        public abstract string Locale { get; }

        /// <summary>
        /// Gets the blueprint removal terms for this language
        /// </summary>
        public abstract string[] BlueprintRemovals { get; }

        /// <summary>
        /// Gets the Tesseract character whitelist for this language
        /// </summary>
        public abstract string CharacterWhitelist { get; }

        /// <summary>
        /// Calculates Levenshtein distance between two strings using language-specific logic
        /// </summary>
        /// <param name="s">First string</param>
        /// <param name="t">Second string</param>
        /// <returns>Levenshtein distance</returns>
        public abstract int CalculateLevenshteinDistance(string s, string t);

        /// <summary>
        /// Normalizes characters for pattern matching in this language
        /// </summary>
        /// <param name="input">Input string to normalize</param>
        /// <returns>Normalized string</returns>
        public abstract string NormalizeForPatternMatching(string input);

        /// <summary>
        /// Validates if a part name meets minimum length requirements for this language
        /// </summary>
        /// <param name="partName">Part name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsPartNameValid(string partName);

        /// <summary>
        /// Validates if a single word fragment should be filtered out during OCR processing
        /// </summary>
        /// <param name="word">Word fragment to validate</param>
        /// <returns>True if word should be filtered out (removed), false if word should be kept</returns>
        public virtual bool ShouldFilterWord(string word)
        {
            // Default implementation: filter very short words (less than 2 characters)
            return !string.IsNullOrEmpty(word) && word.Length < 2;
        }

        /// <summary>
        /// Checks if a text fragment is a blueprint term for this language
        /// </summary>
        /// <param name="text">Text fragment to check</param>
        /// <returns>True if blueprint term, false otherwise</returns>
        public virtual bool IsBlueprintTerm(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            // Normalize text for case-insensitive comparison
            string normalizedText = text.ToLowerInvariant();
            
            // Check against pre-normalized blueprint removal terms
            // Handle common formats: standalone terms, in parentheses, etc.
            var normalizedBlueprintRemovals = GetNormalizedBlueprintRemovals();
            for (int i = 0; i < normalizedBlueprintRemovals.Length; i++)
            {
                string normalizedRemoval = normalizedBlueprintRemovals[i];
                if (normalizedText.Contains(normalizedRemoval) ||
                    normalizedText.StartsWith($"({normalizedRemoval}") ||
                    normalizedText.EndsWith($"{normalizedRemoval})"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets localized name data from market items using language-specific matching
        /// </summary>
        /// <param name="input">Input string to match</param>
        /// <param name="marketItems">Market items dictionary</param>
        /// <param name="useLevenshtein">Whether to use full Levenshtein distance</param>
        /// <returns>Best matching localized name</returns>
        public virtual string GetLocalizedNameData(string input, JObject marketItems, bool useLevenshtein)
        {
            if (string.IsNullOrEmpty(input) || marketItems == null)
                return input;

            string bestMatch = input;
            int bestDistance = int.MaxValue;

            foreach (KeyValuePair<string, JToken> item in marketItems)
            {
                if (item.Key == "version") continue;

                string[] split = item.Value.ToString().Split('|');
                if (split.Length < 3) continue;

                string localizedName = split[2];
                if (string.IsNullOrEmpty(localizedName)) continue;

                // Skip if length difference is too large
                int lengthDiff = Math.Abs(input.Length - localizedName.Length);
                if (lengthDiff > localizedName.Length / 2) continue;

                int distance;
                if (useLevenshtein)
                {
                    distance = CalculateLevenshteinDistance(input, localizedName);
                }
                else
                {
                    string normalizedInput = NormalizeForPatternMatching(input);
                    string normalizedStored = NormalizeForPatternMatching(localizedName);
                    distance = SimpleLevenshteinDistance(normalizedInput, normalizedStored);
                }

                // Only accept matches that are reasonably close (less than 50% difference)
                if (distance < bestDistance && distance < localizedName.Length * 0.5)
                {
                    bestDistance = distance;
                    bestMatch = split[0]; // Return the English name
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Gets localized name data from a lightweight snapshot of market items (avoids JObject DeepClone)
        /// </summary>
        public virtual string GetLocalizedNameData(string input, List<KeyValuePair<string, string>> marketItemsSnapshot, bool useLevenshtein)
        {
            if (string.IsNullOrEmpty(input) || marketItemsSnapshot == null)
                return input;

            string bestMatch = input;
            int bestDistance = int.MaxValue;

            foreach (var item in marketItemsSnapshot)
            {
                if (item.Key == "version") continue;

                string[] split = item.Value.Split('|');
                if (split.Length < 3) continue;

                string localizedName = split[2];
                if (string.IsNullOrEmpty(localizedName)) continue;

                int lengthDiff = Math.Abs(input.Length - localizedName.Length);
                if (lengthDiff > localizedName.Length / 2) continue;

                int distance;
                if (useLevenshtein)
                {
                    distance = CalculateLevenshteinDistance(input, localizedName);
                }
                else
                {
                    string normalizedInput = NormalizeForPatternMatching(input);
                    string normalizedStored = NormalizeForPatternMatching(localizedName);
                    distance = SimpleLevenshteinDistance(normalizedInput, normalizedStored);
                }

                if (distance < bestDistance && distance < localizedName.Length * 0.5)
                {
                    bestDistance = distance;
                    bestMatch = split[0];
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Default Levenshtein distance implementation for languages that don't need special handling
        /// </summary>
        protected virtual int DefaultLevenshteinDistance(string s, string t)
        {
            s = s.ToLower(_culture);
            t = t.ToLower(_culture);
            return ComputeLevenshteinCore(s, t);
        }

        /// <summary>
        /// Simple Levenshtein distance that avoids circular dependencies
        /// </summary>
        public int SimpleLevenshteinDistance(string s, string t)
        {
            return ComputeLevenshteinCore(s, t);
        }

        /// <summary>
        /// Helper method for Levenshtein distance with preprocessing
        /// </summary>
        protected int LevenshteinDistanceWithPreprocessing(string s, string t, string[] blueprintRemovals, Func<string, string> normalizer = null, bool callBaseDefault = false)
        {
            // Remove blueprint equivalents
            s = " " + s;
            t = " " + t;

            if (blueprintRemovals != null)
            {
                foreach (string removal in blueprintRemovals)
                {
                    if (!string.IsNullOrEmpty(removal))
                    {
                        s = System.Text.RegularExpressions.Regex.Replace(s, System.Text.RegularExpressions.Regex.Escape(removal), "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                        t = System.Text.RegularExpressions.Regex.Replace(t, System.Text.RegularExpressions.Regex.Escape(removal), "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    }
                }
            }

            s = s.Replace(" ", "");
            t = t.Replace(" ", "");

            // Apply character normalization if provided
            if (normalizer != null)
            {
                s = normalizer(s);
                t = normalizer(t);
            }

            return callBaseDefault ? ComputeLevenshteinCore(s, t) : DefaultLevenshteinDistance(s, t);
        }

        /// <summary>
        /// Core Levenshtein distance implementation (non-virtual)
        /// </summary>
        private static int ComputeLevenshteinCore(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Removes diacritic marks from text
        /// </summary>
        protected static string RemoveAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Converts full-width characters to half-width (for CJK languages)
        /// </summary>
        protected static string NormalizeFullWidthCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }
            
            var result = new System.Text.StringBuilder(input.Length);
            
            foreach (char c in input)
            {
                if (c == '\u3000') // Fullwidth space
                {
                    result.Append(' ');
                }
                else if (c >= '\uFF01' && c <= '\uFF5E') // Fullwidth ASCII range
                {
                    result.Append((char)(c - 0xFEE0));
                }
                else
                {
                    result.Append(c); // Leave other characters unchanged
                }
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Maximum safe size for character range generation to prevent memory issues
        /// </summary>
        private const int MaxGeneratedRangeSize = 10000;

        /// <summary>
        /// Generates a string containing all characters in the specified Unicode range
        /// </summary>
        /// <param name="start">Starting Unicode code point</param>
        /// <param name="end">Ending Unicode code point</param>
        /// <returns>String containing all characters in the range</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when range size exceeds safe limit</exception>
        protected static string GenerateCharacterRange(int start, int end)
        {
            int rangeSize = end - start + 1;
            if (rangeSize > MaxGeneratedRangeSize)
            {
                throw new ArgumentOutOfRangeException(nameof(end), 
                    $"Character range size ({rangeSize}) exceeds maximum safe limit ({MaxGeneratedRangeSize}). " +
                    $"Use GenerateCharacterRangeIterator for large ranges.");
            }

            var chars = new char[rangeSize];
            for (int i = 0; i < rangeSize; i++)
            {
                chars[i] = (char)(start + i);
            }
            return new string(chars);
        }

        /// <summary>
        /// Generates characters in the specified Unicode range using streaming (no large array allocation)
        /// </summary>
        /// <param name="start">Starting Unicode code point</param>
        /// <param name="end">Ending Unicode code point</param>
        /// <returns>Enumerable that yields characters in the range</returns>
        protected static IEnumerable<char> GenerateCharacterRangeIterator(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                yield return (char)i;
            }
        }
    }
}

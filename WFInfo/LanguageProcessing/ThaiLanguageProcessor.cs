using System;
using System.Linq;
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
            s = s ?? string.Empty;
            t = t ?? string.Empty;
            
            // Apply the same preprocessing used by the fallback path
            s = ApplyBlueprintRemovals(s, BlueprintRemovals);
            t = ApplyBlueprintRemovals(t, BlueprintRemovals);
            s = NormalizeThaiCharacters(s);
            t = NormalizeThaiCharacters(t);
            s = s.Trim();
            t = t.Trim();
            
            // Check if both inputs contain Thai characters for Thai-aware comparison
            bool sHasThai = ContainsThai(s);
            bool tHasThai = ContainsThai(t);
            
            if (sHasThai && tHasThai)
            {
                // Thai-aware path: use normalized Thai characters with Thai similarity logic
                return CalculateThaiAwareDistance(s, t);
            }
            else
            {
                // Fallback/transliterated path: use simple Levenshtein with already-normalized strings
                return SimpleLevenshteinDistance(s, t);
            }
        }

        /// <summary>
        /// Applies blueprint removal patterns to a string
        /// </summary>
        private static string ApplyBlueprintRemovals(string input, string[] removals)
        {
            if (string.IsNullOrEmpty(input) || removals == null)
                return input;
            
            string result = input;
            foreach (var removal in removals)
            {
                if (!string.IsNullOrEmpty(removal))
                {
                    // Case-insensitive replacement using IndexOf
                    int index = result.IndexOf(removal, StringComparison.OrdinalIgnoreCase);
                    while (index >= 0)
                    {
                        result = result.Substring(0, index) + result.Substring(index + removal.Length);
                        index = result.IndexOf(removal, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates Thai-aware Levenshtein distance with character similarity groups
        /// </summary>
        private int CalculateThaiAwareDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;

            if (n == 0) return m;
            if (m == 0) return n;

            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = GetThaiCharacterDifference(s[i - 1], t[j - 1]);
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Gets the character difference cost for Thai characters based on similarity groups
        /// </summary>
        private int GetThaiCharacterDifference(char a, char b)
        {
            if (a == b) return 0;

            // Similar looking Thai characters (common OCR confusions)
            var similarChars = new[]
            {
                new[] {'ก', 'ฮ'}, // ko/ho - similar round shapes
                new[] {'ด', 'ป'}, // do/po - similar loops
                new[] {'ต', 'ถ'}, // to/tho - similar shapes
                new[] {'บ', 'ป'}, // bo/po - similar loops
                new[] {'อ', 'โ'}, // o/o - different forms
                new[] {'ผ', 'ฝ'}, // pho/fo - similar shapes
                new[] {'ซ', 'ศ', 'ษ'}, // so variations
                new[] {'ง', 'ย'}, // ngo/yo - similar tails
                new[] {'ม', 'น'}, // mo/no - similar curves
                new[] {'ว', 'ใ'}, // wo/ai - similar shapes
            };

            foreach (var pair in similarChars)
            {
                if ((a == pair[0] && b == pair[1]) || (a == pair[1] && b == pair[0]))
                    return 1; // Low cost for similar looking characters
                if (pair.Length == 3 && 
                    ((a == pair[0] && b == pair[1]) || (a == pair[1] && b == pair[0]) ||
                     (a == pair[0] && b == pair[2]) || (a == pair[2] && b == pair[0]) ||
                     (a == pair[1] && b == pair[2]) || (a == pair[2] && b == pair[1])))
                    return 1;
            }

            // Tone mark confusions (lower cost for tone differences)
            var toneMarks = new[] {'่', '้', '๊', '๋', '่', '้', '๊', '๋'}; // Different tone marks
            bool aIsTone = toneMarks.Contains(a);
            bool bIsTone = toneMarks.Contains(b);
            if (aIsTone && bIsTone) return 1; // Low cost for tone mark differences

            // Default cost for different characters
            return 2;
        }

        public override bool ShouldFilterWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return true;
            
            bool hasThai = ContainsThai(word);
            bool hasLatin = false;
            foreach (char c in word)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    hasLatin = true;
                    break;
                }
            }
            
            // Keep all Thai text since Thai words are meaningful even when split by OCR
            if (hasThai && !hasLatin) return false;
            
            // For mixed Thai-Latin words, be more lenient
            if (hasThai && hasLatin) return false;
            
            // For non-Thai text, use standard filtering (filter very short words)
            return word.Length < 2;
        }

        /// <summary>
        /// Checks if a string contains Thai characters
        /// </summary>
        private static bool ContainsThai(string input)
        {
            foreach (char c in input)
            {
                // Thai Unicode range (0x0E00-0x0E7F)
                if (c >= 0x0E00 && c <= 0x0E7F) return true;
            }
            return false;
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Thai
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Thai - preserve tone/vowel marks)
            // normalized = RemoveAccents(normalized);

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
            
            // Basic Thai tone mark normalization
            result = result.Normalize(System.Text.NormalizationForm.FormC);
            
            // Common Thai OCR confusions and character variations — single-pass to avoid transitive remaps
            var sb = new System.Text.StringBuilder(result.Length);
            foreach (char c in result)
            {
                switch (c)
                {
                    case 'ซ': sb.Append('ศ'); break; // ซ → ศ
                    case 'ศ': sb.Append('ษ'); break; // ศ → ษ
                    case 'ผ': sb.Append('ฝ'); break; // ผ → ฝ
                    case 'บ': sb.Append('ป'); break; // บ → ป
                    case 'ด': sb.Append('ต'); break; // ด → ต
                    case 'อ': sb.Append('โ'); break; // อ → โ
                    default: sb.Append(c); break;
                }
            }
            result = sb.ToString();
            
            // Remove or normalize common diacritic issues
            result = result.Replace("์", ""); // Remove karan (silent marker) for comparison
            
            // Normalize similar vowel forms
            result = result.Replace('ใ', 'ไ'); // ai vowel variations
            result = result.Replace("ำ", "ํา"); // am vowel variations
            
            return result.ToLowerInvariant();
        }
    }
}

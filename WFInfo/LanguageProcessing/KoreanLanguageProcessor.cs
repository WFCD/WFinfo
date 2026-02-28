using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Korean language processor for OCR text processing
    /// Handles Korean Hangul characters with special normalization rules
    /// </summary>
    public class KoreanLanguageProcessor : LanguageProcessor
    {
                
        // Static spacing corrections to avoid recreating dictionary on every call
        private static readonly Dictionary<string, string> spacingCorrections = new Dictionary<string, string>
        {
            {"  ", " "}, {"   ", " "}, {"    ", " "}
        };
        
        // Korean character similarity groups for enhanced matching
        // Expanded to cover more OCR confusions and visual similarities
        private static readonly List<Dictionary<int, List<int>>> Korean = new List<Dictionary<int, List<int>>>() {
            // Initial consonants (초성)
            new Dictionary<int, List<int>>() {
                { 0, new List<int>{ 6, 7, 8, 16 } }, // ㄱ, ㄲ, ㄴ, ㄷ
                { 1, new List<int>{ 2, 3, 4, 16, 5, 9, 10, 17, 18 } }, // ㄷ, ㄸ, ㄹ, ㅁ, ㅂ, ㅃ, ㅅ, ㅆ, ㅇ, ㅈ, ㅉ, ㅊ, ㅋ, ㅌ, ㅍ, ㅎ
                { 2, new List<int>{ 12, 13, 14, 19, 20 } }, // ㅈ, ㅉ, ㅊ, ㅋ, ㅌ
                { 3, new List<int>{ 0, 1, 15, 11, 18, 21, 22 } }, // ㄱ, ㄲ, ㅋ, ㅇ, ㅎ, additional visual similarities
                { 4, new List<int>{ 1, 5, 6, 7 } }, // ㄹ, ㅁ, ㅂ, ㅃ (rounded shapes)
                { 5, new List<int>{ 4, 6, 7, 8 } }, // ㅁ, ㄹ, ㅂ, ㅃ (box-like shapes)
                { 6, new List<int>{ 0, 7, 8, 5 } }, // ㅂ, ㄱ, ㅃ, ㅁ
                { 7, new List<int>{ 6, 0, 8, 5 } }, // ㅃ, ㅂ, ㄱ, ㅁ
                { 8, new List<int>{ 0, 6, 7 } }, // ㅎ, ㄱ, ㅂ, ㅃ
                { 9, new List<int>{ 10, 11, 12 } }, // ㅅ, ㅆ, ㅈ (vertical strokes)
                { 10, new List<int>{ 9, 11, 12 } }, // ㅆ, ㅅ, ㅈ
                { 11, new List<int>{ 9, 10, 12, 13 } }, // ㅇ, ㅅ, ㅆ, ㅈ, ㅉ
                { 12, new List<int>{ 9, 10, 11, 13, 14 } }, // ㅈ, ㅅ, ㅆ, ㅇ, ㅉ, ㅊ
                { 13, new List<int>{ 12, 14 } }, // ㅉ, ㅈ, ㅊ
                { 14, new List<int>{ 12, 13, 15 } }, // ㅊ, ㅈ, ㅉ, ㅋ
                { 15, new List<int>{ 3, 14, 16 } }, // ㅋ, ㄱ, ㅎ, ㅊ
                { 16, new List<int>{ 3, 15 } }, // ㅌ, ㄱ, ㅋ
                { 17, new List<int>{ 18 } }, // ㅍ, ㅎ
                { 18, new List<int>{ 3, 17 } }  // ㅎ, ㄱ, ㅍ
            },
            // Vowels (중성)
            new Dictionary<int, List<int>>() {
                { 0, new List<int>{ 20, 5, 1, 7, 3, 19, 21, 22 } }, // ㅣ, ㅔ, ㅐ, ㅖ, ㅒ, ㅢ, additional vertical vowels
                { 1, new List<int>{ 16, 11, 15, 10, 23, 24 } }, // ㅟ, ㅚ, ㅞ, ㅙ, additional compound vowels
                { 2, new List<int>{ 4, 0, 6, 2, 14, 9, 25, 26 } }, // ㅓ, ㅏ, ㅕ, ㅑ, ㅝ, ㅘ, additional horizontal vowels
                { 3, new List<int>{ 18, 13, 8, 17, 12, 27, 28 } }, // ㅡ, ㅜ, ㅗ, ㅠ, ㅛ, additional horizontal vowels
                { 4, new List<int>{ 2, 6, 9, 14 } }, // ㅏ, ㅓ, ㅕ, ㅑ, ㅘ
                { 5, new List<int>{ 0, 1, 7, 19 } }, // ㅐ, ㅣ, ㅔ, ㅖ, ㅒ
                { 6, new List<int>{ 2, 4, 9, 14 } }, // ㅑ, ㅓ, ㅏ, ㅕ, ㅘ
                { 7, new List<int>{ 0, 5, 1, 19 } }, // ㅒ, ㅣ, ㅐ, ㅔ, ㅖ
                { 8, new List<int>{ 3, 13, 17, 18 } }, // ㅗ, ㅡ, ㅠ, ㅜ
                { 9, new List<int>{ 2, 4, 6, 14 } }, // ㅜ, ㅓ, ㅏ, ㅑ, ㅘ
                { 10, new List<int>{ 1, 15, 11, 16 } }, // ㅠ, ㅟ, ㅚ, ㅞ
                { 11, new List<int>{ 1, 10, 15, 16 } }, // ㅡ, ㅟ, ㅠ, ㅚ, ㅞ
                { 12, new List<int>{ 3, 18, 13, 17 } }, // ㅛ, ㅡ, ㅗ, ㅠ
                { 13, new List<int>{ 3, 8, 18, 17 } }, // ㅝ, ㅡ, ㅗ, ㅜ
                { 14, new List<int>{ 2, 4, 6, 9 } }, // ㅘ, ㅓ, ㅏ, ㅑ, ㅜ
                { 15, new List<int>{ 1, 10, 11, 16 } }, // ㅚ, ㅟ, ㅠ, ㅡ, ㅞ
                { 16, new List<int>{ 1, 10, 11, 15 } }, // ㅞ, ㅟ, ㅠ, ㅡ, ㅚ
                { 17, new List<int>{ 3, 8, 12, 13 } }, // ㅟ, ㅡ, ㅗ, ㅛ, ㅝ
                { 18, new List<int>{ 3, 8, 11, 13 } }, // ㅢ, ㅡ, ㅗ, ㅝ
                { 19, new List<int>{ 0, 5, 7, 1 } }, // ㅖ, ㅣ, ㅐ, ㅒ, ㅔ
                // Additional compound vowels and visual similarities
                { 20, new List<int>{ 0, 5 } }, // ㅔ variants
                { 21, new List<int>{ 0, 1 } }, // ㅐ variants
                { 22, new List<int>{ 2, 4 } }, // ㅕ variants
                { 23, new List<int>{ 3, 8 } }, // ㅛ variants
                { 24, new List<int>{ 9, 2 } }, // ㅜ variants
                { 25, new List<int>{ 14, 2 } }, // ㅘ variants
                { 26, new List<int>{ 13, 3 } }, // ㅝ variants
                { 27, new List<int>{ 12, 3 } }, // ㅛ variants
                { 28, new List<int>{ 17, 1 } }  // ㅟ variants
            },
            // Final consonants (종성)
            new Dictionary<int, List<int>>() {
                { 0, new List<int>{ 16, 17, 18, 26, 27, 28 } }, // ㄱ, ㄲ, ㄳ, ㄴ, ㄵ, ㄶ, ㄷ, ㄹ, ㄺ, ㄻ, ㄼ, ㄽ, ㄾ, ㄿ, ㅀ, ㅅ, ㅆ, ㅇ, ㅈ, ㅊ, ㅋ, ㅌ, ㅍ, ㅎ
                { 1, new List<int>{ 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 19, 20, 25, 29, 30 } }, // ㄴ cluster and similar endings
                { 2, new List<int>{ 22, 23, 31, 32 } }, // ㅈ, ㅊ, ㅋ, ㅌ cluster
                { 3, new List<int>{ 1, 2, 3, 24, 21, 27, 33 } }, // ㄱ cluster and similar
                { 4, new List<int>{ 0 } }, // No final consonant
                // Expanded final consonant similarities for OCR
                { 5, new List<int>{ 6, 7, 8, 9 } }, // ㄵ, ㄶ, ㄷ, ㄹ similarities
                { 6, new List<int>{ 5, 7, 8, 10 } }, // ㄶ, ㄵ, ㄷ, ㄹ similarities
                { 7, new List<int>{ 5, 6, 8, 11 } }, // ㄷ, ㄵ, ㄶ, ㄹ similarities
                { 8, new List<int>{ 5, 6, 7, 12 } }, // ㄹ, ㄵ, ㄶ, ㄷ similarities
                { 9, new List<int>{ 10, 11, 12, 13 } }, // ㄺ, ㄻ, ㄼ, ㄽ similarities
                { 10, new List<int>{ 9, 11, 12, 14 } }, // ㄻ, ㄺ, ㄼ, ㄽ similarities
                { 11, new List<int>{ 9, 10, 12, 15 } }, // ㄼ, ㄺ, ㄻ, ㄽ similarities
                { 12, new List<int>{ 9, 10, 11, 13 } }, // ㄽ, ㄺ, ㄻ, ㄼ similarities
                { 13, new List<int>{ 12, 14, 15 } }, // ㄾ, ㄽ, ㄼ, ㄾ similarities
                { 14, new List<int>{ 13, 15, 19 } }, // ㄿ, ㄾ, ㄼ, ㅀ similarities
                { 15, new List<int>{ 14, 19, 20 } }, // ㅀ, ㄿ, ㅅ, ㅆ similarities
                { 16, new List<int>{ 0, 17, 18 } }, // ㄲ, ㄱ, ㄳ similarities
                { 17, new List<int>{ 0, 16, 18 } }, // ㄳ, ㄱ, ㄲ similarities
                { 18, new List<int>{ 0, 16, 17 } }, // ㄵ, ㄱ, ㄲ, ㄳ similarities
                { 19, new List<int>{ 14, 15, 20 } }, // ㅅ, ㄿ, ㅀ, ㅆ similarities
                { 20, new List<int>{ 19, 15, 25 } }, // ㅆ, ㅅ, ㅀ, ㅌ similarities
                { 21, new List<int>{ 3, 24, 27 } }, // ㅈ, ㄱ, ㄹ, ㅋ similarities
                { 22, new List<int>{ 2, 23, 31 } }, // ㅊ, ㅈ, ㅋ similarities
                { 23, new List<int>{ 2, 22, 32 } }, // ㅋ, ㅈ, ㅊ, ㅌ similarities
                { 24, new List<int>{ 3, 21, 27 } }, // ㅌ, ㄱ, ㅈ, ㅋ similarities
                { 25, new List<int>{ 1, 20, 30 } }, // ㅍ, ㄴ, ㅆ, ㅎ similarities
                { 26, new List<int>{ 0, 27, 28 } }, // ㄱ, ㄹ, ㅎ similarities
                { 27, new List<int>{ 0, 26, 28, 33 } }, // ㄹ, ㄱ, ㅎ, ㅌ similarities
                { 28, new List<int>{ 0, 26, 27 } }, // ㅎ, ㄱ, ㄹ similarities
                { 29, new List<int>{ 1, 30 } }, // Additional ㄴ variations
                { 30, new List<int>{ 25, 29 } }, // Additional ㅍ variations
                { 31, new List<int>{ 22, 32 } }, // Additional ㅋ variations
                { 32, new List<int>{ 23, 31 } }, // Additional ㅌ variations
                { 33, new List<int>{ 3, 27 } }  // Additional ㄱ variations
            }
        };

        public KoreanLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "ko";

        public override string[] BlueprintRemovals => new[] { "설계도" };

        public override string CharacterWhitelist => GenerateCharacterRange(0xAC00, 0xD7AF) + " "; // Korean Hangul

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            // i18n korean edit distance algorithm
            // Normalize spacing but preserve word boundaries for better OCR fragment matching
            s = NormalizeKoreanTextForComparison(s ?? "");
            t = NormalizeKoreanTextForComparison(t ?? "");

            // Check if both inputs contain Hangul characters for Korean-aware comparison
            bool sHasHangul = ContainsHangul(s);
            bool tHasHangul = ContainsHangul(t);
            
            if (sHasHangul && tHasHangul)
            {
                // Korean-aware path: use original Hangul characters with Korean similarity logic
                return CalculateKoreanAwareDistance(s, t);
            }
            else
            {
                // Fallback/transliterated path: normalize to Latin equivalents
                s = NormalizeKoreanCharacters(s);
                t = NormalizeKoreanCharacters(t);
                return CalculateStandardDistance(s, t);
            }
        }
        
        /// <summary>
        /// Normalizes Korean text for comparison by only removing spaces
        /// Direct OCR to database matching with minimal tampering
        /// </summary>
        private static string NormalizeKoreanTextForComparison(string input)
        {
            if (string.IsNullOrEmpty(input)) return " ";
            
            // Only remove spaces - direct OCR to database matching
            string result = input.Replace(" ", "");
            
            // Add leading space to match original algorithm structure
            return " " + result;
        }
        
        /// <summary>
        /// Checks if a string contains any Hangul characters
        /// </summary>
        private static bool ContainsHangul(string input)
        {
            foreach (char c in input)
            {
                if (c >= 0xAC00 && c <= 0xD7AF) // Hangul syllables range
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Calculates distance using Korean-aware similarity logic
        /// </summary>
        private int CalculateKoreanAwareDistance(string s, string t)
        {
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
                    int cost = GetKoreanCharacterDifference(s[i - 1], t[j - 1]);
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
        
        /// <summary>
        /// Calculates standard distance without Korean-specific logic
        /// </summary>
        private int CalculateStandardDistance(string s, string t)
        {
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
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Direct OCR to database matching - only remove spaces
            return input.Replace(" ", "");
        }

        public override bool IsPartNameValid(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return false;
            
            // Korean requires minimum of 6 characters after removing spaces
            return partName.Replace(" ", "").Length >= 6;
        }

        public override bool ShouldFilterWord(string word)
        {
            // Korean filtering: use intelligent analysis instead of hardcoded fragments
            
            if (string.IsNullOrEmpty(word)) return true;
            
            // Filter out very short non-Korean garbage (single characters that aren't Hangul)
            if (word.Length == 1 && !IsHangulSyllable(word[0])) return true;
            
            // Keep all Korean text (Hangul characters) since Korean words are meaningful
            // even when split by OCR
            if (ContainsHangul(word)) return false;
            
            // For non-Korean text, use standard filtering (filter very short words)
            return word.Length < 2;
        }

        
        /// <summary>
        /// Gets the character difference cost for Korean characters based on similarity groups
        /// </summary>
        private int GetKoreanCharacterDifference(char a, char b)
        {
            if (a == b) return 0;

            // Handle Hangul decomposition for Korean-aware comparison
            if (IsHangulSyllable(a) && IsHangulSyllable(b))
            {
                // Decompose both characters into Jamo indices and compare
                var jamoA = DecomposeHangul(a);
                var jamoB = DecomposeHangul(b);
                
                // Compare each component (initial, medial, final) using similarity groups
                int totalCost = 0;
                
                // Compare initial consonants (초성)
                totalCost += CompareJamoSimilarity(jamoA.initialIndex, jamoB.initialIndex, 0);
                
                // Compare medial vowels (중성)  
                totalCost += CompareJamoSimilarity(jamoA.medialIndex, jamoB.medialIndex, 1);
                
                // Compare final consonants (종성)
                totalCost += CompareJamoSimilarity(jamoA.finalIndex, jamoB.finalIndex, 2);
                
                return totalCost > 0 ? Math.Min(totalCost, 2) : 0;
            }
            
            // Fallback to original logic for non-Hangul or mixed cases
            // Check if characters are in the same similarity group
            for (int group = 0; group < Korean.Count; group++)
            {
                foreach (var similarityGroup in Korean[group])
                {
                    if (similarityGroup.Value.Contains((int)a) && similarityGroup.Value.Contains((int)b))
                    {
                        return 1; // Similar characters have lower cost
                    }
                }
            }

            return 2; // Different characters have higher cost
        }
        
        /// <summary>
        /// Checks if a character is a Hangul syllable
        /// </summary>
        private static bool IsHangulSyllable(char c)
        {
            return c >= 0xAC00 && c <= 0xD7AF;
        }
        
        /// <summary>
        /// Decomposes a Hangul syllable into Jamo component indices
        /// </summary>
        private static (int initialIndex, int medialIndex, int finalIndex) DecomposeHangul(char syllable)
        {
            if (!IsHangulSyllable(syllable))
                return (-1, -1, -1);
                
            int syllableIndex = syllable - 0xAC00;
            
            int finalIndex = syllableIndex % 28; // 0-27 (including no final consonant)
            int medialIndex = (syllableIndex / 28) % 21; // 0-20
            int initialIndex = syllableIndex / (28 * 21); // 0-18
            
            return (initialIndex, medialIndex, finalIndex);
        }
        
        /// <summary>
        /// Compares two Jamo indices using Korean similarity groups
        /// </summary>
        private int CompareJamoSimilarity(int indexA, int indexB, int groupType)
        {
            if (indexA == indexB) return 0;
            if (indexA < 0 || indexB < 0) return 2; // Invalid indices
            
            // Use the Korean similarity groups for the specified type
            if (groupType < Korean.Count)
            {
                foreach (var similarityGroup in Korean[groupType])
                {
                    // Check both the value list and the key for declared pairs
                    if ((similarityGroup.Value.Contains(indexA) && similarityGroup.Value.Contains(indexB)) ||
                        (similarityGroup.Key == indexA && similarityGroup.Value.Contains(indexB)) ||
                        (similarityGroup.Key == indexB && similarityGroup.Value.Contains(indexA)))
                    {
                        return 1; // Similar Jamo have lower cost
                    }
                }
            }
            
            return 2; // Different Jamo have higher cost
        }

        /// <summary>
        /// Normalizes Korean Hangul characters to Latin equivalents for comparison
        /// Uses comprehensive mapping for common OCR confusions and variations
        /// </summary>
        private static string NormalizeKoreanCharacters(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // Common OCR character substitutions and confusions
            // Using List<KeyValuePair<string,string>> to allow duplicate keys and preserve order
            var replacements = new List<KeyValuePair<string, string>>
            {
                // Basic consonants and vowels
                new KeyValuePair<string, string>("가", "ga"), new KeyValuePair<string, string>("개", "gae"), new KeyValuePair<string, string>("갸", "gya"), new KeyValuePair<string, string>("걔", "gyae"), new KeyValuePair<string, string>("거", "geo"), new KeyValuePair<string, string>("게", "ge"), new KeyValuePair<string, string>("겨", "gyeo"), new KeyValuePair<string, string>("계", "gye"),
                new KeyValuePair<string, string>("고", "go"), new KeyValuePair<string, string>("과", "gwa"), new KeyValuePair<string, string>("궈", "gwo"), new KeyValuePair<string, string>("괘", "gwae"), new KeyValuePair<string, string>("괴", "goe"), new KeyValuePair<string, string>("교", "gyo"), new KeyValuePair<string, string>("구", "gu"), new KeyValuePair<string, string>("궈", "gwo"),
                new KeyValuePair<string, string>("궤", "gwe"), new KeyValuePair<string, string>("귀", "gwi"), new KeyValuePair<string, string>("규", "gyu"), new KeyValuePair<string, string>("그", "geu"), new KeyValuePair<string, string>("긔", "gui"), new KeyValuePair<string, string>("기", "gi"),
                
                new KeyValuePair<string, string>("나", "na"), new KeyValuePair<string, string>("내", "nae"), new KeyValuePair<string, string>("냐", "nya"), new KeyValuePair<string, string>("냬", "nyae"), new KeyValuePair<string, string>("너", "neo"), new KeyValuePair<string, string>("네", "ne"), new KeyValuePair<string, string>("녀", "nyeo"), new KeyValuePair<string, string>("녜", "nye"),
                new KeyValuePair<string, string>("노", "no"), new KeyValuePair<string, string>("놔", "nwa"), new KeyValuePair<string, string>("놰", "nwo"), new KeyValuePair<string, string>("놰", "nwae"), new KeyValuePair<string, string>("뇌", "noe"), new KeyValuePair<string, string>("뇨", "nyo"), new KeyValuePair<string, string>("누", "nu"), new KeyValuePair<string, string>("뉘", "nwi"),
                new KeyValuePair<string, string>("뉴", "nyu"), new KeyValuePair<string, string>("느", "neu"), new KeyValuePair<string, string>("늬", "nui"), new KeyValuePair<string, string>("니", "ni"),
                
                new KeyValuePair<string, string>("다", "da"), new KeyValuePair<string, string>("대", "dae"), new KeyValuePair<string, string>("댜", "dya"), new KeyValuePair<string, string>("댸", "dyae"), new KeyValuePair<string, string>("더", "deo"), new KeyValuePair<string, string>("데", "de"), new KeyValuePair<string, string>("뎌", "dyeo"), new KeyValuePair<string, string>("뎨", "dye"),
                new KeyValuePair<string, string>("도", "do"), new KeyValuePair<string, string>("돠", "dwa"), new KeyValuePair<string, string>("돼", "dwae"), new KeyValuePair<string, string>("돼", "doe"), new KeyValuePair<string, string>("됴", "dyo"), new KeyValuePair<string, string>("두", "du"), new KeyValuePair<string, string>("둬", "dwo"), new KeyValuePair<string, string>("뒈", "dwae"),
                new KeyValuePair<string, string>("뒤", "dwi"), new KeyValuePair<string, string>("듀", "dyu"), new KeyValuePair<string, string>("드", "deu"), new KeyValuePair<string, string>("듸", "dui"), new KeyValuePair<string, string>("디", "di"),
                
                new KeyValuePair<string, string>("라", "ra"), new KeyValuePair<string, string>("래", "rae"), new KeyValuePair<string, string>("랴", "rya"), new KeyValuePair<string, string>("럐", "ryae"), new KeyValuePair<string, string>("러", "reo"), new KeyValuePair<string, string>("레", "re"), new KeyValuePair<string, string>("려", "ryeo"), new KeyValuePair<string, string>("례", "rye"),
                new KeyValuePair<string, string>("로", "ro"), new KeyValuePair<string, string>("롸", "rwa"), new KeyValuePair<string, string>("뢔", "roe"), new KeyValuePair<string, string>("료", "ryo"), new KeyValuePair<string, string>("루", "ru"), new KeyValuePair<string, string>("뤄", "rwo"), new KeyValuePair<string, string>("뤠", "rwae"), new KeyValuePair<string, string>("뤼", "rwi"),
                new KeyValuePair<string, string>("류", "ryu"), new KeyValuePair<string, string>("르", "reu"), new KeyValuePair<string, string>("릐", "rui"), new KeyValuePair<string, string>("리", "ri"),
                
                new KeyValuePair<string, string>("마", "ma"), new KeyValuePair<string, string>("매", "mae"), new KeyValuePair<string, string>("먀", "mya"), new KeyValuePair<string, string>("먜", "myae"), new KeyValuePair<string, string>("머", "meo"), new KeyValuePair<string, string>("메", "me"), new KeyValuePair<string, string>("며", "myeo"), new KeyValuePair<string, string>("몌", "mye"),
                new KeyValuePair<string, string>("모", "mo"), new KeyValuePair<string, string>("뫄", "mwa"), new KeyValuePair<string, string>("뫠", "mwae"), new KeyValuePair<string, string>("뫼", "moe"), new KeyValuePair<string, string>("묘", "myo"), new KeyValuePair<string, string>("무", "mu"), new KeyValuePair<string, string>("뭐", "mwo"), new KeyValuePair<string, string>("뭬", "mwae"),
                new KeyValuePair<string, string>("뮈", "mwi"), new KeyValuePair<string, string>("뮤", "myu"), new KeyValuePair<string, string>("므", "meu"), new KeyValuePair<string, string>("믜", "mui"), new KeyValuePair<string, string>("미", "mi"),
                
                new KeyValuePair<string, string>("바", "ba"), new KeyValuePair<string, string>("배", "bae"), new KeyValuePair<string, string>("뱌", "bya"), new KeyValuePair<string, string>("뱨", "byae"), new KeyValuePair<string, string>("버", "beo"), new KeyValuePair<string, string>("베", "be"), new KeyValuePair<string, string>("벼", "byeo"), new KeyValuePair<string, string>("볘", "bye"),
                new KeyValuePair<string, string>("보", "bo"), new KeyValuePair<string, string>("봐", "bwa"), new KeyValuePair<string, string>("봬", "bwae"), new KeyValuePair<string, string>("뵈", "boe"), new KeyValuePair<string, string>("뵤", "byo"), new KeyValuePair<string, string>("부", "bu"), new KeyValuePair<string, string>("붜", "bwo"), new KeyValuePair<string, string>("붸", "bwae"),
                new KeyValuePair<string, string>("뷔", "bwi"), new KeyValuePair<string, string>("뷰", "byu"), new KeyValuePair<string, string>("브", "beu"), new KeyValuePair<string, string>("븨", "bui"), new KeyValuePair<string, string>("비", "bi"),
                
                new KeyValuePair<string, string>("사", "sa"), new KeyValuePair<string, string>("새", "sae"), new KeyValuePair<string, string>("샤", "sya"), new KeyValuePair<string, string>("섀", "syae"), new KeyValuePair<string, string>("서", "seo"), new KeyValuePair<string, string>("세", "se"), new KeyValuePair<string, string>("셔", "syeo"), new KeyValuePair<string, string>("셰", "sye"),
                new KeyValuePair<string, string>("소", "so"), new KeyValuePair<string, string>("솨", "swa"), new KeyValuePair<string, string>("쇄", "swae"), new KeyValuePair<string, string>("쇠", "soe"), new KeyValuePair<string, string>("쇼", "syo"), new KeyValuePair<string, string>("수", "su"), new KeyValuePair<string, string>("숴", "swo"), new KeyValuePair<string, string>("쉐", "swae"),
                new KeyValuePair<string, string>("쉬", "swi"), new KeyValuePair<string, string>("슈", "syu"), new KeyValuePair<string, string>("스", "seu"), new KeyValuePair<string, string>("싀", "sui"), new KeyValuePair<string, string>("시", "si"),
                
                new KeyValuePair<string, string>("아", "a"), new KeyValuePair<string, string>("애", "ae"), new KeyValuePair<string, string>("야", "ya"), new KeyValuePair<string, string>("얘", "yae"), new KeyValuePair<string, string>("어", "eo"), new KeyValuePair<string, string>("에", "e"), new KeyValuePair<string, string>("여", "yeo"), new KeyValuePair<string, string>("예", "ye"),
                new KeyValuePair<string, string>("오", "o"), new KeyValuePair<string, string>("와", "wa"), new KeyValuePair<string, string>("왜", "wae"), new KeyValuePair<string, string>("외", "oe"), new KeyValuePair<string, string>("요", "yo"), new KeyValuePair<string, string>("우", "u"), new KeyValuePair<string, string>("워", "wo"), new KeyValuePair<string, string>("웨", "we"),
                new KeyValuePair<string, string>("위", "wi"), new KeyValuePair<string, string>("유", "yu"), new KeyValuePair<string, string>("으", "eu"), new KeyValuePair<string, string>("의", "ui"), new KeyValuePair<string, string>("이", "i"),
                
                new KeyValuePair<string, string>("자", "ja"), new KeyValuePair<string, string>("재", "jae"), new KeyValuePair<string, string>("쟈", "jya"), new KeyValuePair<string, string>("쟤", "jyae"), new KeyValuePair<string, string>("저", "jeo"), new KeyValuePair<string, string>("제", "je"), new KeyValuePair<string, string>("져", "jyeo"), new KeyValuePair<string, string>("졔", "jye"),
                new KeyValuePair<string, string>("조", "jo"), new KeyValuePair<string, string>("좌", "jwa"), new KeyValuePair<string, string>("좨", "jwae"), new KeyValuePair<string, string>("죄", "joe"), new KeyValuePair<string, string>("죠", "jyo"), new KeyValuePair<string, string>("주", "ju"), new KeyValuePair<string, string>("줘", "jwo"), new KeyValuePair<string, string>("줴", "jwae"),
                new KeyValuePair<string, string>("쥐", "jwi"), new KeyValuePair<string, string>("쥬", "jyu"), new KeyValuePair<string, string>("즈", "jeu"), new KeyValuePair<string, string>("즤", "jui"), new KeyValuePair<string, string>("지", "ji"),
                
                new KeyValuePair<string, string>("차", "cha"), new KeyValuePair<string, string>("채", "chae"), new KeyValuePair<string, string>("챠", "chya"), new KeyValuePair<string, string>("챼", "chyae"), new KeyValuePair<string, string>("처", "cheo"), new KeyValuePair<string, string>("체", "che"), new KeyValuePair<string, string>("쳐", "chyeo"), new KeyValuePair<string, string>("쳬", "chye"),
                new KeyValuePair<string, string>("초", "cho"), new KeyValuePair<string, string>("촤", "chwa"), new KeyValuePair<string, string>("쵀", "chwae"), new KeyValuePair<string, string>("최", "choe"), new KeyValuePair<string, string>("쵸", "chyo"), new KeyValuePair<string, string>("추", "chu"), new KeyValuePair<string, string>("춰", "chwo"), new KeyValuePair<string, string>("췌", "chwae"),
                new KeyValuePair<string, string>("취", "chwi"), new KeyValuePair<string, string>("츄", "chyu"), new KeyValuePair<string, string>("츠", "cheu"), new KeyValuePair<string, string>("츼", "chui"), new KeyValuePair<string, string>("치", "chi"),
                
                new KeyValuePair<string, string>("카", "ka"), new KeyValuePair<string, string>("캐", "kae"), new KeyValuePair<string, string>("캬", "kya"), new KeyValuePair<string, string>("컈", "kyae"), new KeyValuePair<string, string>("커", "keo"), new KeyValuePair<string, string>("케", "ke"), new KeyValuePair<string, string>("켜", "kyeo"), new KeyValuePair<string, string>("켸", "kye"),
                new KeyValuePair<string, string>("코", "ko"), new KeyValuePair<string, string>("콰", "kwa"), new KeyValuePair<string, string>("쾌", "kwae"), new KeyValuePair<string, string>("쾨", "koe"), new KeyValuePair<string, string>("쿄", "kyo"), new KeyValuePair<string, string>("쿠", "ku"), new KeyValuePair<string, string>("퀘", "kwo"), new KeyValuePair<string, string>("퀘", "kwae"),
                new KeyValuePair<string, string>("퀴", "kwi"), new KeyValuePair<string, string>("큐", "kyu"), new KeyValuePair<string, string>("크", "keu"), new KeyValuePair<string, string>("킈", "kui"), new KeyValuePair<string, string>("키", "ki"),
                
                new KeyValuePair<string, string>("타", "ta"), new KeyValuePair<string, string>("태", "tae"), new KeyValuePair<string, string>("탸", "tya"), new KeyValuePair<string, string>("턔", "tyae"), new KeyValuePair<string, string>("터", "teo"), new KeyValuePair<string, string>("테", "te"), new KeyValuePair<string, string>("텨", "tyeo"), new KeyValuePair<string, string>("톄", "tye"),
                new KeyValuePair<string, string>("토", "to"), new KeyValuePair<string, string>("톼", "twa"), new KeyValuePair<string, string>("퇘", "twae"), new KeyValuePair<string, string>("퇴", "toe"), new KeyValuePair<string, string>("툐", "tyo"), new KeyValuePair<string, string>("투", "tu"), new KeyValuePair<string, string>("퉈", "two"), new KeyValuePair<string, string>("퉤", "twae"),
                new KeyValuePair<string, string>("튀", "twi"), new KeyValuePair<string, string>("튜", "tyu"), new KeyValuePair<string, string>("트", "teu"), new KeyValuePair<string, string>("틔", "tui"), new KeyValuePair<string, string>("티", "ti"),
                
                new KeyValuePair<string, string>("파", "pa"), new KeyValuePair<string, string>("패", "pae"), new KeyValuePair<string, string>("퍄", "pya"), new KeyValuePair<string, string>("퍠", "pyae"), new KeyValuePair<string, string>("퍼", "peo"), new KeyValuePair<string, string>("페", "pe"), new KeyValuePair<string, string>("펴", "pyeo"), new KeyValuePair<string, string>("폐", "pye"),
                new KeyValuePair<string, string>("포", "po"), new KeyValuePair<string, string>("퐈", "pwa"), new KeyValuePair<string, string>("퐤", "pwae"), new KeyValuePair<string, string>("푀", "poe"), new KeyValuePair<string, string>("표", "pyo"), new KeyValuePair<string, string>("푸", "pu"), new KeyValuePair<string, string>("풔", "pwo"), new KeyValuePair<string, string>("풰", "pwae"),
                new KeyValuePair<string, string>("퓌", "pwi"), new KeyValuePair<string, string>("퓨", "pyu"), new KeyValuePair<string, string>("프", "peu"), new KeyValuePair<string, string>("픠", "pui"), new KeyValuePair<string, string>("피", "pi"),
                
                new KeyValuePair<string, string>("하", "ha"), new KeyValuePair<string, string>("해", "hae"), new KeyValuePair<string, string>("햐", "hya"), new KeyValuePair<string, string>("햬", "hyae"), new KeyValuePair<string, string>("허", "heo"), new KeyValuePair<string, string>("헤", "he"), new KeyValuePair<string, string>("혀", "hyeo"), new KeyValuePair<string, string>("혜", "hye"),
                new KeyValuePair<string, string>("호", "ho"), new KeyValuePair<string, string>("화", "hwa"), new KeyValuePair<string, string>("홰", "hwae"), new KeyValuePair<string, string>("회", "hoe"), new KeyValuePair<string, string>("효", "hyo"), new KeyValuePair<string, string>("후", "hu"), new KeyValuePair<string, string>("훠", "hwo"), new KeyValuePair<string, string>("훼", "hwe"),
                new KeyValuePair<string, string>("휘", "hwi"), new KeyValuePair<string, string>("류", "hyu"), new KeyValuePair<string, string>("흐", "heu"), new KeyValuePair<string, string>("희", "hui"), new KeyValuePair<string, string>("히", "hi"),
                
            };
            
            string result = input;
            foreach (var replacement in replacements.OrderByDescending(r => r.Key.Length))
            {
                result = result.Replace(replacement.Key, replacement.Value);
            }
            
            return result;
        }
    }
}

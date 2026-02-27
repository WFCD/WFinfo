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
            s = " " + s.Replace("설계도", "").Replace(" ", "");
            t = " " + t.Replace("설계도", "").Replace(" ", "");

            // Normalize Korean characters to Latin equivalents for proper comparison
            s = NormalizeKoreanCharacters(s);
            t = NormalizeKoreanCharacters(t);

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

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Korean
            string normalized = input.ToLower(_culture).Trim();

            // Fix common OCR character substitutions and garbage text FIRST
            normalized = FixCommonOCRErrors(normalized);
            
            // Preprocess common Korean OCR spacing issues
            normalized = FixKoreanSpacing(normalized);

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Korean)
            normalized = RemoveAccents(normalized);

            // Remove extra spaces and normalize spacing
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string result = string.Join(" ", parts);
            
            return result;
        }
        
        /// <summary>
        /// Fixes common spacing issues in Korean OCR text
        /// Korean OCR often misses spaces between words or adds incorrect spaces
        /// </summary>
        /// <param name="input">Input string with spacing issues</param>
        /// <returns>String with corrected spacing</returns>
        private static string FixKoreanSpacing(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            string result = input;
            
            // Add spaces before common Korean suffixes and particles that are often concatenated
            result = Regex.Replace(result, "(프라임)(?=[가-힣])", "$1 "); // Prime + Korean
            result = Regex.Replace(result, "(설계도)(?=[가-힣])", "$1 "); // Blueprint + Korean
            result = Regex.Replace(result, "([가-힣])(?=프라임)", "$1 "); // Korean + Prime
            result = Regex.Replace(result, "([가-힣])(?=설계도)", "$1 "); // Korean + Blueprint
            
            // Fix common concatenated part names using patterns only
            result = Regex.Replace(result, "([가-힣]{2,4})(프라임)", "$1 $2");
            result = Regex.Replace(result, "(프라임)(뉴로옵틱스|섀시|리시버|건틀렛|핸들|블레이드|시스템|스트링)", "$1 $2");
            result = Regex.Replace(result, "(뉴로옵틱스|섀시|리시버|건틀렛|핸들|블레이드|시스템|스트링)(설계도)", "$1 $2");
            
            // Specific fix for neuroptics blueprint concatenation
            result = Regex.Replace(result, "뉴로옵틱스설계도", "뉴로옵틱스 설계도");
            result = Regex.Replace(result, "뉴로옵틱스 설계도", "뉴로옵틱스 설계도");
            
            // Add spaces between Korean words when they're concatenated (heuristic approach)
            result = Regex.Replace(result, "([가-힣]{2,4})([가-힣]{2,4})(?=[가-힣]|$)", m => {
                string word1 = m.Groups[1].Value;
                string word2 = m.Groups[2].Value;
                
                // Common part type patterns that should have spaces
                var partTypes = new[] { "프라임", "뉴로옵틱스", "섀시", "리시버", "건틀렛", "핸들", "블레이드", "시스템", "스트링", "설계도" };
                
                if (partTypes.Contains(word1, StringComparer.Ordinal) || partTypes.Contains(word2, StringComparer.Ordinal))
                {
                    return word1 + " " + word2;
                }
                
                return m.Value;
            });
            
            return result;
        }
        
        /// <summary>
        /// Fixes common OCR character substitutions and confusions in Korean text
        /// </summary>
        /// <param name="input">Input string with OCR errors</param>
        /// <returns>String with corrected characters</returns>
        private static string FixCommonOCRErrors(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // Apply pattern-based fixes FIRST before character-level replacements
            var patternCorrections = new Dictionary<string, string>
            {
                {"속스프", ""}, // Common OCR garbage text
                {"스프", ""}, // Common OCR garbage suffix
                {"속스", ""}, // Common OCR garbage prefix
                {"노스프킨", "뉴로옵틱스"}, // Scrambled neuroptics pattern
                {"온티스석", "옵틱스"}, // Scrambled optics pattern
                {"오티스석", "옵틱스"}, // Alternative scrambled optics pattern
                {"버1", ""}, // Common OCR garbage suffix
                {"버", ""}, // Common OCR garbage character
                
                // Common OCR corrections for Prime parts
                {"프라임", "prime"}, {"프리임", "prime"}, {"프라읍", "prime"},
                // Removed "설계도" → "blueprint" to keep Korean text intact
            };
            
            string result = input;
            foreach (var correction in patternCorrections.OrderByDescending(c => c.Key.Length))
            {
                result = result.Replace(correction.Key, correction.Value);
            }
            
            // Apply spacing corrections
            var spacingCorrections = new Dictionary<string, string>
            {
                {"  ", " "}, {"   ", " "}, {"    ", " "}
            };
            
            foreach (var correction in spacingCorrections.OrderByDescending(c => c.Key.Length))
            {
                result = result.Replace(correction.Key, correction.Value);
            }
            
            return result;
        }

        public override bool IsPartNameValid(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return false;
            
            // Apply basic OCR fixes before validation
            string cleaned = FixCommonOCRErrors(partName);
            
            // Korean requires minimum of 6 characters after removing spaces
            return cleaned.Replace(" ", "").Length >= 6;
        }

        public override bool ShouldFilterWord(string word)
        {
            // Korean filtering: don't filter short Korean words as they may be valid parts of compound words
            // Only filter out actual garbage (null/empty) and very short single characters
            // Also preserve common Korean OCR fragments that might be parts of words
            var validKoreanFragments = new[] { "노", "스", "프", "킨", "옵", "틱", "석", "계", "도", "이쿼", "녹스" };
            
            return string.IsNullOrEmpty(word) || (word.Length == 1 && !validKoreanFragments.Contains(word));
        }

        
        /// <summary>
        /// Gets the character difference cost for Korean characters based on similarity groups
        /// </summary>
        private int GetKoreanCharacterDifference(char a, char b)
        {
            if (a == b) return 0;

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
        /// Normalizes Korean Hangul characters to Latin equivalents for comparison
        /// Uses comprehensive mapping for common OCR confusions and variations
        /// </summary>
        private static string NormalizeKoreanCharacters(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // Common OCR character substitutions and confusions
            var replacements = new Dictionary<string, string>
            {
                // Basic consonants and vowels
                {"가", "ga"}, {"개", "gae"}, {"갸", "gya"}, {"걔", "gyae"}, {"거", "geo"}, {"게", "ge"}, {"겨", "gyeo"}, {"계", "gye"},
                {"고", "go"}, {"과", "gwa"}, {"궈", "gwo"}, {"괘", "gwae"}, {"괴", "goe"}, {"교", "gyo"}, {"구", "gu"}, {"궈", "gwo"},
                {"궤", "gwe"}, {"귀", "gwi"}, {"규", "gyu"}, {"그", "geu"}, {"긔", "gui"}, {"기", "gi"},
                
                {"나", "na"}, {"내", "nae"}, {"냐", "nya"}, {"냬", "nyae"}, {"너", "neo"}, {"네", "ne"}, {"녀", "nyeo"}, {"녜", "nye"},
                {"노", "no"}, {"놔", "nwa"}, {"놰", "nwo"}, {"놰", "nwae"}, {"뇌", "noe"}, {"뇨", "nyo"}, {"누", "nu"}, {"뉘", "nwi"},
                {"뉴", "nyu"}, {"느", "neu"}, {"늬", "nui"}, {"니", "ni"},
                
                {"다", "da"}, {"대", "dae"}, {"댜", "dya"}, {"댸", "dyae"}, {"더", "deo"}, {"데", "de"}, {"뎌", "dyeo"}, {"뎨", "dye"},
                {"도", "do"}, {"돠", "dwa"}, {"돼", "dwae"}, {"돼", "doe"}, {"됴", "dyo"}, {"두", "du"}, {"둬", "dwo"}, {"뒈", "dwae"},
                {"뒤", "dwi"}, {"듀", "dyu"}, {"드", "deu"}, {"듸", "dui"}, {"디", "di"},
                
                {"라", "ra"}, {"래", "rae"}, {"랴", "rya"}, {"럐", "ryae"}, {"러", "reo"}, {"레", "re"}, {"려", "ryeo"}, {"례", "rye"},
                {"로", "ro"}, {"롸", "rwa"}, {"뢔", "roe"}, {"료", "ryo"}, {"루", "ru"}, {"뤄", "rwo"}, {"뤠", "rwae"}, {"뤼", "rwi"},
                {"류", "ryu"}, {"르", "reu"}, {"릐", "rui"}, {"리", "ri"},
                
                {"마", "ma"}, {"매", "mae"}, {"먀", "mya"}, {"먜", "myae"}, {"머", "meo"}, {"메", "me"}, {"며", "myeo"}, {"몌", "mye"},
                {"모", "mo"}, {"뫄", "mwa"}, {"뫠", "mwae"}, {"뫼", "moe"}, {"묘", "myo"}, {"무", "mu"}, {"뭐", "mwo"}, {"뭬", "mwae"},
                {"뮈", "mwi"}, {"뮤", "myu"}, {"므", "meu"}, {"믜", "mui"}, {"미", "mi"},
                
                {"바", "ba"}, {"배", "bae"}, {"뱌", "bya"}, {"뱨", "byae"}, {"버", "beo"}, {"베", "be"}, {"벼", "byeo"}, {"볘", "bye"},
                {"보", "bo"}, {"봐", "bwa"}, {"봬", "bwae"}, {"뵈", "boe"}, {"뵤", "byo"}, {"부", "bu"}, {"붜", "bwo"}, {"붸", "bwae"},
                {"뷔", "bwi"}, {"뷰", "byu"}, {"브", "beu"}, {"븨", "bui"}, {"비", "bi"},
                
                {"사", "sa"}, {"새", "sae"}, {"샤", "sya"}, {"섀", "syae"}, {"서", "seo"}, {"세", "se"}, {"셔", "syeo"}, {"셰", "sye"},
                {"소", "so"}, {"솨", "swa"}, {"쇄", "swae"}, {"쇠", "soe"}, {"쇼", "syo"}, {"수", "su"}, {"숴", "swo"}, {"쉐", "swae"},
                {"쉬", "swi"}, {"슈", "syu"}, {"스", "seu"}, {"싀", "sui"}, {"시", "si"},
                
                {"아", "a"}, {"애", "ae"}, {"야", "ya"}, {"얘", "yae"}, {"어", "eo"}, {"에", "e"}, {"여", "yeo"}, {"예", "ye"},
                {"오", "o"}, {"와", "wa"}, {"왜", "wae"}, {"외", "oe"}, {"요", "yo"}, {"우", "u"}, {"워", "wo"}, {"웨", "we"},
                {"위", "wi"}, {"유", "yu"}, {"으", "eu"}, {"의", "ui"}, {"이", "i"},
                
                {"자", "ja"}, {"재", "jae"}, {"쟈", "jya"}, {"쟤", "jyae"}, {"저", "jeo"}, {"제", "je"}, {"져", "jyeo"}, {"졔", "jye"},
                {"조", "jo"}, {"좌", "jwa"}, {"좨", "jwae"}, {"죄", "joe"}, {"죠", "jyo"}, {"주", "ju"}, {"줘", "jwo"}, {"줴", "jwae"},
                {"쥐", "jwi"}, {"쥬", "jyu"}, {"즈", "jeu"}, {"즤", "jui"}, {"지", "ji"},
                
                {"차", "cha"}, {"채", "chae"}, {"챠", "chya"}, {"챼", "chyae"}, {"처", "cheo"}, {"체", "che"}, {"쳐", "chyeo"}, {"쳬", "chye"},
                {"초", "cho"}, {"촤", "chwa"}, {"쵀", "chwae"}, {"최", "choe"}, {"쵸", "chyo"}, {"추", "chu"}, {"춰", "chwo"}, {"췌", "chwae"},
                {"취", "chwi"}, {"츄", "chyu"}, {"츠", "cheu"}, {"츼", "chui"}, {"치", "chi"},
                
                {"카", "ka"}, {"캐", "kae"}, {"캬", "kya"}, {"컈", "kyae"}, {"커", "keo"}, {"케", "ke"}, {"켜", "kyeo"}, {"켸", "kye"},
                {"코", "ko"}, {"콰", "kwa"}, {"쾌", "kwae"}, {"쾨", "koe"}, {"쿄", "kyo"}, {"쿠", "ku"}, {"퀘", "kwo"}, {"퀘", "kwae"},
                {"퀴", "kwi"}, {"큐", "kyu"}, {"크", "keu"}, {"킈", "kui"}, {"키", "ki"},
                
                {"타", "ta"}, {"태", "tae"}, {"탸", "tya"}, {"턔", "tyae"}, {"터", "teo"}, {"테", "te"}, {"텨", "tyeo"}, {"톄", "tye"},
                {"토", "to"}, {"톼", "twa"}, {"퇘", "twae"}, {"퇴", "toe"}, {"툐", "tyo"}, {"투", "tu"}, {"퉈", "two"}, {"퉤", "twae"},
                {"튀", "twi"}, {"튜", "tyu"}, {"트", "teu"}, {"틔", "tui"}, {"티", "ti"},
                
                {"파", "pa"}, {"패", "pae"}, {"퍄", "pya"}, {"퍠", "pyae"}, {"퍼", "peo"}, {"페", "pe"}, {"펴", "pyeo"}, {"폐", "pye"},
                {"포", "po"}, {"퐈", "pwa"}, {"퐤", "pwae"}, {"푀", "poe"}, {"표", "pyo"}, {"푸", "pu"}, {"풔", "pwo"}, {"풰", "pwae"},
                {"퓌", "pwi"}, {"퓨", "pyu"}, {"프", "peu"}, {"픠", "pui"}, {"피", "pi"},
                
                {"하", "ha"}, {"해", "hae"}, {"햐", "hya"}, {"햬", "hyae"}, {"허", "heo"}, {"헤", "he"}, {"혀", "hyeo"}, {"혜", "hye"},
                {"호", "ho"}, {"화", "hwa"}, {"홰", "hwae"}, {"회", "hoe"}, {"효", "hyo"}, {"후", "hu"}, {"훠", "hwo"}, {"훼", "hwe"},
                {"휘", "hwi"}, {"류", "hyu"}, {"흐", "heu"}, {"희", "hui"}, {"히", "hi"},
                
                {"속스프", ""}, // Common OCR garbage text
                {"스프", ""}, // Common OCR garbage suffix
                {"속스", ""}, // Common OCR garbage prefix
                {"노스프킨", "뉴로옵틱스"}, // Scrambled neuroptics pattern
                {"오티스석", "옵틱스 설계도"}, // Scrambled optics blueprint pattern
                {"온티스석", "옵틱스 설계도"}, // Alternative scrambled optics blueprint pattern
                {"버1", ""}, // Common OCR garbage suffix
                {"버", ""}, // Common OCR garbage character
                
                // Common OCR corrections for Prime parts
                {"프라임", "prime"}, {"프리임", "prime"}, {"프라읍", "prime"},
                {"설계도", "blueprint"},
                
                // Common character confusions in OCR
                {"리", "ri"}, {"이", "i"}, {"ㄱ", "k"}, {"ㄴ", "n"}, {"ㄷ", "t"}, {"ㄹ", "r"}, {"ㅁ", "m"}, {"ㅂ", "p"}, {"ㅅ", "s"}, {"ㅇ", "ng"}, {"ㅈ", "j"}, {"ㅊ", "ch"}, {"ㅋ", "k"}, {"ㅌ", "t"}, {"ㅍ", "p"}, {"ㅎ", "h"}
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

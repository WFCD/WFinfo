using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Japanese language processor for OCR text processing
    /// Handles Japanese Hiragana, Katakana, and Kanji characters
    /// </summary>
    public class JapaneseLanguageProcessor : LanguageProcessor
    {
        public JapaneseLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "ja";

        public override string[] BlueprintRemovals => new[] { "設計図", "青図" };

        public override string CharacterWhitelist => 
            GenerateCharacterRange(0x3040, 0x309F) + 
            GenerateCharacterRange(0x30A0, 0x30FF) + 
            string.Concat(GenerateCharacterRangeIterator(0x4E00, 0x6FFF)) + 
            GenerateCharacterRange(0x7000, 0x7FFF) + 
            GenerateCharacterRange(0x8000, 0x9FAF) + 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz "; // Japanese Hiragana, Katakana, Kanji

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            // Check if both inputs contain Japanese characters for Japanese-aware comparison
            bool sHasJapanese = ContainsJapanese(s);
            bool tHasJapanese = ContainsJapanese(t);
            
            if (sHasJapanese && tHasJapanese)
            {
                // Japanese-aware path: use original Japanese characters with Japanese similarity logic
                return CalculateJapaneseAwareDistance(s, t);
            }
            else
            {
                // Fallback/transliterated path: normalize to Latin equivalents
                return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeJapaneseCharacters, callBaseDefault: true);
            }
        }

        /// <summary>
        /// Calculates Japanese-aware Levenshtein distance with character similarity groups
        /// </summary>
        private int CalculateJapaneseAwareDistance(string s, string t)
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
                    int cost = GetJapaneseCharacterDifference(s[i - 1], t[j - 1]);
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Gets the character difference cost for Japanese characters based on similarity groups
        /// </summary>
        private int GetJapaneseCharacterDifference(char a, char b)
        {
            if (a == b) return 0;

            // Hiragana-Katakana equivalents (lower cost for similar characters)
            var hiraganaKatakanaPairs = new Dictionary<char, char>
            {
                {'あ', 'ア'}, {'い', 'イ'}, {'う', 'ウ'}, {'え', 'エ'}, {'お', 'オ'},
                {'か', 'カ'}, {'き', 'キ'}, {'く', 'ク'}, {'け', 'ケ'}, {'こ', 'コ'},
                {'が', 'ガ'}, {'ぎ', 'ギ'}, {'ぐ', 'グ'}, {'げ', 'ゲ'}, {'ご', 'ゴ'},
                {'さ', 'サ'}, {'し', 'シ'}, {'す', 'ス'}, {'せ', 'セ'}, {'そ', 'ソ'},
                {'ざ', 'ザ'}, {'じ', 'ジ'}, {'ず', 'ズ'}, {'ぜ', 'ゼ'}, {'ぞ', 'ゾ'},
                {'た', 'タ'}, {'ち', 'チ'}, {'つ', 'ツ'}, {'て', 'テ'}, {'と', 'ト'},
                {'だ', 'ダ'}, {'ぢ', 'ヂ'}, {'づ', 'ヅ'}, {'で', 'デ'}, {'ど', 'ド'},
                {'な', 'ナ'}, {'に', 'ニ'}, {'ぬ', 'ヌ'}, {'ね', 'ネ'}, {'の', 'ノ'},
                {'は', 'ハ'}, {'ひ', 'ヒ'}, {'ふ', 'フ'}, {'へ', 'ヘ'}, {'ほ', 'ホ'},
                {'ば', 'バ'}, {'び', 'ビ'}, {'ぶ', 'ブ'}, {'べ', 'ベ'}, {'ぼ', 'ボ'},
                {'ぱ', 'パ'}, {'ぴ', 'ピ'}, {'ぷ', 'プ'}, {'ぺ', 'ペ'}, {'ぽ', 'ポ'},
                {'ま', 'マ'}, {'み', 'ミ'}, {'む', 'ム'}, {'め', 'メ'}, {'も', 'モ'},
                {'や', 'ヤ'}, {'ゆ', 'ユ'}, {'よ', 'ヨ'},
                {'ら', 'ラ'}, {'り', 'リ'}, {'る', 'ル'}, {'れ', 'レ'}, {'ろ', 'ロ'},
                {'わ', 'ワ'}, {'ゐ', 'ヰ'}, {'ゑ', 'ヱ'}, {'を', 'ヲ'}, {'ん', 'ン'},
                {'っ', 'ッ'}, {'ゃ', 'ャ'}, {'ゅ', 'ュ'}, {'ょ', 'ョ'}
            };

            // Check if characters are hiragana-katakana equivalents
            if (hiraganaKatakanaPairs.TryGetValue(a, out var katakanaEquiv) && katakanaEquiv == b)
                return 1; // Low cost for hiragana-katakana equivalents
            if (hiraganaKatakanaPairs.TryGetValue(b, out var hiraganaEquiv) && hiraganaEquiv == a)
                return 1;

            // Similar looking characters (common OCR confusions)
            var similarChars = new[]
            {
                new[] {'シ', 'ツ'}, // shi/tsu confusion
                new[] {'ソ', 'ン'}, // so/n confusion  
                new[] {'ク', 'ワ'}, // ku/wa confusion
                new[] {'ヘ', 'へ'}, // he/he (different forms)
                new[] {'ベ', 'べ'}, // be/be (different forms)
                new[] {'ヲ', 'ヲ'}, // wo/wo (different forms)
                new[] {'ヶ', 'ケ'}, // ke/ke variation
                new[] {'ヵ', 'カ'}, // ka/ka variation
            };

            foreach (var pair in similarChars)
            {
                if ((a == pair[0] && b == pair[1]) || (a == pair[1] && b == pair[0]))
                    return 1; // Low cost for similar looking characters
            }

            // Default cost for different characters
            return 2;
        }

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Apply Japanese-specific normalization first
            string normalized = NormalizeJapaneseCharacters(input);

            // Basic cleanup for Japanese
            normalized = normalized.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Japanese - preserve combining marks)
            // normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Japanese requires minimum of 4 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 4;
        }

        
        public override bool ShouldFilterWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return true;
            
            bool hasJapanese = ContainsJapanese(word);
            bool hasLatin = false;
            foreach (char c in word)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    hasLatin = true;
                    break;
                }
            }
            
            // Keep all Japanese text (Hiragana/Katakana/Kanji characters) since Japanese words are meaningful
            // even when split by OCR
            if (hasJapanese) return false;
            
            // For mixed Japanese-Latin words, be more lenient
            if (hasJapanese && hasLatin) return false;
            
            // For non-Japanese text, use standard filtering (filter very short words)
            return word.Length < 2;
        }

        /// <summary>
        /// Checks if a string contains Japanese characters (Hiragana, Katakana, or Kanji)
        /// </summary>
        private static bool ContainsJapanese(string input)
        {
            foreach (char c in input)
            {
                // Hiragana (0x3040-0x309F)
                if (c >= 0x3040 && c <= 0x309F) return true;
                // Katakana (0x30A0-0x30FF)
                if (c >= 0x30A0 && c <= 0x30FF) return true;
                // Kanji (0x4E00-0x9FAF)
                if (c >= 0x4E00 && c <= 0x9FAF) return true;
            }
            return false;
        }

        /// <summary>
        /// Normalizes Japanese characters for comparison
        /// </summary>
        private static string NormalizeJapaneseCharacters(string input)
        {
            string result = NormalizeFullWidthCharacters(input);
            
            // Normalize katakana/hiragana variations and common OCR confusions
            result = result.Replace('ヶ', 'ケ').Replace('ヵ', 'カ');
            result = result.Replace('ﾞ', '゛').Replace('ﾟ', '゜'); // Handakuten and Dakuten normalization
            
            // Common katakana OCR confusions
            result = result.Replace('ヲ', 'ヲ').Replace('ヮ', 'ワ').Replace('ヰ', 'イ').Replace('ヱ', 'エ').Replace('ヲ', 'オ');
            
            return result.ToLowerInvariant();
        }
    }
}

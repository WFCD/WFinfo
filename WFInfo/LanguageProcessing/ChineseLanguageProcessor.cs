using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Base class for Chinese language processors containing shared behaviors
    /// </summary>
    public abstract class ChineseLanguageProcessorBase : LanguageProcessor
    {
        protected ChineseLanguageProcessorBase(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string CharacterWhitelist => 
            string.Concat(GenerateCharacterRangeIterator(0x4E00, 0x7FFF)) + 
            string.Concat(GenerateCharacterRangeIterator(0x8000, 0x9FFF)) + 
            GenerateCharacterRange(0x3400, 0x4DBF) + 
            GenerateCharacterRange(0xF900, 0xFAFF) + 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz "; // Full CJK ideographs

        public override string NormalizeForPatternMatching(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Basic cleanup for Chinese
            string normalized = input.ToLower(_culture).Trim();

            // Add spaces around "Prime" to match database format better
            normalized = normalized.Replace("prime", " prime ");

            // Remove accents (not typically needed for Chinese)
            normalized = RemoveAccents(normalized);

            // Remove extra spaces
            var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public override bool IsPartNameValid(string partName)
        {
            // Chinese requires minimum of 4 characters after removing spaces
            return !string.IsNullOrEmpty(partName) && partName.Replace(" ", "").Length >= 4;
        }

        public override bool ShouldFilterWord(string word)
        {
            return FilterWordCore(word);
        }

        /// <summary>
        /// Shared filtering logic for Chinese word processing
        /// </summary>
        public static bool FilterWordCore(string word)
        {
            if (string.IsNullOrEmpty(word)) return true;
            
            bool hasCJK = ContainsCJK(word);
            bool hasLatin = false;
            foreach (char c in word)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    hasLatin = true;
                    break;
                }
            }
            
            // Pure CJK words: keep (even single chars are meaningful in Chinese)
            if (hasCJK && !hasLatin) return false;
            
            // Pure Latin words: shortest valid item name component is 3 chars (Ash, Nyx, Mag)
            // Filter Latin-only words with <= 2 chars ("ll", "ee", "on", "me" = OCR noise from UI)
            if (hasLatin && !hasCJK) return word.Length <= 2;
            
            // Mixed Latin+CJK: filter short mixed words (like "G壬") which are OCR garbage
            // Valid mixed text is always longer (e.g. "Prime" next to CJK is separate words)
            if (hasCJK && hasLatin && word.Length <= 2) return true;
            
            // Keep everything else
            return false;
        }

        /// <summary>
        /// Checks if a string contains CJK characters
        /// </summary>
        public static bool ContainsCJK(string text)
        {
            foreach (char c in text)
            {
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF) || (c >= 0xF900 && c <= 0xFAFF))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Normalizes Chinese characters for comparison
        /// </summary>
        protected static string NormalizeChineseCharacters(string input)
        {
            return NormalizeFullWidthCharacters(input).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Simplified Chinese language processor for OCR text processing
    /// Handles Simplified Chinese characters
    /// </summary>
    public class SimplifiedChineseLanguageProcessor : ChineseLanguageProcessorBase
    {
        public SimplifiedChineseLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "zh-hans";

        public override string[] BlueprintRemovals => new[] { "蓝图", "设计图" };

        private static readonly IReadOnlyDictionary<string, string> _ignoredItemNames = new Dictionary<string, string>
        {
            ["Forma Blueprint"] = "Forma 蓝图",
            ["Exilus Weapon Adapter Blueprint"] = "Exilus 武器适配器 蓝图",
            ["Kuva"] = "赤毒",
            ["Riven Sliver"] = "裂罅碎块",
            ["Ayatan Amber Star"] = "阿耶檀识 琥珀星",
            ["Galariak Prime Blueprint"] = "加拉瑞克 Prime 蓝图",
            ["Galariak Prime Blade"] = "加拉瑞克 Prime 刀刃 蓝图",
            ["Galariak Prime Handle"] = "加拉瑞克 Prime 握柄 蓝图",
            ["Sagek Prime Blueprint"] = "萨杰克 Prime 蓝图",
            ["Sagek Prime Barrel"] = "萨杰克 Prime 枪管 蓝图",
            ["Sagek Prime Receiver"] = "萨杰克 Prime 枪机 蓝图"
        };

        public override IReadOnlyDictionary<string, string> IgnoredItemNames => _ignoredItemNames;

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeChineseCharacters, callBaseDefault: true);
        }
    }

    /// <summary>
    /// Traditional Chinese language processor for OCR text processing
    /// Handles Traditional Chinese characters
    /// </summary>
    public class TraditionalChineseLanguageProcessor : ChineseLanguageProcessorBase
    {
        public TraditionalChineseLanguageProcessor(IReadOnlyApplicationSettings settings) : base(settings)
        {
        }

        public override string Locale => "zh-hant";

        public override string[] BlueprintRemovals => new[] { "藍圖", "設計圖" };

        private static readonly IReadOnlyDictionary<string, string> _ignoredItemNames = new Dictionary<string, string>
        {
            ["Forma Blueprint"] = "Forma 藍圖",
            ["Exilus Weapon Adapter Blueprint"] = "Exilus 武器適配器 藍圖",
            ["Kuva"] = "赤毒",
            ["Riven Sliver"] = "裂罅碎塊",
            ["Ayatan Amber Star"] = "阿耶檀識 琥珀星",
            ["Galariak Prime Blueprint"] = "加拉瑞克 Prime 藍圖",
            ["Galariak Prime Blade"] = "加拉瑞克 Prime 刀刃 藍圖",
            ["Galariak Prime Handle"] = "加拉瑞克 Prime 握柄 藍圖",
            ["Sagek Prime Blueprint"] = "薩傑克 Prime 藍圖",
            ["Sagek Prime Barrel"] = "薩傑克 Prime 槍管 藍圖",
            ["Sagek Prime Receiver"] = "薩傑克 Prime 槍機 藍圖"
        };

        public override IReadOnlyDictionary<string, string> IgnoredItemNames => _ignoredItemNames;

        public override int CalculateLevenshteinDistance(string s, string t)
        {
            return LevenshteinDistanceWithPreprocessing(s, t, BlueprintRemovals, NormalizeChineseCharacters, callBaseDefault: true);
        }
    }
}

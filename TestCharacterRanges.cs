using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using WFInfo.LanguageProcessing;
using WFInfo.Settings;

namespace WFInfo.Test
{
    /// <summary>
    /// Simple test to verify character range generation works correctly
    /// </summary>
    public class TestCharacterRanges
    {
        // Known minimum whitelist lengths — any shorter indicates a regression
        private const int MinJapaneseWhitelistLength = 10000;
        private const int MinKoreanWhitelistLength = 5000;
        private const int MinSimplifiedChineseWhitelistLength = 10000;
        private const int MinTraditionalChineseWhitelistLength = 10000;

        public static void RunTestCharacterRanges()
        {
            var settings = new TestApplicationSettings();
            LanguageProcessorFactory.Initialize(settings);
            LanguageProcessorFactory.ClearCache();

            AssertWhitelistLength("ja", MinJapaneseWhitelistLength);
            AssertWhitelistLength("ko", MinKoreanWhitelistLength);
            AssertWhitelistLength("zh-Hans", MinSimplifiedChineseWhitelistLength);
            AssertWhitelistLength("zh-Hant", MinTraditionalChineseWhitelistLength);
        }

        private static void AssertWhitelistLength(string locale, int minExpected)
        {
            var processor = LanguageProcessorFactory.GetProcessor(locale);
            string whitelist = processor.CharacterWhitelist;
            Debug.Assert(
                whitelist != null && whitelist.Length >= minExpected,
                $"CharacterWhitelist for '{locale}' has length {whitelist?.Length ?? 0}, expected >= {minExpected}");
            if (whitelist == null || whitelist.Length < minExpected)
                throw new InvalidOperationException(
                    $"CharacterWhitelist regression: '{locale}' length={whitelist?.Length ?? 0}, expected >= {minExpected}");
        }
    }
    
    /// <summary>
    /// Mock application settings for testing
    /// </summary>
    public class TestApplicationSettings : IReadOnlyApplicationSettings
    {
        public Display Display => Display.Window;
        public double MainWindowLocation_X => 0;
        public double MainWindowLocation_Y => 0;
        public Point MainWindowLocation => new Point(0, 0);
        public bool IsOverlaySelected => false;
        public bool IsLightSelected => false;
        public string ActivationKey => "";
        public Key? ActivationKeyKey => null;
        public MouseButton? ActivationMouseButton => null;
        public Key DebugModifierKey => Key.None;
        public Key SearchItModifierKey => Key.None;
        public Key SnapitModifierKey => Key.None;
        public Key MasterItModifierKey => Key.None;
        public bool Debug => false;
        public string Locale => "en";
        public bool Clipboard => false;
        public long AutoDelay => 0;
        public int ImageRetentionTime => 0;
        public string ClipboardTemplate => "";
        public bool SnapitExport => false;
        public int Delay => 0;
        public bool HighlightRewards => false;
        public bool ClipboardVaulted => false;
        public bool Auto => false;
        public bool HighContrast => false;
        public int OverlayXOffsetValue => 0;
        public int OverlayYOffsetValue => 0;
        public bool AutoList => false;
        public bool AutoCSV => false;
        public bool AutoCount => false;
        public bool DoDoubleCheck => false;
        public double MaximumEfficiencyValue => 0;
        public double MinimumEfficiencyValue => 0;
        public bool DoSnapItCount => false;
        public int SnapItDelay => 0;
        public double SnapItHorizontalNameMargin => 0;
        public bool DoCustomNumberBoxWidth => false;
        public double SnapItNumberBoxWidth => 0;
        public bool SnapMultiThreaded => false;
        public double SnapRowTextDensity => 0;
        public double SnapRowEmptyDensity => 0;
        public double SnapColEmptyDensity => 0;
        public int MinOverlayWidth => 0;
        public int MaxOverlayWidth => 0;
        public WFtheme ThemeSelection => WFtheme.AUTO;
        public bool CF_usePrimaryHSL => false;
        public bool CF_usePrimaryRGB => false;
        public bool CF_useSecondaryHSL => false;
        public bool CF_useSecondaryRGB => false;
        public float CF_pHueMax => 0;
        public float CF_pHueMin => 0;
        public float CF_pSatMax => 0;
        public float CF_pSatMin => 0;
        public float CF_pBrightMax => 0;
        public float CF_pBrightMin => 0;
        public int CF_pRMax => 0;
        public int CF_pRMin => 0;
        public int CF_pGMax => 0;
        public int CF_pGMin => 0;
        public int CF_pBMax => 0;
        public int CF_pBMin => 0;
        public float CF_sHueMax => 0;
        public float CF_sHueMin => 0;
        public float CF_sSatMax => 0;
        public float CF_sSatMin => 0;
        public float CF_sBrightMax => 0;
        public float CF_sBrightMin => 0;
        public int CF_sRMax => 0;
        public int CF_sRMin => 0;
        public int CF_sGMax => 0;
        public int CF_sGMin => 0;
        public int CF_sBMax => 0;
        public int CF_sBMin => 0;
        public long FixedAutoDelay => 0;
        public string Ignored => "";
        public HdrSupportEnum HdrSupport => HdrSupportEnum.None;
    }
}

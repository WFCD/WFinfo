using System;
using WFInfo.LanguageProcessing;
using WFInfo.Settings;

namespace WFInfo.Test
{
    /// <summary>
    /// Simple test to verify character range generation works correctly
    /// </summary>
    public class TestCharacterRanges
    {
        public static void RunTestCharacterRanges()
        {
            Console.WriteLine("Testing character range generation...");
            
            // Create a mock settings object
            var settings = new TestApplicationSettings();
            
            try
            {
                // Test Japanese processor
                var japaneseProcessor = new JapaneseLanguageProcessor(settings);
                var japaneseWhitelist = japaneseProcessor.CharacterWhitelist;
                Console.WriteLine($"Japanese whitelist length: {japaneseWhitelist.Length}");
                
                // Test Korean processor  
                var koreanProcessor = new KoreanLanguageProcessor(settings);
                var koreanWhitelist = koreanProcessor.CharacterWhitelist;
                Console.WriteLine($"Korean whitelist length: {koreanWhitelist.Length}");
                
                // Test Chinese processors
                var simplifiedProcessor = new SimplifiedChineseLanguageProcessor(settings);
                var simplifiedWhitelist = simplifiedProcessor.CharacterWhitelist;
                Console.WriteLine($"Simplified Chinese whitelist length: {simplifiedWhitelist.Length}");
                
                var traditionalProcessor = new TraditionalChineseLanguageProcessor(settings);
                var traditionalWhitelist = traditionalProcessor.CharacterWhitelist;
                Console.WriteLine($"Traditional Chinese whitelist length: {traditionalWhitelist.Length}");
                
                Console.WriteLine("All character range tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing character ranges: {ex.Message}");
                throw;
            }
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
        public System.Windows.Point MainWindowLocation => new System.Windows.Point(0, 0);
        public bool IsOverlaySelected => false;
        public bool IsLightSelected => false;
        public string ActivationKey => "";
        public System.Windows.Input.Key? ActivationKeyKey => null;
        public System.Windows.Input.MouseButton? ActivationMouseButton => null;
        public System.Windows.Input.Key DebugModifierKey => System.Windows.Input.Key.None;
        public System.Windows.Input.Key SearchItModifierKey => System.Windows.Input.Key.None;
        public System.Windows.Input.Key SnapitModifierKey => System.Windows.Input.Key.None;
        public System.Windows.Input.Key MasterItModifierKey => System.Windows.Input.Key.None;
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

using System.Windows;
using System.Windows.Input;

namespace WFInfo.Settings
{
    public enum Display
    {
        Window,
        Overlay,
        Light
    }
    /// <summary>
    /// Readonly copy of the settings for services to use
    /// </summary>
    public interface IReadOnlyApplicationSettings
    {
        Display Display { get; }
        double MainWindowLocation_X { get; }
        double MainWindowLocation_Y { get; }
        Point MainWindowLocation { get; }
        bool IsOverlaySelected { get; }
        bool IsLightSelected { get; }
        string ActivationKey { get; }
        Key? ActivationKeyKey { get; }
        MouseButton? ActivationMouseButton { get; }
        Key DebugModifierKey { get; }
        Key SearchItModifierKey { get; }
        Key SnapitModifierKey { get; }
        Key MasterItModifierKey { get; }
        bool Debug { get; }
        string Locale { get; }
        bool Clipboard { get; }
        long AutoDelay { get; }
        int ImageRetentionTime { get; }
        string ClipboardTemplate { get; }
        bool SnapitExport { get; }
        int Delay { get; }
        bool HighlightRewards { get; }
        bool ClipboardVaulted { get; }
        bool Auto { get; }
        bool HighContrast { get; }
        int OverlayXOffsetValue { get; }
        int OverlayYOffsetValue { get; }
        bool AutoList { get; }
        bool DoDoubleCheck { get; }
        double MaximumEfficiencyValue { get; }
        double MinimumEfficiencyValue { get; }
        bool DoSnapItCount { get; }
        int SnapItDelay { get; }
        double SnapItHorizontalNameMargin { get; }
        bool DoCustomNumberBoxWidth { get; }
        double SnapItNumberBoxWidth { get; }
        bool SnapMultiThreaded { get; }
        double SnapRowTextDensity { get; }
        double SnapRowEmptyDensity { get; }
        double SnapColEmptyDensity { get; }
        int MinOverlayWidth { get; }
        int MaxOverlayWidth { get; }
        WFtheme ThemeSelection { get; }
        bool CF_usePrimaryHSL { get; }
        bool CF_usePrimaryRGB { get; }
        bool CF_useSecondaryHSL { get; }
        bool CF_useSecondaryRGB { get; }
        float CF_pHueMax { get; }
        float CF_pHueMin { get; }
        float CF_pSatMax { get; }
        float CF_pSatMin { get; }
        float CF_pBrightMax { get; }
        float CF_pBrightMin { get; }
        int CF_pRMax { get; }
        int CF_pRMin { get; }
        int CF_pGMax { get; }
        int CF_pGMin { get; }
        int CF_pBMax { get; }
        int CF_pBMin { get; }
        float CF_sHueMax { get; }
        float CF_sHueMin { get; }
        float CF_sSatMax { get; }
        float CF_sSatMin { get; }
        float CF_sBrightMax { get; }
        float CF_sBrightMin { get; }
        int CF_sRMax { get; }
        int CF_sRMin { get; }
        int CF_sGMax { get; }
        int CF_sGMin { get; }
        int CF_sBMax { get; }
        int CF_sBMin { get; }
        string Ignored { get; }
    }
}
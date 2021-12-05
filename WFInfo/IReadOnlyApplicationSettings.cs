using System.Windows;
using System.Windows.Input;
using Google.Apis.Sheets.v4;

namespace WFInfo
{
    public enum Display
    {
        Window,
        Overlay,
        Light
    }
    public interface IReadOnlyApplicationSettings
    {
        Display Display { get; }
        double MainWindowLocation_X { get; }
        double MainWindowLocation_Y { get; }
        Point MainWindowLocation { get; }
        bool IsOverlaySelected { get; }
        bool IsLightSelected { get; }
        string ActivationKey { get; }
        // Key ActivationKeyKey { get; }
        // MouseButton ActivationMouseButton { get; }
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
        int SnapItCountThreshold { get; }
        int SnapItEdgeWidth { get; }
        int SnapItEdgeRadius { get; }
        double SnapItHorizontalNameMargin { get; }
        bool DoCustomNumberBoxWidth { get; }
        double SnapItNumberBoxWidth { get; }
        bool SnapMultiThreaded { get; }
        double SnapRowTextDensity { get; }
        double SnapRowEmptyDensity { get; }
        double SnapColEmptyDensity { get; }
        string Ignored { get; }
    }
}
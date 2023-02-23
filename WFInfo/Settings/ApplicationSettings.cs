using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WFInfo.Settings
{
    /// <summary>
    /// Singleton class for storing and retrieving settings which can be serialized to a file.
    /// </summary>
    public class ApplicationSettings : IReadOnlyApplicationSettings
    {
        /// <summary>
        /// Global singleton access to readonly settings
        /// </summary>
        public static IReadOnlyApplicationSettings GlobalReadonlySettings => GlobalSettings;
        /// <summary>
        /// A singleton static instance of the settings class instead of injection
        /// </summary>
        internal static ApplicationSettings GlobalSettings { get; } = new ApplicationSettings();
        [JsonIgnore]
        public bool Initialized { get; set; } = false;
        public Display Display { get; set; } = Display.Overlay;
        [JsonProperty]
        public double MainWindowLocation_X { get; private set; } = 300;
        [JsonProperty]
        public double MainWindowLocation_Y { get; private set; } = 300;

        [JsonIgnore]
        public Point MainWindowLocation
        {
            get => new Point(MainWindowLocation_X, MainWindowLocation_Y);
            set
            {
                MainWindowLocation_X = value.X;
                MainWindowLocation_Y = value.Y;
            }
        }

        [JsonIgnore]
        public bool IsOverlaySelected => Display == Display.Overlay;
        [JsonIgnore]
        public bool IsLightSelected => Display == Display.Light;
        public string ActivationKey { get; set; } = "Snapshot";
        [JsonIgnore]
        public Key? ActivationKeyKey => Enum.TryParse<Key>(ActivationKey, out var res) ? res : (Key?)null;
        [JsonIgnore]
        public MouseButton? ActivationMouseButton => Enum.TryParse<MouseButton>(ActivationKey, out var res) ? res : (MouseButton?)null;
        public Key DebugModifierKey { get; set; } = Key.LeftShift;
        public Key SearchItModifierKey { get; set; } = Key.OemTilde;
        public Key SnapitModifierKey { get; set; } = Key.LeftCtrl;
        public Key MasterItModifierKey { get; set; } = Key.RightCtrl;
        public bool Debug { get; set; } = false;
        public string Locale { get; set; } = "en";
        public bool Clipboard { get; set; } = false;
        public long AutoDelay { get; set; } = 250L;
        public int ImageRetentionTime { get; set; } = 12;
        public string ClipboardTemplate { get; set; } = "-- PC 48 hours avg price by WFM (c) WFInfo";
        public bool SnapitExport { get; set; } = false;
        public int Delay { get; set; } = 10000;
        public bool HighlightRewards { get; set; } = true;
        public bool ClipboardVaulted { get; set; } = false;
        public bool Auto { get; set; } = false;
        public bool HighContrast { get; set; } = false;
        public int OverlayXOffsetValue { get; set; } = 0;
        public int OverlayYOffsetValue { get; set; } = 0;
        public bool AutoList { get; set; } = false;
        public bool AutoCSV { get; set; } = false;
        public bool AutoCount { get; set; } = false;
        public bool DoDoubleCheck { get; set; } = true;
        public double MaximumEfficiencyValue { get; set; } = 9.5;
        public double MinimumEfficiencyValue { get; set; } = 4.5;
        public bool DoSnapItCount { get; set; } = false;
        public int SnapItDelay { get; set; } = 20000;
        public double SnapItHorizontalNameMargin { get; set; } = 0;
        public bool DoCustomNumberBoxWidth { get; set; } = false;
        public double SnapItNumberBoxWidth { get; set; } = 0.4;
        public bool SnapMultiThreaded { get; set; } = true;
        public double SnapRowTextDensity { get; set; } = 0.015;
        public double SnapRowEmptyDensity { get; set; } = 0.01;
        public double SnapColEmptyDensity { get; set; } = 0.005;
        public int MinOverlayWidth { get; set; } = 120;
        public int MaxOverlayWidth { get; set; } = 160;

        public WFtheme ThemeSelection { get; set; } = WFtheme.AUTO;
        public bool CF_usePrimaryHSL { get; set; } = false;
        public bool CF_usePrimaryRGB { get; set; } = false;
        public bool CF_useSecondaryHSL { get; set; } = false;
        public bool CF_useSecondaryRGB { get; set; } = false;
        public float CF_pHueMax { get; set; } = 360.0F;
        public float CF_pHueMin { get; set; } = 0.0F;
        public float CF_pSatMax { get; set; } = 1.0F;
        public float CF_pSatMin { get; set; } = 0.0F;
        public float CF_pBrightMax { get; set; } = 1.0F;
        public float CF_pBrightMin { get; set; } = 0.0F;
        public int CF_pRMax { get; set; } = 255;
        public int CF_pRMin { get; set; } = 0;
        public int CF_pGMax { get; set; } = 255;
        public int CF_pGMin { get; set; } = 0;
        public int CF_pBMax { get; set; } = 255;
        public int CF_pBMin { get; set; } = 0;
        public float CF_sHueMax { get; set; } = 360.0F;
        public float CF_sHueMin { get; set; } = 0.0F;
        public float CF_sSatMax { get; set; } = 1.0F;
        public float CF_sSatMin { get; set; } = 0.0F;
        public float CF_sBrightMax { get; set; } = 1.0F;
        public float CF_sBrightMin { get; set; } = 0.0F;
        public int CF_sRMax { get; set; } = 255;
        public int CF_sRMin { get; set; } = 0;
        public int CF_sGMax { get; set; } = 255;
        public int CF_sGMin { get; set; } = 0;
        public int CF_sBMax { get; set; } = 255;
        public int CF_sBMin { get; set; } = 0;
        public string Ignored { get; set; } = null;
        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            Main.AddLog("Failed to parse settings file: " + errorContext.Error.Message);
            errorContext.Handled = true;
        }
 
    }
}

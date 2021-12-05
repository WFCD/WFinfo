using System.Windows.Input;

namespace WFInfo
{
    public class SettingsViewModel : INPC
    {
        private ApplicationSettings _settings;
        public Key DebugModifierKey
        {
            get => _settings.DebugModifierKey;
            set { 
                _settings.DebugModifierKey = value;
                RaisePropertyChanged(); 
            }
        }

        public Key SearchItModifierKey
        {
            get => _settings.SearchItModifierKey;
            set { 
                _settings.SearchItModifierKey = value;
                RaisePropertyChanged(); 
            }
        }

        public Key SnapitModifierKey
        {
            get => _settings.SnapitModifierKey;
            set { 
                _settings.SnapitModifierKey = value;
                RaisePropertyChanged(); 
            }
        }

        public Key MasterItModifierKey
        {
            get => _settings.MasterItModifierKey;
            set { 
                _settings.MasterItModifierKey = value;
                RaisePropertyChanged(); 
            }
        }

        public bool Debug
        {
            get => _settings.Debug;
            set { 
                _settings.Debug = value;
                RaisePropertyChanged(); 
            }
        }

        public string Locale
        {
            get => _settings.Locale;
            set { 
                _settings.Locale = value;
                RaisePropertyChanged(); 
            }
        }

        public bool Clipboard
        {
            get => _settings.Clipboard;
            set { 
                _settings.Clipboard = value;
                RaisePropertyChanged(); 
            }
        }

        public long AutoDelay
        {
            get => _settings.AutoDelay;
            set { 
                _settings.AutoDelay = value;
                RaisePropertyChanged(); 
            }
        }

        public int ImageRetentionTime
        {
            get => _settings.ImageRetentionTime;
            set { 
                _settings.ImageRetentionTime = value;
                RaisePropertyChanged(); 
            }
        }

        public string ClipboardTemplate
        {
            get => _settings.ClipboardTemplate;
            set { 
                _settings.ClipboardTemplate = value;
                RaisePropertyChanged(); 
            }
        }

        public bool SnapitExport
        {
            get => _settings.SnapitExport;
            set { 
                _settings.SnapitExport = value;
                RaisePropertyChanged(); 
            }
        }

        public int Delay
        {
            get => _settings.Delay;
            set { 
                _settings.Delay = value;
                RaisePropertyChanged(); 
            }
        }

        public bool HighlightRewards
        {
            get => _settings.HighlightRewards;
            set { 
                _settings.HighlightRewards = value;
                RaisePropertyChanged(); 
            }
        }

        public bool ClipboardVaulted
        {
            get => _settings.ClipboardVaulted;
            set { 
                _settings.ClipboardVaulted = value;
                RaisePropertyChanged(); 
            }
        }

        public bool Auto
        {
            get => _settings.Auto;
            set { 
                _settings.Auto = value;
                RaisePropertyChanged(); 
            }
        }

        public bool HighContrast
        {
            get => _settings.HighContrast;
            set { 
                _settings.HighContrast = value;
                RaisePropertyChanged(); 
            }
        }

        public int OverlayXOffsetValue
        {
            get => _settings.OverlayXOffsetValue;
            set { 
                _settings.OverlayXOffsetValue = value;
                RaisePropertyChanged(); 
            }
        }

        public int OverlayYOffsetValue
        {
            get => _settings.OverlayYOffsetValue;
            set { 
                _settings.OverlayYOffsetValue = value;
                RaisePropertyChanged(); 
            }
        }

        public bool AutoList
        {
            get => _settings.AutoList;
            set { 
                _settings.AutoList = value;
                RaisePropertyChanged(); 
            }
        }

        public bool DoDoubleCheck
        {
            get => _settings.DoDoubleCheck;
            set { 
                _settings.DoDoubleCheck = value;
                RaisePropertyChanged(); 
            }
        }

        public double MaximumEfficiencyValue
        {
            get => _settings.MaximumEfficiencyValue;
            set { 
                _settings.MaximumEfficiencyValue = value;
                RaisePropertyChanged(); 
            }
        }

        public double MinimumEfficiencyValue
        {
            get => _settings.MinimumEfficiencyValue;
            set { 
                _settings.MinimumEfficiencyValue = value;
                RaisePropertyChanged(); 
            }
        }

        public bool DoSnapItCount
        {
            get => _settings.DoSnapItCount;
            set { 
                _settings.DoSnapItCount = value;
                RaisePropertyChanged(); 
            }
        }

        public int SnapItCountThreshold
        {
            get => _settings.SnapItCountThreshold;
            set { 
                _settings.SnapItCountThreshold = value;
                RaisePropertyChanged(); 
            }
        }

        public int SnapItEdgeWidth
        {
            get => _settings.SnapItEdgeWidth;
            set { 
                _settings.SnapItEdgeWidth = value;
                RaisePropertyChanged(); 
            }
        }

        public int SnapItEdgeRadius
        {
            get => _settings.SnapItEdgeRadius;
            set { 
                _settings.SnapItEdgeRadius = value;
                RaisePropertyChanged(); 
            }
        }

        public double SnapItHorizontalNameMargin
        {
            get => _settings.SnapItHorizontalNameMargin;
            set { 
                _settings.SnapItHorizontalNameMargin = value;
                RaisePropertyChanged(); 
            }
        }

        public bool DoCustomNumberBoxWidth
        {
            get => _settings.DoCustomNumberBoxWidth;
            set { 
                _settings.DoCustomNumberBoxWidth = value;
                RaisePropertyChanged(); 
            }
        }

        public double SnapItNumberBoxWidth
        {
            get => _settings.SnapItNumberBoxWidth;
            set { 
                _settings.SnapItNumberBoxWidth = value;
                RaisePropertyChanged(); 
            }
        }

        public bool SnapMultiThreaded
        {
            get => _settings.SnapMultiThreaded;
            set { 
                _settings.SnapMultiThreaded = value;
                RaisePropertyChanged(); 
            }
        }

        public double SnapRowTextDensity
        {
            get => _settings.SnapRowTextDensity;
            set { 
                _settings.SnapRowTextDensity = value;
                RaisePropertyChanged(); 
            }
        }

        public double SnapRowEmptyDensity
        {
            get => _settings.SnapRowEmptyDensity;
            set { 
                _settings.SnapRowEmptyDensity = value;
                RaisePropertyChanged(); 
            }
        }

        public double SnapColEmptyDensity
        {
            get => _settings.SnapColEmptyDensity;
            set { 
                _settings.SnapColEmptyDensity = value;
                RaisePropertyChanged(); 
            }
        }

        public SettingsViewModel(ApplicationSettings settings)
        {
            _settings = settings;
        }

        public static SettingsViewModel Instance { get; }= new SettingsViewModel(new ApplicationSettings());
    }
}
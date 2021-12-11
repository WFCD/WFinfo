using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WFInfo
{
    public class SettingsViewModel : INPC, INotifyDataErrorInfo
    {
        private ApplicationSettings _settings;

        public Display Display
        {
            get => _settings.Display;
            set
            {
                _settings.Display = value;
                RaisePropertyChanged();
            }
        }
        
        public Point MainWindowLocation
        {
            get => _settings.MainWindowLocation;
            set
            {
                _settings.MainWindowLocation = value;
                RaisePropertyChanged();
            }
        }
        public string ActivationKey 
        {
            get => _settings.ActivationKey;
            set
            {
                _settings.ActivationKey = value;
                RaisePropertyChanged();
            }
        }
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
                if (value < _settings.MinimumEfficiencyValue)
                {
                    _validationErrors.Add(nameof(MaximumEfficiencyValue), "Maximum efficiency cannot be less than minimum efficiency");
                }
                else
                {
                    _settings.MaximumEfficiencyValue = value;
                    _validationErrors.Remove(nameof(MaximumEfficiencyValue));
                    RaisePropertyChanged();
                }
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(MaximumEfficiencyValue)));
            }
        }

        public double MinimumEfficiencyValue
        {
            get => _settings.MinimumEfficiencyValue;
            set { 
                if (value > _settings.MaximumEfficiencyValue)
                {
                    _validationErrors[nameof(MinimumEfficiencyValue)] = "Minimum efficiency cannot be greater than maximum efficiency";
                }
                else
                {
                    _settings.MinimumEfficiencyValue = value;
                    _validationErrors.Remove(nameof(MinimumEfficiencyValue));
                    RaisePropertyChanged(); 
                }
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(MaximumEfficiencyValue)));
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

        public string Ignored{
            get => _settings.Ignored;
            set { 
                _settings.Ignored = value;
                RaisePropertyChanged(); 
            }
        }
        
        private static readonly string settingsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json";  //change to WFInfo after release
        public SettingsViewModel(ApplicationSettings settings)
        {
            _settings = settings;
            this.PropertyChanged += (sender, args) => Save();
        }

        public void Save()
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            jsonSettings.Converters.Add(new StringEnumConverter());
            File.WriteAllText(settingsDirectory, JsonConvert.SerializeObject(ApplicationSettings.GlobalSettings, Formatting.Indented,jsonSettings));
        }

        public static SettingsViewModel Instance { get; }= new SettingsViewModel(ApplicationSettings.GlobalSettings);
        private readonly Dictionary<string, string> _validationErrors = new Dictionary<string, string>();
 
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        public bool HasErrors => _validationErrors.Count > 0;
        public IEnumerable GetErrors(string propertyName) =>
            _validationErrors.TryGetValue(propertyName, out string error) ? new string[1] { error } : null;

    }
}
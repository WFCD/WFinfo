using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace WFInfo
{
    public class AutoAddViewModel : INPC
    {
        private ObservableCollection<AutoAddSingleItem> _itemList;
        private double _totalPlatinum;
        private int _totalDucats;

        public ObservableCollection<AutoAddSingleItem> ItemList
        {
            get => _itemList;
            private set
            {
                _itemList = value;
                RaisePropertyChanged();
            }
        }

        public double TotalPlatinum
        {
            get => _totalPlatinum;
            private set
            {
                _totalPlatinum = value;
                RaisePropertyChanged();
            }
        }
        public int TotalDucats
        {
            get => _totalDucats;
            private set
            {
                _totalDucats = value;
                RaisePropertyChanged();
            }
        }

        public AutoAddViewModel()
        {
            _itemList = new ObservableCollection<AutoAddSingleItem>();
            _itemList.CollectionChanged += CollectionChanged;
        }

        public void addItem(AutoAddSingleItem item)
        {
            item.PropertyChanged += ItemChanged;
            _itemList.Add(item);
        }

        public void removeItem(AutoAddSingleItem item)
        {
            item.PropertyChanged -= ItemChanged;
            _itemList.Remove(item);
            RecalculateTotals();
        }

        private void ItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AutoAddSingleItem.PlatinumValue) || e.PropertyName == nameof(AutoAddSingleItem.DucatValue))
            {
                RecalculateTotals();
            }
        }

        private void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            TotalPlatinum = _itemList.Sum(item => item.PlatinumValue);
            TotalDucats = _itemList.Sum(item => item.DucatValue);
        }
    }

    public class AutoAddSingleItem : INPC
    {
        public AutoAddViewModel _parent;

        private const NumberStyles style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;
        private ObservableCollection<string> _rewardOptions;
        private string _activeOption;
        private double _platinumValue;
        private int _ducatValue;

        public ObservableCollection<string> RewardOptions
        {
            get => _rewardOptions;
            private set
            {
                _rewardOptions = value;
                RaisePropertyChanged();
            }
        }

        public string ActiveOption
        {
            get => _activeOption;
            set
            {
                if (_activeOption != value)
                {
                    _activeOption = value;
                    UpdateValues();
                    RaisePropertyChanged();
                }
            }
        }
        public double PlatinumValue
        {
            get => _platinumValue;
            private set
            {
                _platinumValue = value;
                RaisePropertyChanged();
            }
        }
        public int DucatValue
        {
            get => _ducatValue;
            private set
            {
                _ducatValue = value;
                RaisePropertyChanged();
            }
        }

        public SimpleCommand Increment { get; }

        public SimpleCommand Remove { get; }

        public AutoAddSingleItem(List<string> options, int activeIndex, AutoAddViewModel parent)
        {
            RewardOptions = new ObservableCollection<string>(options);
            activeIndex = Math.Min(RewardOptions.Count - 1, activeIndex);
            ActiveOption = activeIndex >= 0 ? options[activeIndex] : "";
            _parent = parent;
            Remove = new SimpleCommand(() => RemoveFromParent());
            Increment = new SimpleCommand(() => AddCount(true));
        }

        private void UpdateValues()
        {
            if (!string.IsNullOrEmpty(ActiveOption))
            {
                JObject job = (JObject)Main.dataBase.marketData.GetValue(ActiveOption);
                string plat = job["plat"].ToObject<string>();
                string ducats = job["ducats"].ToObject<string>();
                PlatinumValue = double.TryParse(plat, style, CultureInfo.InvariantCulture, out double platValue) ? platValue : 0;
                DucatValue = int.TryParse(ducats, style, CultureInfo.InvariantCulture, out int ducatValue) ? ducatValue : 0;
            }
            else
            {
                PlatinumValue = 0;
                DucatValue = 0;
            }
        }

        public void AddCount(bool save)
        {
            //get item count, increment, save
            bool saveFailed = false;
            string item = ActiveOption;
            if (item.Contains("Prime"))
            {
                string[] nameParts = item.Split(new string[] { "Prime" }, 2, StringSplitOptions.None);
                string primeName = nameParts[0] + "Prime";
                string partName = primeName + ((nameParts[1].Length > 10 && !nameParts[1].Contains("Kubrow")) ? nameParts[1].Replace(" Blueprint", "") : nameParts[1]);


                Main.AddLog("Incrementing owned amount for part \"" + partName + "\"");
                try
                {

                    int count = Main.dataBase.equipmentData[primeName]["parts"][partName]["owned"].ToObject<int>();

                    Main.dataBase.equipmentData[primeName]["parts"][partName]["owned"] = count + 1;
                }
                catch (Exception ex)
                {
                    Main.AddLog("FAILED to increment owned amount, Name: " + item + ", primeName: " + primeName + ", partName: " + partName + Environment.NewLine + ex.Message);
                    saveFailed = true;
                }
            }
            if (saveFailed)
            {
                //shouldn't need Main.RunOnUIThread since this is already on the UI Thread
                //adjust for time diff between snap-it finishing and save being pressed, in case of long delay
                Main.SpawnErrorPopup(DateTime.UtcNow);
                Main.StatusUpdate("Failed to save one or more item, report to dev", 2);
            }

            RemoveFromParent();
            if (save)
            {
                Main.dataBase.SaveAllJSONs();
                EquipmentWindow.INSTANCE.reloadItems();
            }
        }

        private void RemoveFromParent()
        {
            if (_parent != null)
            {
                _parent.removeItem(this);
            }
            RaisePropertyChanged();
        }
    }
}

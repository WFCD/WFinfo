using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace WFInfo
{
    public class AutoAddViewModel : INPC
    {
        private ObservableCollection<AutoAddSingleItem> _itemList;
        private double _totalPlatinum;
        private int _totalDucats;
        private const NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;

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
        }

        public void addItem(AutoAddSingleItem item)
        {
            _itemList.Add(item);
            UpdateTotalsForOptionChange(null, item.ActiveOption);
        }

        public void removeItem(AutoAddSingleItem item)
        {
            UpdateTotalsForOptionChange(item.ActiveOption, null);
            _itemList.Remove(item);
        }

        public void UpdateTotalsForOptionChange(string previousOption, string newOption)
        {
            if (!string.IsNullOrEmpty(previousOption))
            {
                JObject previousJob = (JObject)Main.dataBase.marketData.GetValue(previousOption);
                string previousPlat = previousJob["plat"].ToObject<string>();
                string previousDucats = previousJob["ducats"].ToObject<string>();
                if (double.TryParse(previousPlat, styles, CultureInfo.InvariantCulture, out double previousPlatValue))
                {
                    TotalPlatinum -= previousPlatValue;
                }
                if (int.TryParse(previousDucats, styles, CultureInfo.InvariantCulture, out int previousDucatValue))
                {
                    TotalDucats -= previousDucatValue;
                }
            }

            if (!string.IsNullOrEmpty(newOption))
            {
                JObject newJob = (JObject)Main.dataBase.marketData.GetValue(newOption);
                string newPlat = newJob["plat"].ToObject<string>();
                string newDucats = newJob["ducats"].ToObject<string>();
                if (double.TryParse(newPlat, styles, CultureInfo.InvariantCulture, out double newPlatValue))
                {
                    TotalPlatinum += newPlatValue;
                }
                if (int.TryParse(newDucats, styles, CultureInfo.InvariantCulture, out int newDucatValue))
                {
                    TotalDucats += newDucatValue;
                }
            }
        }
    }

    public class AutoAddSingleItem : INPC
    {
        public AutoAddViewModel _parent;

        private ObservableCollection<string> _rewardOptions;

        public ObservableCollection<string> RewardOptions
        {
            get => _rewardOptions;
            private set
            {
                _rewardOptions = value;
                RaisePropertyChanged();
            }
        }

        private string _activeOption;
        public string ActiveOption
        {
            get => _activeOption;
            set
            {
                // FIXME: breakpoint here doesn't always get hit (active option doesn't get changed). why?
                if (_activeOption != value)
                {
                    string previousOption = _activeOption;
                    _activeOption = value;
                    RaisePropertyChanged();
                    _parent?.UpdateTotalsForOptionChange(previousOption, _activeOption);
                }
            }
        }

        public SimpleCommand Increment { get; }

        public SimpleCommand Remove { get; }

        public AutoAddSingleItem(List<string> options, int activeIndex, AutoAddViewModel parent)
        {

            RewardOptions = new ObservableCollection<string>(options);
            activeIndex = Math.Min(RewardOptions.Count - 1, activeIndex);
            if (activeIndex >= 0 && options != null)
            {
                ActiveOption = options[activeIndex];
            }
            else
            {
                ActiveOption = "";
            }
            _parent = parent;
            Remove = new SimpleCommand(() => RemoveFromParent());
            Increment = new SimpleCommand(() => AddCount(true));
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WFInfo
{
    public class AutoAddViewModel : INPC
    {
        private ObservableCollection<AutoAddSingleItem> _itemList;

        public  ObservableCollection<AutoAddSingleItem> ItemList
        {
            get => _itemList;
            private set
            {
                _itemList = value;
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
            RaisePropertyChanged();
        }

        public void removeItem(AutoAddSingleItem item)
        {
            _itemList.Remove(item);
            RaisePropertyChanged();
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
                _activeOption = value;
                RaisePropertyChanged();
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
            } else
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

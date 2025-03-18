using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;

namespace cFollower
{
    internal class cFollowerSettings : JsonSettings
    {
        private static cFollowerSettings _instance;
        public static cFollowerSettings Instance => _instance ?? (_instance = new cFollowerSettings());
        private cFollowerSettings() : base(GetSettingsFilePath(Configuration.Instance.Name, "cFollowerSettings.json"))
        {
            if (ItemFilterList == null)
            {
                ItemFilterList = SetupDefaultItems();
            }
                
        }
        private string _leaderName;
        private int _minDistance;
        private int _transitionCheckDistance;
        private bool _lootEnabled;
        private bool _depositEnabled;
        private bool _tradeEnabled;
        private int _distanceToLeaderLoot;
        private int _distanceLootLeader;
        private float _distanceToLootPlayer;
        private ObservableCollection<ItemFilter> _itemFilterList;
        private string _depositTabName;
        private int _stashDepositDelay;
        private int _guildStashDepositDelay;
        private int _tradeDepositDelay;
        private int _obstacleSizeMultiplier;
        public ObservableCollection<ItemFilter> ItemFilterList
        {
            get => _itemFilterList;//?? (_defensiveSkills = new ObservableCollection<DefensiveSkillsClass>());
            set
            {
                _itemFilterList = value;
                NotifyPropertyChanged(() => ItemFilterList);
            }
        }
        public string DepositTabNames
        {
            get { return _depositTabName; }
            set
            {
                if (value == _depositTabName) return;
                _depositTabName = value;
                NotifyPropertyChanged(() => DepositTabNames);
            }
        }
        public string LeaderName
        {
            get { return _leaderName; }
            set
            {
                if (value == _leaderName) return;
                _leaderName = value;
                NotifyPropertyChanged(() => LeaderName);
            }
        }
        [DefaultValue(true)]
        public bool LootEnabled
        {
            get { return _lootEnabled; }
            set
            {
                if (value == _lootEnabled) return;
                _lootEnabled = value;
                NotifyPropertyChanged(() => LootEnabled);
            }
        }
        [DefaultValue(true)]
        public bool DepositEnabled
        {
            get { return _depositEnabled; }
            set
            {
                if (value == _depositEnabled) return;
                _depositEnabled = value;
                NotifyPropertyChanged(() => DepositEnabled);
            }
        }
        [DefaultValue(true)]
        public bool TradeEnabled
        {
            get { return _tradeEnabled; }
            set
            {
                if (value == _tradeEnabled) return;
                _tradeEnabled = value;
                NotifyPropertyChanged(() => TradeEnabled);
            }
        }
        public int MinDistanceToFollow
        {
            get { return _minDistance; }
            set
            {
                if (value == _minDistance) return;
                _minDistance = value;
                NotifyPropertyChanged(() => MinDistanceToFollow);
            }
        }
        [DefaultValue(2)]
        public int ObstacleSizeMultiplier
        {
            get { return _obstacleSizeMultiplier; }
            set
            {
                if (value == _obstacleSizeMultiplier) return;
                _obstacleSizeMultiplier = value;
                NotifyPropertyChanged(() => ObstacleSizeMultiplier);
            }
        }
        public int DistanceToCheckTransition
        {
            get { return _transitionCheckDistance; }
            set
            {
                if (value == _transitionCheckDistance) return;
                _transitionCheckDistance = value;
                NotifyPropertyChanged(() => DistanceToCheckTransition);
            }
        }
        public int DistanceToLeaderLoot
        {
            get { return _distanceToLeaderLoot; }
            set
            {
                if (value == _distanceToLeaderLoot) return;
                _distanceToLeaderLoot = value;
                NotifyPropertyChanged(() => DistanceToLeaderLoot);
            }
        }
        [DefaultValue(50)]
        public int StashDepositDelay
        {
            get { return _stashDepositDelay; }
            set
            {
                if (value == _stashDepositDelay) return;
                _stashDepositDelay = value;
                NotifyPropertyChanged(() => StashDepositDelay);
            }
        }
        [DefaultValue(200)]
        public int GuildStashDepositDelay
        {
            get { return _guildStashDepositDelay; }
            set
            {
                if (value == _guildStashDepositDelay) return;
                _guildStashDepositDelay = value;
                NotifyPropertyChanged(() => GuildStashDepositDelay);
            }
        }
        [DefaultValue(50)]
        public int TradeDepositDelay
        {
            get { return _tradeDepositDelay; }
            set
            {
                if (value == _tradeDepositDelay) return;
                _tradeDepositDelay = value;
                NotifyPropertyChanged(() => TradeDepositDelay);
            }
        }
        public int DistanceToLootLeader
        {
            get { return _distanceLootLeader; }
            set
            {
                if (value == _distanceLootLeader) return;
                _distanceLootLeader = value;
                NotifyPropertyChanged(() => DistanceToLootLeader);
            }
        }

        public float DistanceToLootPlayer
        {
            get { return _distanceToLootPlayer; }
            set
            {
                if (value == _distanceToLootPlayer) return;
                _distanceToLootPlayer = value;
                NotifyPropertyChanged(() => DistanceToLootPlayer);
            }
        }

        private ObservableCollection<ItemFilter> SetupDefaultItems()
        {
            ObservableCollection<ItemFilter> items = new ObservableCollection<ItemFilter>();

            items.Add(new ItemFilter(true, "Headhunter", "Art/2DItems/Belts/Headhunter"));
            items.Add(new ItemFilter(true, "Mageblood", "Art/2DItems/Belts/InjectorBelt"));
            items.Add(new ItemFilter(true, "Mirror", "Art/2DItems/Currency/CurrencyDuplicate"));
            return items;
        }


    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                ItemFilterList = new ObservableCollection<ItemFilter>() { new ItemFilter(true, "Headhunter", "Art/2DItems/Belts/Headhunter") };
            }
        }

        #region TaskSettings

        private bool _entityScanTaskToggle;
        private bool _handlePartyTaskToggle;
        private bool _resurrectionTaskToggle;
        private bool _combatTaskToggle;
        private bool _handleAreaTaskToggle;
        private bool _tradeTaskToggle;
        private bool _depositTaskToggle;
        private bool _lootTaskToggle;
        private bool _followTaskToggle;

        [DefaultValue(true)]
        public bool EntityScanTaskToggle
        {
            get { return _entityScanTaskToggle; }
            set
            {
                if (value == _entityScanTaskToggle) return;
                _entityScanTaskToggle = value;
                NotifyPropertyChanged(() => EntityScanTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool HandlePartyTaskToggle
        {
            get { return _handlePartyTaskToggle; }
            set
            {
                if (value == _handlePartyTaskToggle) return;
                _handlePartyTaskToggle = value;
                NotifyPropertyChanged(() => HandlePartyTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool ResurrectionTaskToggle
        {
            get { return _resurrectionTaskToggle; }
            set
            {
                if (value == _resurrectionTaskToggle) return;
                _resurrectionTaskToggle = value;
                NotifyPropertyChanged(() => ResurrectionTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool HandleAreaTaskToggle
        {
            get { return _handleAreaTaskToggle; }
            set
            {
                if (value == _handleAreaTaskToggle) return;
                _handleAreaTaskToggle = value;
                NotifyPropertyChanged(() => HandleAreaTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool CombatTaskToggle
        {
            get { return _combatTaskToggle; }
            set
            {
                if (value == _combatTaskToggle) return;
                _combatTaskToggle = value;
                NotifyPropertyChanged(() => CombatTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool TradeTaskToggle
        {
            get { return _tradeTaskToggle; }
            set
            {
                if (value == _tradeTaskToggle) return;
                _tradeTaskToggle = value;
                NotifyPropertyChanged(() => TradeTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool DepositTaskToggle
        {
            get { return _depositTaskToggle; }
            set
            {
                if (value == _depositTaskToggle) return;
                _depositTaskToggle = value;
                NotifyPropertyChanged(() => DepositTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool LootTaskToggle
        {
            get { return _lootTaskToggle; }
            set
            {
                if (value == _lootTaskToggle) return;
                _lootTaskToggle = value;
                NotifyPropertyChanged(() => LootTaskToggle);
            }
        }

        [DefaultValue(true)]
        public bool FollowTaskToggle
        {
            get { return _followTaskToggle; }
            set
            {
                if (value == _followTaskToggle) return;
                _followTaskToggle = value;
                NotifyPropertyChanged(() => FollowTaskToggle);
            }
        }


        #endregion

        #region GeneralSettings

        private int _obstacleSizeMultiplier;
        private int _entityScanRate;

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

        [DefaultValue(80)]
        public int EntityScanRate
        {
            get { return _entityScanRate; }
            set
            {
                if (value == _entityScanRate) return;
                _entityScanRate = value;
                NotifyPropertyChanged(() => EntityScanRate);
            }
        }

        #endregion GeneralSettings

        #region FollowerSettings

        private MoveType _moveType;
        private string _leaderName;
        private int _minDistanceToFollow;
        private int _distanceToCheckTransition;

        [DefaultValue(MoveType.ToLeader)]
        public MoveType FollowType
        {
            get { return _moveType; }
            set
            {
                if (value == _moveType) return;
                _moveType = value;
                NotifyPropertyChanged(() => FollowType);
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

        public int MinDistanceToFollow
        {
            get { return _minDistanceToFollow; }
            set
            {
                if (value == _minDistanceToFollow) return;
                _minDistanceToFollow = value;
                NotifyPropertyChanged(() => MinDistanceToFollow);
            }
        }

        public int DistanceToCheckTransition
        {
            get { return _distanceToCheckTransition; }
            set
            {
                if (value == _distanceToCheckTransition) return;
                _distanceToCheckTransition = value;
                NotifyPropertyChanged(() => DistanceToCheckTransition);
            }
        }

        #endregion FollowerSettings

        #region ToggleSettings

        private bool _lootEnabled;
        private bool _depositEnabled;
        private bool _tradeEnabled;

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

        #endregion ToggleSettings

        #region TradeSettings

        private int _stashDepositDelay;
        private int _guildStashDepositDelay;
        private int _tradeDepositDelay;

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

        #endregion TradeSettings

        #region LootSettings

        private ObservableCollection<ItemFilter> _itemFilterList;
        private string _depositTabName;
        private int _distanceToLeaderLoot;
        private int _radiusLeaderLoot;
        private int _radiusPlayerLoot;

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
        [DefaultValue(55)]
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
        [DefaultValue(40)]
        public int RadiusLeaderLoot
        {
            get { return _radiusLeaderLoot; }
            set
            {
                if (value == _radiusLeaderLoot) return;
                _radiusLeaderLoot = value;
                NotifyPropertyChanged(() => RadiusLeaderLoot);
            }
        }

        [DefaultValue(40)]
        public int RadiusPlayerLoot
        {
            get { return _radiusPlayerLoot; }
            set
            {
                if (value == _radiusPlayerLoot) return;
                _radiusPlayerLoot = value;
                NotifyPropertyChanged(() => RadiusPlayerLoot);
            }
        }

        #endregion LootSettings

        public static List<MoveType> MoveTypeOptions = new List<MoveType>
            {
                MoveType.ToLeader,
                MoveType.ToCursor
            };

        public enum MoveType
        {
            ToCursor,
            ToLeader
        }
    }
}
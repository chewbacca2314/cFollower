using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DPBDevHelper;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace cFollower
{
    public class EntityScanTask : ITask
    {
        private Stopwatch swScan = Stopwatch.StartNew();

        private HashSet<PlayerInfo> playerList = new HashSet<PlayerInfo>(new PlayerComparer());
        private HashSet<WorldItem> worldItemList = new HashSet<WorldItem>();
        private HashSet<Monster> monsterList = new HashSet<Monster>();

        public void Tick()
        {
            NotifyOnZoneChange();

            if (swScan.ElapsedMilliseconds > cFollowerSettings.Instance.EntityScanRate)
            {
                HashSet<NetworkObject> networkObjectList = new HashSet<NetworkObject>(LokiPoe.ObjectManager.Objects);

                bool wiFlag = false;
                bool playerFlag = false;
                bool monsterFlag = false;

                foreach (NetworkObject networkObject in networkObjectList)
                {
                    if (!ValidateObject(networkObject))
                    {
                        RemoveInvalidObject(networkObject);
                        continue;
                    }

                    if (WorldItemProcess(networkObject, ref wiFlag))
                    {
                        continue;
                    }

                    if (PlayerProcess(networkObject, ref playerFlag))
                    {
                        continue;
                    }

                    if (MonsterProcess(networkObject, ref monsterFlag))
                    {
                        continue;
                    }
                }

                foreach (WorldItem item in new HashSet<WorldItem>(worldItemList))
                {
                    RemoveInvalidObject(item);
                }

                foreach (PlayerInfo player in new HashSet<PlayerInfo>(playerList))
                {
                    //Log.Debug($"[EntityScantTask] Validating player with id {player.PlayerId}, " +
                    //    $"entity is {player?.PlayerEntity.Id}, position is {player?.PlayerEntity?.Position}" +
                    //    $"name: {player?.PlayerEntity?.Name}, " +
                    //    $"IsLeader: {player.IsLeader}, " +
                    //    $"count: {playerList.Count}");
                    RemoveInvalidObject(player.PlayerEntity);
                }

                foreach (Monster monster in new HashSet<Monster>(monsterList))
                {
                    RemoveInvalidObject(monster);
                }

                if (wiFlag)
                {
                    Message msg = DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.WorldItemListUpdate.ToString(), worldItemList);
                }

                if (playerFlag)
                {
                    Message msg = DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.PlayerListUpdate.ToString(), playerList);
                }

                if (monsterFlag)
                {
                    Message msg = DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.PlayerListUpdate.ToString(), playerList);
                }

                swScan.Restart();
            }
        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                return true;
            }

            PlayerInfo leader = playerList.FirstOrDefault(x => x.IsLeader);
            if (leader != null && leader?.PlayerPartyMember != null && !leader.PlayerPartyMember.PlayerEntry.IsOnline)
            {
                return true;
            }

            return await Task.FromResult(false);
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Enums.MessageType.ZoneChange.ToString())
            {
                ClearLists();
            }
            return MessageResult.Unprocessed;
        }

        private void NotifyOnZoneChange()
        {
            if (LokiPoe.StateManager.IsAreaLoadingStateActive)
            {
                Message msg = DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.ZoneChange.ToString());
                Log.Warn($"[EntityScanTask] Sent message with zone change. Message id: {msg.Id}");
            }
        }

        private bool WorldItemProcess(NetworkObject networkObject, ref bool flag)
        {
            if (networkObject is WorldItem)
            {
                if (worldItemList.Add(networkObject as WorldItem))
                {
                    flag = true;
                };

                return true;
            }

            return false;
        }

        private bool PlayerProcess(NetworkObject networkObject, ref bool flag)
        {
            if (networkObject is Player)
            {
                Player player = networkObject as Player;
                if (player.Name != LokiPoe.Me.Name)
                {
                    PlayerInfo playerInput = new PlayerInfo(networkObject as Player);
                    if (playerList.Add(playerInput))
                    {
                        playerInput.Update();
                        flag = true;
                    };
                }

                return true;
            }

            return false;
        }

        private bool MonsterProcess(NetworkObject networkObject, ref bool flag)
        {
            if (networkObject is Monster)
            {
                if (monsterList.Add(networkObject as Monster))
                {
                    flag = true;
                };

                return true;
            }

            return false;
        }

        private void RemoveInvalidObject(NetworkObject networkObject)
        {
            if (!ValidateObject(networkObject))
            {
                if (worldItemList.Remove(networkObject as WorldItem))
                {
                    DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.WorldItemListUpdate.ToString(), worldItemList);
                };

                if (networkObject is Player)
                {
                    if (playerList.RemoveWhere(x => x.PlayerId == networkObject.Id) > 0)
                    {
                        DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.PlayerListUpdate.ToString(), playerList);
                    }
                };

                if (monsterList.Remove(networkObject as Monster))
                {
                    DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.MonsterListUpdate.ToString(), monsterList);
                }
            }
        }

        private bool ValidateObject(NetworkObject networkObject)
        {
            if (networkObject == null || !networkObject.IsValid)
            {
                return false;
            }

            return true;
        }

        private void ClearLists()
        {
            worldItemList.Clear();
            playerList.Clear();
            monsterList.Clear();
        }

        #region skip

        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await Task.FromResult(LogicResult.Unprovided);
        }

        public string Name => "EntityScanTask";
        public string Description => "template desc";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";

        public void Start()
        {
            Log.Info($"[EntityScanTask] Task Loaded.");
        }

        public void Stop()
        {
        }

        #endregion skip
    }

    public class PlayerInfo
    {
        public PlayerInfo(Player playerEntity)
        {
            PlayerEntity = playerEntity;
            PlayerId = PlayerEntity.Id;
            IsLeader = DetermineLeader(PlayerEntity.Name);
        }

        public Player PlayerEntity { get; set; }
        public PartyMember PlayerPartyMember { get; set; }
        public int PlayerId { get; set; }
        public bool IsLeader { get; set; }

        public void Update()
        {
            PlayerPartyMember = DPBDevHelper.PartyHelper.GetPlayerPartyMember(PlayerEntity?.Name);
            IsLeader = DetermineLeader(PlayerEntity.Name);
        }

        private bool DetermineLeader(string name)
        {
            return name == cFollowerSettings.Instance.LeaderName;
        }
    }

    public class PlayerComparer : IEqualityComparer<PlayerInfo>
    {
        public bool Equals(PlayerInfo x, PlayerInfo y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.PlayerId == y.PlayerId;
        }

        public int GetHashCode(PlayerInfo obj)
        {
            if (obj is null) return 0;
            return obj.PlayerId.GetHashCode();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Controllers;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Elements;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using log4net;

namespace cFollower
{
    public class LootTask : ITask
    {
        private float distanceToLeader;
        private Player leader;
        private Stopwatch lootSw = Stopwatch.StartNew();
        private HashSet<int> trashItems = new HashSet<int>();
        private HashSet<WorldItem> lootTable = new HashSet<WorldItem>();
        private HashSet<WorldItem> worldItemList = new HashSet<WorldItem>();

        public void Tick()
        {
        }

        public async Task<bool> Run()
        {
            if (!cFollowerSettings.Instance.LootEnabled)
                return false;

            if (!LokiPoe.IsInGame)
            {
                //Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }
            
            if (leader == null || worldItemList == null || worldItemList.Count == 0)
            {
                return false;
            }

            //if (leader.Distance > cFollowerSettings.Instance.DistanceToLeaderLoot)
            //{
            //    Log.Debug($"[{Name}] {leader.Distance} > {cFollowerSettings.Instance.DistanceToLeaderLoot}");
            //    return false;
            //}

            if (lootSw.IsRunning && lootSw.ElapsedMilliseconds > cFollowerSettings.Instance.EntityScanRate)
            {
                lootTable = GetNearbyItems();
                lootSw.Restart();
            }

            Log.Debug($"[{Name}] Found {lootTable.Count} items to loot");
            if (lootTable.Count == 0)
            {
                return false;
            }

            var invControl = LokiPoe.InGameState.InventoryUi.InventoryControl_Main;
            foreach (var item in lootTable)
            {
                if (leader.Distance > cFollowerSettings.Instance.DistanceToLeaderLoot)
                {
                    //Log.Debug($"[{Name}] {leader.Distance} > {cFollowerSettings.Instance.DistanceToLeaderLoot}");
                    return false;
                }

                var itemPos = item.Position;
                if (LokiPoe.MyPosition.Distance(itemPos) > cFollowerSettings.Instance.RadiusPlayerLoot
                    || leader.Position.Distance(itemPos) > cFollowerSettings.Instance.RadiusLeaderLoot)
                {
                    continue;
                }

                if (!item.HasVisibleHighlightLabel)
                {
                    continue;
                }

                if (invControl.Inventory.CanFitItem(item.Item))
                {
                    if (item.IsHighlightable && await Coroutines.InteractWith(item))
                    {
                        Log.Debug($"[{Name}] Item {item.Name} looted");
                    }
                };
            }

            return false;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == DPBDevHelper.Enums.MessageType.PlayerListUpdate.ToString())
            {
                HashSet<PlayerInfo> playerList = new HashSet<PlayerInfo>();
                if (message.TryGetInput<HashSet<PlayerInfo>>(0, out playerList))
                {
                    if (playerList?.Count > 0)
                    {
                        leader = playerList.FirstOrDefault(x => x.IsLeader).PlayerEntity;
                    }
                    //Log.Debug($"[CombatTask] Monster list processed, count {monsterList.Count}");
                    return MessageResult.Processed;
                };
            }

            if (message.Id == DPBDevHelper.Enums.MessageType.WorldItemListUpdate.ToString())
            {
                if (message.TryGetInput<HashSet<WorldItem>>(0, out worldItemList))
                {
                    //Log.Debug($"[CombatTask] Monster list processed, count {monsterList.Count}");
                    return MessageResult.Processed;
                };
            }
            return MessageResult.Processed;
        }

        public HashSet<WorldItem> GetNearbyItems()
        {
            //var metadataObjects = LokiPoe.ObjectManager.GetObjectsByMetadata("123");
            HashSet<WorldItem> resultWorldItems = new HashSet<WorldItem>();

            foreach (var wi in worldItemList)
            {
                if (wi == null
                    || !wi.IsValid
                    || trashItems.Contains(wi.Id)
                    || !wi.HasVisibleHighlightLabel)
                {
                    continue;
                }

                if (wi.IsAllocatedToOther
                    || !IsItemToLoot(wi))
                {
                    trashItems.Add(wi.Id);
                    continue;
                }

                var wiPos = wi.Position;

                if (LokiPoe.MyPosition.Distance(wiPos) > cFollowerSettings.Instance.RadiusPlayerLoot
                    || leader.Position.Distance(wiPos) > cFollowerSettings.Instance.RadiusLeaderLoot)
                {
                    continue;
                }

                resultWorldItems.Add(wi);
            }

            return resultWorldItems;
        }

        public bool IsItemToLoot(WorldItem wi)
        {
            if (wi == null)
            {
                return false;
            }

            if (wi.Item.Rarity != DreamPoeBot.Loki.Game.GameData.Rarity.Unique)
            {
                return true;
            }

            string renderArt = wi.Item.RenderArt;

            return cFollowerSettings.Instance.ItemFilterList.Any(x => renderArt.ToLower().Contains(x.RenderItem.ToLower()));
        }

        #region skip

        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Name => "Loot task";
        public string Description => "Task to handle looting";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";

        public void Start()
        {
            Log.Info($"[{Name}] Task Loaded.");
        }

        public void Stop()
        {
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await Task.FromResult(LogicResult.Unprovided);
        }

        #endregion skip
    }
}
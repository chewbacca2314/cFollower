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

        public async Task<bool> Run()
        {
            if (!cFollowerSettings.Instance.LootEnabled)
                return false;

            if (!LokiPoe.IsInGame)
            {
                Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            leader = Utility.GetLeaderPlayer();
            distanceToLeader = leader.Distance;
            if (distanceToLeader > cFollowerSettings.Instance.DistanceToLeaderLoot)
            {
                Log.Debug($"[{Name}] Distance to leader {distanceToLeader}. Skip");
                return false;
            }

            Log.Debug($"[{Name}] Building loot table");
            var lootTable = BuildLootTable(GetNearbyItemsLabels());

            Log.Debug($"[{Name}] Found {lootTable.Count} items to loot");
            if (lootTable.Count <= 0)
            {
                return false;
            }

            foreach (var item in lootTable)
            {
                var invControl = TradeHelper.GetInventoryControl();
                
                if (invControl.Inventory.CanFitItem(item.Item) && await Coroutines.InteractWith(item))
                {
                    Log.Debug($"[{Name}] Item {item.Name} looted");
                };
            }

            return true;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Processed;
        }

        public List<ItemsOnGroundLabelElement> GetNearbyItemsLabels()
        {
            //var metadataObjects = LokiPoe.ObjectManager.GetObjectsByMetadata("123");
            var labels = GameController.Instance.Game.IngameState.IngameUi.ItemsOnGroundLabels.ToList();
            List<ItemsOnGroundLabelElement> resultLabels = new List<ItemsOnGroundLabelElement>();

            foreach (var label in labels)
            {
                if (label == null
                    || !label.IsVisible)
                {

                    //Log.Debug($"Skipping label {label.ItemOnGround.Name} IsVisible {label.IsVisible} CanPickup {label.CanPickUp}");
                    continue;
                }

                resultLabels.Add(label);
            }

            return resultLabels;
        }

        public List<WorldItem> BuildLootTable(List<ItemsOnGroundLabelElement> labels)
        {
            var result = new List<WorldItem>();
            if (labels.Count == 0)
            {
                Log.Warn($"labels.Count {labels.Count}");
                return result;
            }
                

            foreach (var label in labels)
            {
                var res = IsLabelToLoot(label);
                //Log.Debug($"[BuildLootTable] Checking if we need to loot label {label.Label.Text}. Result: {res}");
                if (res)
                {
                    NetworkObject no = LokiPoe.ObjectManager.GetObjectById(label.ItemOnGround.Id);
                    WorldItem wi = CastNetworkToWorld(no);
                    result.Add(wi);
                }
            }

            return result;
        }

        public bool IsLabelToLoot(ItemsOnGroundLabelElement label)
        {
            NetworkObject no = LokiPoe.ObjectManager.GetObjectById(label.ItemOnGround.Id);
            WorldItem wi = CastNetworkToWorld(no);
            
            if (wi == null)
            {
                //Log.Warn($"[IsLabelToLoot] WorldItem is null for {label.Label.Text}");
                return false;
            }

            string resourcePath = wi.Item.Components.RenderItemComponent.ResourcePath;

            if (!(wi.Position.Distance(leader.Position) < cFollowerSettings.Instance.DistanceToLootLeader || wi.Position.Distance(LokiPoe.Me.Position) < cFollowerSettings.Instance.DistanceToLootPlayer))
            {
                Log.Warn($"{wi.Position.Distance(leader.Position)} < {cFollowerSettings.Instance.DistanceToLootLeader} || {wi.Position.Distance(LokiPoe.Me.Position)} < {cFollowerSettings.Instance.DistanceToLootPlayer}");
                return false;
            }

            foreach (var x in cFollowerSettings.Instance.ItemFilterList)
            {
                Log.Debug($"[IsLabelToLoot] Enabled {x.Enabled} Name {x.Name} Icon {x.RenderItem}");
            }

            //Log.Debug($"[IsLabelToLoot] {cFollowerSettings.Instance.ItemFilterList.Any(x => x.RenderItem + "dds" == resourcePath)} {resourcePath}");

            return cFollowerSettings.Instance.ItemFilterList.Any(x => resourcePath.ToLower().Contains(x.RenderItem.ToLower()));
        }

        public WorldItem CastNetworkToWorld(NetworkObject no)
        {
            Log.Debug($"[CastNetworkToWorld] {no.Type}");

            if (no.Type.Contains("WorldItem"))
            {
                return (WorldItem)no;
            }

            return null;
        }
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

        public void Tick()
        {
        }
    }
}
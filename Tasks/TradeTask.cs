using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Components;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using log4net;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace TestPlugin
{
    public class TradeTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private TradeControlWrapper tradeControl;

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (LokiPoe.Me.PartyStatus != PartyStatus.PartyMember)
                return false;

            if (!LokiPoe.InGameState.NotificationHud.IsOpened && !LokiPoe.InGameState.TradeUi.IsOpened)
                return false;

            LokiPoe.InGameState.NotificationHud.HandleNotificationEx(IsTradeRequestToBeAccepted);

            await Wait.Sleep(200);

            if (!LokiPoe.InGameState.TradeUi.IsOpened)
            {
                Log.Warn($"[{Name}] Trade UI not opened");
                return false;
            }

            tradeControl = LokiPoe.InGameState.TradeUi.TradeControl;

            if (tradeControl == null)
            {
                Log.Warn($"[{Name}] Trade window ERROR");
                return false;
            }

            var currentArea = LokiPoe.LocalData.WorldArea;
            var otherOffer = tradeControl.InventoryControl_OtherOffer;
            if (currentArea.IsCombatArea)
            {
                Log.Debug($"[{Name}] Start trading in combat area");
                await ProcessOtherOffer();
                await ProcessAcceptButton();
            }
            else if (currentArea.IsHideoutArea || currentArea.IsTown)
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                var inventoryControl = GetInventoryControl();

                if (inventoryControl == null)
                    Log.Debug($"[{Name}] Inventory control is null");

                Log.Debug("Items" + inventory.Items.Count);

                if (inventory.Items.Count > 0)
                {
                    await MoveAllFromInventory(inventory, inventoryControl, MoveType.Trade);
                }

                await ProcessOtherOffer();
                await ProcessAcceptButton();
            }

            return true;
        }

        public static InventoryControlWrapper GetInventoryControl()
        {
            var invControls = LokiPoe.InGameState.InventoryUi.AllInventoryControls;
            foreach (var control in invControls)
            {
                if (control.Inventory.PageSlot == InventorySlot.Main)
                {
                    return control;
                }
            }
            return null;
        }

        public InventoryControlWrapper GetGuildInventoryControl()
        {
            InventoryControlWrapper inventoryControl = null;

            if (LokiPoe.InGameState.GuildStashUi.IsOpened)
            {
                Log.Debug($"[{Name}] Guild stash UI is opened");
                inventoryControl = LokiPoe.InGameState.GuildStashUi.InventoryControl;
            }


            return inventoryControl;
        }

        public static void RemoveDuplicateCurrency(List<Item> list)
        {
            HashSet<string> seenSortableItems = new HashSet<string>();

            // Iterate backward to avoid index issues
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Metadata.Contains("Currency") || list[i].Metadata.Contains("MapFragment") || list[i].Metadata.Contains("DivinationCard")) // Check if the number is even
                {
                    if (seenSortableItems.Contains(list[i].Metadata))
                    {
                        // If the even number has already been seen, remove it
                        list.RemoveAt(i);
                    }
                    else
                    {
                        // Otherwise, mark it as seen
                        seenSortableItems.Add(list[i].Metadata);
                    }
                }
                // Odd numbers are left untouched
            }

            list.Sort((p1, p2) => p2.Size.Y.CompareTo(p1.Size.Y));
        }

        public static async Task MoveAllFromInventory(Inventory inventory, InventoryControlWrapper inventoryControl, MoveType moveType)
        {
            int sleepMS = 0;
            switch (moveType)
            {
                case MoveType.Stash:
                    {
                        sleepMS = 50;
                        break;
                    }
                case MoveType.GuildStash:
                    {
                        sleepMS = 200;
                        break;
                    }
                case MoveType.Trade:
                    {
                        sleepMS = 50;
                        break;
                    }
                default:
                    break;
            }

            var inventoryItems = inventory.Items;
            Log.Debug($"[MoveAllFromInventory] Inventory found: {inventory.IsValid} Inventory Control found: {inventoryControl.IsValid} Inventory items count: {inventoryItems.Count}");
            
            if (moveType == MoveType.Stash || moveType == MoveType.Trade)
            {
                Log.Debug($"[MoveAllFromInventory] Removing dupliactes b/c it's {moveType} trade");
                RemoveDuplicateCurrency(inventoryItems);
                Log.Debug($"[MoveAllFromInventory] Inventory items after cleaning count: {inventoryItems.Count}");
            }

            while ((moveType == MoveType.Trade && inventoryItems.Any(x => !inventoryControl.IsItemTransparent(x.LocalId))) 
                || (moveType != MoveType.Trade && inventoryItems.Where(x => x.Rarity != Rarity.Quest).ToList().Count > 0))
            {
                Log.Debug($"[MoveAllFromInventory] Entered while cycle");
                foreach (var item in inventoryItems)
                {
                    Log.Debug($"[MoveAllFromInventory] Found item {item.Name} IsItemTransparent {inventoryControl.IsItemTransparent(item.LocalId)}");
                    if (moveType == MoveType.Trade && !inventoryControl.IsItemTransparent(item.LocalId) 
                        || moveType == MoveType.Stash 
                        || moveType == MoveType.GuildStash)
                    {
                        await DepositTask.SelectProperTab(item);
                        if (moveType != MoveType.GuildStash && ((item.Metadata.Contains("StackableCurrency") || item.Metadata.Contains("MapFragment") || item.Metadata.Contains("DivinationCard"))))
                        {
                            Log.Debug($"[MoveAllFromInventory] Moving currency item: {item.FullName}");
                            await Wait.For(() => inventoryControl.FastMoveAll(item.LocalId) == FastMoveResult.None || item == null, "fastmove", 50, 150);
                        }
                        else
                        {
                            Log.Debug($"[MoveAllFromInventory] Moving normal item: {item.FullName}");
                            await Wait.For(() => inventoryControl.FastMove(item.LocalId) == FastMoveResult.None || item == null, "fastmove", 50, 150);
                        }
                    }

                    await Wait.Sleep(sleepMS);

                    if (inventory.Items.Count <= 0)
                    {
                        Log.Debug($"[MoveAllFromInventory] {inventory.Items.Count}");
                        break;
                    }
                }

                if (inventory.Items.Count <= 0)
                {
                    Log.Debug($"[MoveAllFromInventory] {inventory.Items.Count}");
                    break;
                }
            }

            await Wait.LatencySleep();
        }

        public bool IsTradeRequestToBeAccepted(NotificationData data, NotificationType type)
        {
            if (type == LokiPoe.InGameState.NotificationType.Trade)
            {
                if (data.CharacterName.ToLower() == cFollowerSettings.Instance.LeaderName.ToLower())
                {
                    Log.Debug($"[{Name}] Trade request found");
                    return true;
                }
                Log.Warn($"[{Name}] Trade request leader ERROR");
                return false;
            }

            Log.Warn($"[{Name}] Trade request general ERROR");
            return false;
        }

        public async Task ProcessOtherOffer()
        {
            var otherOffer = tradeControl.InventoryControl_OtherOffer;
            while (!tradeControl.OtherAcceptedTheOffert || tradeControl.ConfirmLabelText == "Mouse over each item to enable accept")
            {
                Log.Debug($"[{Name}] Waiting for other player to accept. Viewing items");
                otherOffer.ViewItemsInInventory(
                    (_, localItem) => otherOffer.IsItemTransparent(localItem.LocalId),
                    () => LokiPoe.InGameState.TradeUi.IsOpened
                    );

                await Wait.Sleep(1);
            }

            Log.Debug($"[{Name}] Item viewing finished");
        }

        public async Task<bool> ProcessAcceptButton()
        {
            if (await Wait.For(() => !tradeControl.IsConfirmLabelVisible
            && tradeControl.ConfirmLabelText != "Not enough space to accept this trade", "Not enough space to accept this trade", 100, 20000))
            {
                Log.Debug($"[{Name}] Pressing Accept button");
                var acceptResult = tradeControl.Accept();

                await Wait.Sleep(200);

                if (acceptResult == TradeResult.None)
                {
                    Log.Debug($"[{Name}] Trade succesfully accepted");
                    return true;
                }
                else
                {
                    Log.Warn($"[{Name}] Trade accepting error: {acceptResult}");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public enum MoveType
        {
            Stash,
            GuildStash,
            Trade
        }
        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Processed;
        }

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
        public string Name => "Trade Task";
        public string Description => "Task that handles trades in\\out";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";
    }
}
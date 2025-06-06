using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DPBDevHelper;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using log4net;
using static cFollower.TradeTask;

namespace cFollower
{
    public static class TradeHelper
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        
        public static List<string> FastMoveTypes = new List<string>() 
        {
            "StackableCurrency",
            "MapFragment",
            "DivinationCard"
        };

        public static async Task MoveAllFromInventory(InventoryControlWrapper inventoryControl, MoveType moveType)
        {
            int sleepMS = 0;
            switch (moveType)
            {
                case MoveType.Stash:
                    {
                        sleepMS = cFollowerSettings.Instance.StashDepositDelay;
                        break;
                    }
                case MoveType.GuildStash:
                    {
                        sleepMS = cFollowerSettings.Instance.GuildStashDepositDelay;
                        break;
                    }
                case MoveType.Trade:
                    {
                        sleepMS = cFollowerSettings.Instance.TradeDepositDelay;
                        break;
                    }
                default:
                    break;
            }

            var inventory = inventoryControl.Inventory;
            var inventoryItems = inventory.Items.Where(x => x.Rarity != Rarity.Quest && !inventoryControl.IsItemTransparent(x.LocalId)).ToList();
            Log.Debug($"[MoveAllFromInventory] Inventory found: {inventory.IsValid} Inventory Control found: {inventoryControl.IsValid} Inventory items count: {inventoryItems.Count}");

            if (moveType == MoveType.Stash || moveType == MoveType.Trade)
            {
                Log.Debug($"[MoveAllFromInventory] Removing dupliactes b/c it's {moveType} trade");
                RemoveDuplicateCurrency(inventoryItems);
                Log.Debug($"[MoveAllFromInventory] Inventory items after cleaning count: {inventoryItems.Count}");
            }

            foreach (var item in inventoryItems)
            {
                Log.Debug($"[MoveAllFromInventory] Found item {item.Name} IsItemTransparent {inventoryControl.IsItemTransparent(item.LocalId)}");
                if (moveType != MoveType.Trade)
                {
                    await TradeHelper.SelectProperTab(item);
                }

                if (!inventoryControl.IsItemTransparent(item.LocalId))
                {
                    Log.Debug($"[MoveAllFromInventory] FastMoveAll name: {item.FullName} isTransparent {inventoryControl.IsItemTransparent(item.LocalId)}");
                    await Wait.For(() => inventoryControl.FastMoveAll(item.LocalId) == FastMoveResult.None || item == null, "FastMoveAll", 100, 400);
                }

                await Wait.Sleep(sleepMS);
            }

            await Wait.LatencySleep();
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

        public static InventoryControlWrapper GetGuildInventoryControl()
        {
            InventoryControlWrapper inventoryControl = null;

            if (LokiPoe.InGameState.GuildStashUi.IsOpened)
            {
                Log.Debug($"[GetGuildInventoryControl] Guild stash UI is opened");
                inventoryControl = LokiPoe.InGameState.GuildStashUi.InventoryControl;
            }

            return inventoryControl;
        }

        public static async Task<bool> SelectProperTab(Item item)
        {
            if (!LokiPoe.InGameState.GuildStashUi.IsOpened)
            {
                Log.Debug($"[SelectProperTab] LokiPoe.InGameState.GuildStashUi.IsOpened == false");
                return false;
            }


            if (LokiPoe.InGameState.GuildStashUi.TabControl == null)
            {
                Log.Debug($"[SelectProperTab] TabControl == null");
                return false;
            }

            var tabControl = LokiPoe.InGameState.GuildStashUi.TabControl;
            var depositTabNames = Utility.ParseByDivider(cFollowerSettings.Instance.DepositTabNames, ',');

            foreach (var tab in depositTabNames)
            {
                if (tabControl.TabNames.Any(x => x == tab))
                {
                    var result = tabControl.SwitchToTabKeyboard(tab);
                    if (!await Wait.For(() => result == SwitchToTabResult.None, "switching tab", 100, 1000))
                    {
                        Log.Warn($"[SelectProperTab] Tab switching error because: {result}");
                    }

                    var tabObject = LokiPoe.InstanceInfo.GuildStashTabs.First(x => x.DisplayName == tab);

                    if (await Wait.For(() => LokiPoe.InGameState.GuildStashUi.InventoryControl.Inventory != null, "load stash inventory", 100, 1000))
                    {
                        if (LokiPoe.InGameState.GuildStashUi.InventoryControl.Inventory.CanFitItem(item))
                        {
                            Log.Debug($"[SelectProperTab] Can fit item in current tab.");
                            return true;
                        }
                        else
                        {
                            Log.Warn($"[SelectProperTab] CANNOT fit item in current tab. Changing tab.");
                            continue;
                        };
                    }
                    else
                    {
                        Log.Warn($"[SelectProperTab] Error while loading stash tab info");
                        continue;
                    }


                }
            }

            return false;
        }
    }
}

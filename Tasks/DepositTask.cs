using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using log4net;

namespace cFollower
{
    public class DepositTask : ITask
    {
        public async Task<bool> Run()
        {
            if (!cFollowerSettings.Instance.DepositEnabled)
                return false;

            if (!LokiPoe.IsInGame)
            {
                Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (!LokiPoe.Me.IsInHideout)
            {
                Log.Info($"[{Name}] LokiPoe.Me.IsInHideout = {LokiPoe.Me.IsInHideout}");
                return false;
            }

            if (!await ZoneHandler.IsInSameZoneWithLeader())
            {
                Log.Info($"[{Name}] We're not in same zone with leader");
                return false;
            }


            var inventoryControl = TradeTask.GetInventoryControl();
            var inventory = inventoryControl.Inventory;
            if (inventory.Items.Where(x => x.Rarity != Rarity.Quest).ToList().Count <= 0)
            {
                Log.Debug($"[{Name}] Items count {inventory.Items.Where(x => x.Rarity != Rarity.Quest).ToList().Count}. Returning false");
                return false;
            }

            await Coroutines.CloseBlockingWindows();

            if (!await FindGuildStash())
            {
                Log.Warn($"[{Name}] Guild stash find ERORR");
                return false;
            }

            Log.Debug($"[{Name}] Items count {inventory.Items.Where(x => x.Rarity != Rarity.Quest).ToList().Count}");

            await TradeTask.MoveAllFromInventory(inventory, inventoryControl, TradeTask.MoveType.GuildStash);
            await Coroutines.CloseBlockingWindows();

            return true;
        }

        public static async Task<bool> FindGuildStash()
        {
            var guildStash = LokiPoe.ObjectManager.GetObjectByType<GuildStash>();
            await Wait.For(() => guildStash != null, "guild stash null", 50, 300);
            if (guildStash == null)
            {
                Log.Debug($"[DepositTask] Guild stash is null");
                return false;
            }

            while (!LokiPoe.InGameState.IsLeftPanelShown)
            {
                while (guildStash.Distance > 100)
                {
                    if (PlayerMoverManager.Current.MoveTowards(guildStash.Position))
                    {
                        Log.Debug($"[DepositTask] Guild stash found at {guildStash.Position}. Moving");
                    }
                    else
                    {
                        Log.Error($"[DepositTask] Guild stash found at {guildStash.Position}. Failed to move");
                    }
                }

                Log.Debug($"[DepositTask] Guild stash is close enough. Interacting with it");
                if (await Coroutines.InteractWith(guildStash))
                {
                    Log.Debug($"[DepositTask] Succesful interact");
                    if (LokiPoe.InGameState.IsLeftPanelShown)
                    {
                        Log.Debug($"[DepositTask] Left panel found");
                        return true;
                    }
                }
                else
                {
                    Log.Debug($"[DepositTask] Guild stash is null");
                };
                await Wait.Sleep(100);
            }

            return false;
        }

        public async Task<bool> FindStash()
        {
            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
            {
                Log.Debug($"[{Name}] Guild stash is null");
                return false;
            }

            while (!LokiPoe.InGameState.IsLeftPanelShown)
            {
                while (stash.Distance > 50)
                {
                    if (PlayerMoverManager.Current.MoveTowards(stash.Position))
                    {
                        Log.Debug($"[{Name}] Stash found at {stash.Position}. Moving");
                    }
                    else
                    {
                        Log.Error($"[{Name}] Stash found at {stash.Position}. Failed to move");
                    }
                }

                Log.Debug($"[{Name}] Trying to interact with stash");
                if (await Coroutines.InteractWith(stash))
                {
                    Log.Debug($"[{Name}] Succesfully interacted");
                }
                else
                {
                    Log.Debug($"[{Name}] Failed to interact");
                };
                await Wait.Sleep(100);
            }

            if (LokiPoe.InGameState.IsLeftPanelShown)
            {
                Log.Debug($"[{Name}] Succesfully interacted");
                return true;
            }

            return false;
        }

        public static async Task<bool> SelectProperTab(Item item)
        {
            if (LokiPoe.InGameState.GuildStashUi.TabControl == null)
            {
                Log.Debug($"[SelectProperTab] TabControl == null");
                return false;
            }

            if (!LokiPoe.InGameState.GuildStashUi.IsOpened)
            {
                Log.Debug($"[SelectProperTab] LokiPoe.InGameState.GuildStashUi.IsOpened == false");
                return false;
            }

            var tabControl = LokiPoe.InGameState.GuildStashUi.TabControl;
            var depositTabNames = ParseByDivider(cFollowerSettings.Instance.DepositTabNames, ',');

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
                    } else
                    {
                        Log.Warn($"[SelectProperTab] Error while loading stash tab info");
                        continue;
                    }
                    

                }
            }

            return false;
        }

        public static List<string> ParseByDivider(string _str, char divider)
        {
            return _str.Split(divider).ToList();
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Processed;
        }

        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Name => "Deposit Task";
        public string Description => "Task to deposit items to guild stash";
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
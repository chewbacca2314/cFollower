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
using DPBDevHelper;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.NativeWrappers;
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
            {
                //Log.Debug($"[{Name}] Deposit toggle: {cFollowerSettings.Instance.DepositEnabled}");
                return false;
            }
                

            if (!LokiPoe.IsInGame)
            {
                //Log.Debug($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (!LokiPoe.Me.IsInHideout)
            {
                //Log.Debug($"[{Name}] LokiPoe.Me.IsInHideout = {LokiPoe.Me.IsInHideout}");
                return false;
            }

            if (!ZoneHelper.IsInSameZoneWithLeader())
            {
                //Log.Debug($"[{Name}] We're not in same zone with leader");
                return false;
            }

            if (GetGuildStash() == null)
            {
                //Log.Debug("[GetGuildStash] Guild stash is null");
                return false;
            }

            var inventoryControl = TradeHelper.GetInventoryControl();

            if (inventoryControl == null)
            {
                Log.Warn($"[{Name}] Inventory control is null");
                return false;
            }

            var inventory = inventoryControl.Inventory;
            if (inventory.Items.Where(x => x.Rarity != Rarity.Quest).ToList().Count <= 0)
            {
                return false;
            }

            await Coroutines.CloseBlockingWindows();

            if (!await FindGuildStash())
            {
                Log.Warn($"[{Name}] Guild stash find ERORR");
                return false;
            }

            Log.Debug($"[{Name}] Items count {inventory.Items.Where(x => x.Rarity != Rarity.Quest).ToList().Count}");

            await TradeHelper.MoveAllFromInventory(inventoryControl, TradeTask.MoveType.GuildStash);
            await Coroutines.CloseBlockingWindows();

            return true;
        }

        public static async Task<bool> FindGuildStash()
        {
            //var guildStash = LokiPoe.ObjectManager.GetObjectByType<GuildStash>();
            var guildStashIconWrapper = GetGuildStash();
            var guildStashPos = guildStashIconWrapper.LastSeenPosition;
            var myPos = LokiPoe.MyPosition;

            while (!LokiPoe.InGameState.IsLeftPanelShown)
            {
                while (guildStashPos.Distance(myPos) > 70)
                {
                    myPos = LokiPoe.MyPosition;
                    //Log.Debug($"[FindGuildStash] myPos: {myPos} LokiPoe.Me.Position: {LokiPoe.Me.Position} guildStashPos: {guildStashPos} distancefrom: {guildStashPos.Distance(myPos)} distanceto: {myPos.Distance(guildStashPos)}");

                    if (PlayerMoverManager.Current.MoveTowards(guildStashPos))
                    {
                        Log.Debug($"[FindGuildStash] Guild stash found at {guildStashPos}. Moving");
                    }
                    else
                    {
                        Log.Error($"[FindGuildStash] Guild stash found at {guildStashPos}. Failed to move");
                    }
                }

                Log.Debug($"[FindGuildStash] Guild stash is close enough. Interacting with it");

                var guildStashObject = guildStashIconWrapper?.NetworkObject;

                if (guildStashObject == null)
                {
                    Log.Warn("[FindGuildStash] Guild stash is still null");
                    return false;
                }

                if (await Coroutines.InteractWith(guildStashObject))
                {
                    Log.Debug($"[FindGuildStash] Succesful interact");
                    if (await Wait.For(() => LokiPoe.InGameState.IsLeftPanelShown, "guild stash open", 500, 3000))
                    {
                        return true;
                    }
                }
                else
                {
                    Log.Debug($"[FindGuildStash] Guild stash is null");
                };

                await Wait.LatencySleep();
            }

            return false;
        }

        public static MinimapIconWrapper GetGuildStash()
        {
            return LokiPoe.InstanceInfo.MinimapIcons.FirstOrDefault(x => x.MinimapIcon.Name == "StashGuild");
        }

        public static async Task<bool> FindStash()
        {
            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
            {
                Log.Debug($"[FindStash] Guild stash is null");
                return false;
            }

            while (!LokiPoe.InGameState.IsLeftPanelShown)
            {
                while (stash.Distance > 50)
                {
                    if (PlayerMoverManager.Current.MoveTowards(stash.Position))
                    {
                        Log.Debug($"[FindStash] Stash found at {stash.Position}. Moving");
                    }
                    else
                    {
                        Log.Error($"[FindStash] Stash found at {stash.Position}. Failed to move");
                    }
                }

                Log.Debug($"[FindStash] Trying to interact with stash");
                if (await Coroutines.InteractWith(stash))
                {
                    Log.Debug($"[FindStash] Succesfully interacted");
                }
                else
                {
                    Log.Debug($"[FindStash] Failed to interact");
                };
                await Wait.Sleep(100);
            }

            if (LokiPoe.InGameState.IsLeftPanelShown)
            {
                Log.Debug($"[FindStash] Succesfully interacted");
                return true;
            }

            return false;
        }

        #region skip

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

        #endregion skip
    }
}
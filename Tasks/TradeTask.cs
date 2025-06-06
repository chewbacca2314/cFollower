using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DPBDevHelper;
using log4net;

namespace cFollower
{
    public class TradeTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private TradeControlWrapper tradeControl;

        public async Task<bool> Run()
        {
            if (!cFollowerSettings.Instance.TradeEnabled)
            {
                return false;
            }

            if (!LokiPoe.IsInGame)
            {
                //Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (LokiPoe.Me.PartyStatus != PartyStatus.PartyMember)
            {
                Log.Debug($"[{Name}] Not a party member");
                return false;
            }

            if (!LokiPoe.InGameState.NotificationHud.IsOpened)
            {
                Log.Debug($"[{Name}] Notification hud not opened");
                return false;
            }
            if (!LokiPoe.InGameState.NotificationHud.NotificationList.Any(x => x.NotificationTypeEnum == LokiPoe.InGameState.NotificationType.Trade))
            {
                Log.Debug($"[{Name}] No trade notificatio");
                return false;
            }
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            await Wait.SleepSafe(400, 400);
            var acceptResult = LokiPoe.InGameState.NotificationHud.HandleNotificationEx(IsTradeRequestToBeAccepted);

            Log.Debug($"[{Name}] Found notification. Accept result: {acceptResult}");

            if (!LokiPoe.InGameState.TradeUi.IsOpened)
            {
                //Log.Warn($"[{Name}] Trade UI not opened");
                //await Coroutines.CloseBlockingWindows();
                return false;
            }

            tradeControl = LokiPoe.InGameState.TradeUi.TradeControl;

            if (tradeControl == null)
            {
                Log.Warn($"[{Name}] tradeControl is null");
                await Coroutines.CloseBlockingWindows();
                return false;
            }

            var currentArea = LokiPoe.CurrentWorldArea;
            var otherOffer = tradeControl.InventoryControl_OtherOffer;
            if (currentArea.IsCombatArea)
            {
                Log.Debug($"[{Name}] Start trading in combat area");
                await ProcessOtherOffer();
                await ProcessAcceptButton();
            }
            else if (currentArea.IsHideoutArea || currentArea.IsTown)
            {
                if (LokiPoe.InGameState.InventoryUi.InventoryControl_Main == null)
                {
                    Log.Debug($"[{Name}] Inventory control is null");
                    return false;
                }

                var inventoryControl = LokiPoe.InGameState.InventoryUi.InventoryControl_Main;

                if (inventoryControl?.Inventory.Items.Count > 0)
                {
                    await TradeHelper.MoveAllFromInventory(inventoryControl, MoveType.Trade);
                }

                await ProcessOtherOffer();
                await ProcessAcceptButton();
            }

            return true;
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

        public bool IsTradeRequestToBeAccepted(LokiPoe.InGameState.NotificationData data, LokiPoe.InGameState.NotificationType type)
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

                await Wait.Sleep(100);
            }

            Log.Debug($"[{Name}] Item viewing finished");
        }

        public async Task<bool> ProcessAcceptButton()
        {
            if (!await Wait.For(() => !tradeControl.IsConfirmLabelVisible
            && tradeControl?.ConfirmLabelText != "Not enough space to accept this trade", "Not enough space to accept this trade", 500, 20000))
                return false;

            Log.Debug($"[{Name}] Pressing Accept button");
            if (await AcceptBtnWait())
            {
                return await AcceptBtnClick();
            }

            return false;
        }

        public async Task<bool> AcceptBtnWait()
        {
            return await Wait.For(() => tradeControl.AcceptButtonText.ToLower() == "accept", "accept button text", 100, 1000);
        }

        public async Task<bool> AcceptBtnClick()
        {
            return await Wait.For(() =>
            {
                var tradeResult = tradeControl.Accept();
                Log.Debug($"[{Name}] Trade result: {tradeResult}");
                return tradeResult == TradeResult.None;
            }, "accept button click", 100, 1000);
        }

        public enum MoveType
        {
            Stash,
            GuildStash,
            Trade
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

        #endregion skip
    }
}
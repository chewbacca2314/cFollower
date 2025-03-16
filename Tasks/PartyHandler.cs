using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Framework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace TestPlugin
{
    public class PartyHandler: ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Name => "Party Handler";
        public string Description => "Handles party composition";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";
        static cFollowerSettings settings = cFollowerSettings.Instance;
        public void Start()
        {
            Log.InfoFormat($"[{Name}] Task Loaded.");
        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }
        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }
            //Log.Debug($"[{Name}] Start executing");
            var partyStatus = LokiPoe.InstanceInfo.PartyStatus;
            //Log.Debug($"[{Name}] {partyStatus}");
            if (partyStatus == PartyStatus.PartyMember) 
            {
                //Log.Debug($"[{Name}] We're in party, returning false");
                return false;
            }
            if (LokiPoe.InstanceInfo.PendingPartyInvites.Any(x => x.PartyMembers.Any(y => y.PlayerEntry.Name == settings.LeaderName)))
            {
                Log.Debug($"[{Name}] Party invite found");
                LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.S);
                LokiPoe.InGameState.SocialUi.SwitchToPartyTab();
                var result = LokiPoe.InGameState.SocialUi.HandlePendingPartyInviteNew(settings.LeaderName);
                if (result == LokiPoe.InGameState.HandlePendingPartyInviteResut.Accepted)
                {
                    Log.Debug($"[{Name}] Party invite accepted");
                    await Coroutines.CloseBlockingWindows();
                    return false;
                }
                else
                {
                    Log.Debug($"[{Name}] Failed to accept party invite");
                };
            } else
            {
                Log.Debug($"[{Name}] Party invite not found");
            }
            if (partyStatus == PartyStatus.PartyLeader) // set actual party leader if we're in party
            {
                var leader = GetLeaderPartyMember();
                if (leader != null)
                {
                    Log.Debug($"[{Name}] Leader in party found, passing PL to him");
                    var ctxResult = LokiPoe.InGameState.PartyHud.OpenContextMenu(settings.LeaderName);
                    if (ctxResult == LokiPoe.InGameState.OpenContextMenuResult.None) 
                    {
                        Log.Debug($"[{Name}] Party menu opened");
                    }
                    else {
                        Log.Debug($"[{Name}] Failed to open party menu");
                    }
                    if (LokiPoe.InGameState.ContextMenu.IsOpened)
                    {
                        Log.Debug($"[{Name}] Context menu is opened");
                        LokiPoe.InGameState.ContextMenu.PromoteToPartyLeader();
                        Log.Debug($"[{Name}] Leader should be promoted now");
                    }
                } else
                {
                    Log.Debug($"[{Name}] Actual leader not in party");
                }
                return true;
            }

            await Wait.SleepSafe(10, 15);
            return true;
        }
        public static PartyMember GetLeaderPartyMember()
        {
            return LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.PlayerEntry.Name == settings.LeaderName);
        }
        public static Player GetLeaderPlayer()
        {
            return LokiPoe.ObjectManager.GetObjectsByType<Player>().FirstOrDefault(x => x.Name == settings.LeaderName);
        }
        public async Task<LogicResult> Logic (Logic logic)
        {
            return LogicResult.Unprovided;
        }
        public MessageResult Message(Message message)
        {
            return MessageResult.Processed;
        }
        public void CloseSocialPanel()
        {
            Log.Debug($"[{Name}] Now closing social UI panel");

            if (LokiPoe.InGameState.SocialUi.IsOpened)
            {
                var closeAllWindowKey = LokiPoe.Input.Binding.close_panels;
                LokiPoe.Input.SimulateKeyEvent(closeAllWindowKey);
            }

        }
    }

}

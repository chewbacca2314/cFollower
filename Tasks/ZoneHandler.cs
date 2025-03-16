using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Framework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Components;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;
using AreaTransition = DreamPoeBot.Loki.Game.Objects.AreaTransition;

namespace TestPlugin
{
    public class ZoneHandler : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private Stopwatch zoneSW = Stopwatch.StartNew();
        public string Name => "Zone Handler";
        public string Description => "Class to handle zone transition/swirl";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";
        float oldDistance;
        float newDistance;
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
        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }
            if (await IsInSameZoneWithLeader()) // add transitioning
            {
                //oldDistance = PartyHandler.GetLeaderPlayer().Distance;
                //if (zoneSW.ElapsedMilliseconds > 1000)
                //{
                //    newDistance = PartyHandler.GetLeaderPlayer().Distance;
                //    zoneSW.Restart();
                //}
                //if (Math.Abs(newDistance - oldDistance) > cFollowerSettings.Instance.DistanceToCheckTransition)
                //{
                //    Log.Debug($"[{Name}] Leader travelled too far, looking for transition");
                //    var transitionNearLeader = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>().OrderBy(x => x.Position.Distance(PartyHandler.GetLeaderPlayer().Position)).First();
                //    if (transitionNearLeader.IsValid && transitionNearLeader.IsTargetable)
                //    {
                //        await Coroutines.InteractWith(transitionNearLeader);
                //        await Wait.SleepSafe(100, 150);
                //    }
                //    return true;
                //}

                //Log.Debug($"[{Name}] We're in same zone with leader. Returning false");
                return false;
            } else
            {
                if (await IsLeaderOnMap())
                {
                    Log.Debug($"[{Name}] Leader on map. Trying to find portals");
                    var portals = LokiPoe.ObjectManager.InTownPortals.OrderBy(x => x.Distance).ToList();
                    await Coroutines.InteractWith(portals.First());
                    await Wait.SleepSafe(100, 150);
                }
                else
                {
                    if (await IsLeaderInLab() && (LokiPoe.CurrentWorldArea.IsLabyrinthArea || LokiPoe.CurrentWorldArea.Name == "Aspirant's Plaza")) // add interaction with lab trial later on
                    {
                        Log.Debug($"[{Name}] Leader in lab. Trying to find transition");
                        var leaderArea = PartyHandler.GetLeaderPartyMember().PlayerEntry.Area;
                        var leaderTransition = LokiPoe.ObjectManager.AreaTransition(leaderArea.Name);
                        if (leaderTransition != null)
                        {
                            Log.Debug($"[{Name}] Leader in lab. Transition found");
                            await Coroutines.InteractWith(leaderTransition);
                            await Wait.SleepSafe(100, 150);
                        }
                    }
                    else
                    {
                        if (!await IsLeaderInLab() && (await IsLeaderInCombatArea() || await IsLeaderInHideout() || await IsLeaderInTown()))
                        {
                            Log.Debug($"[{Name}] Leader is in combat area/hideout. Swirling");
                            LokiPoe.InGameState.PartyHud.FastGoToZone(PartyHandler.GetLeaderPartyMember().PlayerEntry.Name);
                            await Wait.SleepSafe(1000, 1100);
                        }
                    }
                }
            }

            Log.Debug($"[{Name}] ");
            return true;
        }
        public async Task<LogicResult> Logic (Logic logic)
        {
            return LogicResult.Unprovided;
        }
        public MessageResult Message(Message message)
        {
            return MessageResult.Processed;
        }
        public static async Task<bool> IsLeaderInHideout()
        {
            var leader = PartyHandler.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsHideoutArea;
            }
            await Wait.Sleep(1);
            return false;
        }
        public static async Task<bool> IsLeaderOnMap()
        {
            var leader = PartyHandler.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsMap;
            }
            await Wait.Sleep(1);
            return false;
        }
        public static async Task<bool> IsLeaderInCombatArea()
        {
            var leader = PartyHandler.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsCombatArea;
            }
            await Wait.Sleep(1);
            return false;
        }
        public static async Task<bool> IsLeaderInLab()
        {
            // check for leader in lab
            var leader = PartyHandler.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsLabyrinthArea;
            }
            await Wait.Sleep(1);
            return false;
        }
        public static async Task<bool> IsLeaderInTown()
        {
            // check for leader in lab
            var leader = PartyHandler.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsTown;
            }
            await Wait.Sleep(1);
            return false;
        }
        public static async Task<bool> IsInSameZoneWithLeader()
        {
            var leader = PartyHandler.GetLeaderPartyMember();
            if (leader != null)
            {
                return LokiPoe.InGameState.PartyHud.IsInSameZone(leader.PlayerEntry.Name);
            }
            await Wait.Sleep(1);
            return false;
        }
    }
}

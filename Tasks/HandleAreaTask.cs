using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DPBDevHelper;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace cFollower
{
    public class HandleAreaTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private Vector2i lastSeenLeaderPos;
        private bool blockedTransition = false;
        private List<TriggerableBlockage> arenaBlockages;
        private AreaTransition areaTransition = null;
        private Player leader;

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || !ExilePather.IsReady)
            {
                //Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            //if (leader != null)
            //{
            //    return false;
            //}

            if (ZoneHelper.IsInSameZoneWithLeader())
            {
                if (leader == null)
                {
                    return false;
                }
                //var leader = Utility.GetLeaderPlayer();
                Vector2i leaderPos = leader.Position;
                Vector2i myPos = LokiPoe.Me.Position;
                string currentWorldArea = LokiPoe.CurrentWorldArea.Name;

                if (!blockedTransition && Utility.TransitionCheckAreas.ContainsKey(currentWorldArea))
                {
                    var blockages = LokiPoe.ObjectManager.GetObjectsByType<TriggerableBlockage>().Where(x => Utility.TransitionCheckAreas.Any(y => y.Key == currentWorldArea && y.Value == x.Metadata)).ToList();
                    if (blockages.Count() > 0)
                    {
                        arenaBlockages = blockages;
                        foreach (var blockage in blockages)
                        {
                            Log.Debug($"[{Name}] Found arena transition, blocking it");
                            Utility.AddObstacle(blockage);
                            blockedTransition = true;
                        }
                    }
                }

                Vector2i fastWalkable = ExilePather.FastWalkablePositionFor(leaderPos);
                if (ExilePather.PathExistsBetween(myPos, fastWalkable))
                {
                    lastSeenLeaderPos = fastWalkable;
                    areaTransition = null;
                    return false;
                }
                else
                {
                    if (ExilePather.PathExistsBetween(myPos, lastSeenLeaderPos))
                    {
                        if (lastSeenLeaderPos != Vector2i.Zero)
                        {
                            if (lastSeenLeaderPos.Distance(myPos) > 30)
                            {
                                Log.Debug($"[{Name}] No path to leader. Moving to last seen pos at {lastSeenLeaderPos}.");
                                PlayerMoverManager.Current.MoveTowards(lastSeenLeaderPos);
                                await Wait.SleepSafe(20, 30);
                            }

                            if (areaTransition == null)
                            {
                                areaTransition = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>()
                                    .OrderBy(x => x.Position.Distance(lastSeenLeaderPos))
                                    .FirstOrDefault(x => ExilePather.PathExistsBetween(myPos, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                            }

                            var interactionResult = await Coroutines.InteractWith(areaTransition);
                            Log.Debug($"[{Name}] Interacting with area transition {areaTransition.Name} at {areaTransition.Position}. Succesful?: {interactionResult}");
                            await Wait.SleepSafe(100, 200);

                            if (arenaBlockages?.Any() == true)
                            {
                                foreach (var x in arenaBlockages)
                                {
                                    Log.Debug($"[{Name}] We transitioned to arena. Now unblocking it at {x.Position}");
                                    Utility.RemoveObstacle(x);
                                }
                            }
                        }
                        return true;
                    }
                    else
                    {
                        areaTransition = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>()
                            .OrderBy(x => x.Position.Distance(LokiPoe.Me.Position))
                            .FirstOrDefault(x => ExilePather.PathExistsBetween(LokiPoe.Me.Position, ExilePather.FastWalkablePositionFor(x.Position, 20)));

                        if (areaTransition != null)
                        {
                            var interactionResult = await Coroutines.InteractWith(areaTransition);
                            Log.Debug($"[{Name}] Interacting with area transition {areaTransition?.Name} at {areaTransition?.Position}. Succesful?: {interactionResult}");
                            await Wait.SleepSafe(100, 200);

                            if (arenaBlockages?.Any() == true)
                            {
                                foreach (var x in arenaBlockages)
                                {
                                    Log.Debug($"[{Name}] We transitioned to arena.s Now unblocking it at {x.Position}");
                                    Utility.RemoveObstacle(x);
                                }
                            }
                            return true;
                        }
                    }

                    return true;
                }
            }
            else
            {
                if (ZoneHelper.IsLeaderOnMap())
                {
                    Log.Debug($"[{Name}] Leader on map. Trying to find portals");
                    var portals = LokiPoe.ObjectManager.InTownPortals.OrderBy(x => x.Distance).ToList();
                    var portalsCount = portals.Count;
                    Log.Debug($"[{Name}] Leader on map. Found {portalsCount} portals");

                    if (portals.Count > 0)
                    {
                        await Coroutines.InteractWith(portals.First());
                        await Wait.LatencySleep();
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    if (ZoneHelper.IsLeaderInLab() && (LokiPoe.CurrentWorldArea.IsLabyrinthArea || LokiPoe.CurrentWorldArea.Name == "Aspirants' Plaza")) // add interaction with lab trial later on
                    {
                        Log.Debug($"[{Name}] Leader in lab. Trying to find transition");
                        var leaderArea = Utility.GetLeaderPartyMember().PlayerEntry.Area;
                        var leaderTransition = LokiPoe.ObjectManager.AreaTransition(leaderArea.Name);
                        if (leaderTransition != null)
                        {
                            Log.Debug($"[{Name}] Leader in lab. Transition found at {leaderTransition.Position}");
                            await Coroutines.InteractWith(leaderTransition);
                            await Wait.LatencySleep();
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (!ZoneHelper.IsLeaderInLab() && (ZoneHelper.IsLeaderInCombatArea() || ZoneHelper.IsLeaderInHideout() || ZoneHelper.IsLeaderInTown()))
                        {
                            Log.Debug($"[{Name}] Leader is in combat area/hideout. Swirling");
                            LokiPoe.InGameState.PartyHud.FastGoToZone(Utility.GetLeaderPartyMember().PlayerEntry.Name);
                            await Wait.SleepSafe(1000, 1100);
                        }
                    }
                }
            }
            return true;
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
                        leader = playerList.FirstOrDefault(x => x.IsLeader)?.PlayerEntity;
                    }
                    //Log.Debug($"[CombatTask] Monster list processed, count {monsterList.Count}");
                    return MessageResult.Processed;
                };
            }

            if (message.Id == DPBDevHelper.Enums.MessageType.ZoneChange.ToString())
            {
                ClearData();
            }

            return MessageResult.Processed;
        }

        private void ClearData()
        {
            leader = null;
            areaTransition = null;
            lastSeenLeaderPos = Vector2i.Zero;
        }

        #region skip

        public string Name => "HandleAreaTask";
        public string Description => "Class to handle zone transition/swirl";
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

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        #endregion skip
    }
}
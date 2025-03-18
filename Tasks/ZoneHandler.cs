using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace cFollower
{
    public class ZoneHandler : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private Stopwatch zoneSW = Stopwatch.StartNew();
        private Vector2i lastSeenLeaderPos;
        private bool blockedTransition = false;
        private List<TriggerableBlockage> arenaBlockages;
        private AreaTransition areaTransition = null;

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                //Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (IsInSameZoneWithLeader())
            {
                var leader = Utility.GetLeaderPlayer();
                var leaderPos = leader.Position;
                var myPos = LokiPoe.Me.Position;
                var currentWorldArea = LokiPoe.CurrentWorldArea.Name;

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

                var fastWalkable = ExilePather.FastWalkablePositionFor(leaderPos);
                if (ExilePather.PathExistsBetween(myPos, fastWalkable))
                    lastSeenLeaderPos = fastWalkable;
                else
                {
                    if (lastSeenLeaderPos != Vector2i.Zero)
                    {
                        while (lastSeenLeaderPos.Distance(myPos) > 30)
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
                        Log.Debug($"[{Name}] Interacting with area transition at {areaTransition.Position}. Succesful?: {interactionResult}");
                        await Wait.LatencySleep();

                        if (arenaBlockages.Count > 0)
                        {
                            foreach (var x in arenaBlockages)
                            {
                                Log.Debug($"[{Name}] We transitioned to arena. Now unblocking it at {x.Position}");
                                Utility.RemoveObstacle(x);
                            }
                        }
                    }
                }
                //Log.Debug($"[{Name}] We're in same zone with leader. Returning false");
                return false;
            }
            else
            {
                if (IsLeaderOnMap())
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
                    if (IsLeaderInLab() && (LokiPoe.CurrentWorldArea.IsLabyrinthArea || LokiPoe.CurrentWorldArea.Name == "Aspirant's Plaza")) // add interaction with lab trial later on
                    {
                        Log.Debug($"[{Name}] Leader in lab. Trying to find transition");
                        var leaderArea = Utility.GetLeaderPartyMember().PlayerEntry.Area;
                        var leaderTransition = LokiPoe.ObjectManager.AreaTransition(leaderArea.Name);
                        if (leaderTransition != null)
                        {
                            Log.Debug($"[{Name}] Leader in lab. Transition found at {leaderTransition.Position}");
                            await Coroutines.InteractWith(leaderTransition);
                            await Wait.LatencySleep();
                        } else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (!IsLeaderInLab() && (IsLeaderInCombatArea() || IsLeaderInHideout() || IsLeaderInTown()))
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

        public static bool IsLeaderInHideout()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsHideoutArea;
            }

            return false;
        }

        public static bool IsLeaderOnMap()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsMap;
            }

            return false;
        }

        public static bool IsLeaderInCombatArea()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsCombatArea;
            }

            return false;
        }

        public static bool IsLeaderInLab()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsLabyrinthArea;
            }

            return false;
        }

        public static bool IsLeaderInTown()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsTown;
            }

            return false;
        }

        public static bool IsInSameZoneWithLeader()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return LokiPoe.InGameState.PartyHud.IsInSameZone(leader.PlayerEntry.Name);
            }

            return false;
        }

        #region skip

        public string Name => "Zone Handler";
        public string Description => "Class to handle zone transition/swirl";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";
        private float oldDistance;
        private float newDistance;

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

        public MessageResult Message(Message message)
        {
            return MessageResult.Processed;
        }

        #endregion skip
    }
}
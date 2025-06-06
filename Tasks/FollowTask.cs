using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DPBDevHelper;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;

using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace cFollower
{
    public class FollowTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private Player leader;

        public void Tick()
        {
        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || !ExilePather.IsReady)
            {
                //Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (leader == null)
            {
                Log.Debug($"[{Name}] Leader not found. Do nothing");
                return false;
            }

            if (!ZoneHelper.IsInSameZoneWithLeader())
            {
                return false;
            }

            float distanceToLeader = leader.Distance;
            if (distanceToLeader < cFollowerSettings.Instance.MinDistanceToFollow && !leader.IsMoving)
            {
                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                Log.Debug($"[{Name}] Standing. Distance to leader: {(distanceToLeader)}");
                return false;
            }

            var leaderPosition = leader.Position;
            //Log.Debug($"[FollowTask] leaderPos {leader.Position} leaderName {leader.Name} id {leader.Id}");
            Vector2i fastWalkable = ExilePather.FastWalkablePositionFor(leaderPosition);

            //if (!ExilePather.PathExistsBetween(LokiPoe.Me.Position, fastWalkable))
            //{
            //    return true;
            //}

            if (cFollowerSettings.Instance.FollowType == cFollowerSettings.MoveType.ToCursor)
            {
                Vector2i leaderDest = Vector2i.Zero;

                if (leader?.CurrentAction != null && leader.CurrentAction.Skill.InternalName == "Move")
                {
                    leaderDest = leader.CurrentAction.Destination;
                }

                if (leaderDest != Vector2i.Zero)
                {
                    fastWalkable = ExilePather.FastWalkablePositionFor(leaderDest);
                }
            }

            PlayerMoverManager.Current.MoveTowards(fastWalkable);

            await Wait.SleepSafe(20, 40);

            return false;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Enums.MessageType.PlayerListUpdate.ToString())
            {
                //Log.Debug("[CombatTask] Message with monster list received");
                HashSet<PlayerInfo> playerList = new HashSet<PlayerInfo>();
                if (message.TryGetInput<HashSet<PlayerInfo>>(0, out playerList))
                {
                    if (playerList?.Count > 0)
                    {
                        leader = playerList.FirstOrDefault(x => x.IsLeader).PlayerEntity;
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
        }

        #region skip

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await Task.FromResult(LogicResult.Unprovided);

        }

        public string Name => "Follow Task";
        public string Description => "Task to follow leader";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";

        public void Start()
        {
            Log.Info($"[{Name}] Task Loaded.");
        }

        public void Stop()
        {
        }

        #endregion skip
    }
}
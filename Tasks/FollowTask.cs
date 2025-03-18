using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;
using cFollower.cMover;

namespace cFollower
{
    public class FollowTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Name => "Follow Task";
        public string Description => "Task to follow leader";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";
        static cFollowerSettings settings = cFollowerSettings.Instance;
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
            Player leader = Utility.GetLeaderPlayer();
            if (leader != null)
            {
                float distanceToLeader = leader.Distance;
                if (distanceToLeader < settings.MinDistanceToFollow)
                {
                    Log.Debug($"[{Name}] Standing. Distance to leader: {((int)distanceToLeader)}");
                    return false;
                }
                var leaderPosition = leader.Position;

                if (PlayerMoverManager.Current.MoveTowards(leaderPosition))
                {
                    Log.Debug($"[{Name}] Leader found at {leaderPosition}. Moving");
                } else
                {
                    Log.Error($"[{Name}] Leader found at {leaderPosition}. Failed to move");
                }

            } else
            {
                Log.Debug($"[{Name}] Leader not found. Do nothing");
            }

            await Wait.SleepSafe(50, 70);
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
    }
}

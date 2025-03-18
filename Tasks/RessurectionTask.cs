using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using log4net;

namespace cFollower
{
    public class RessurectionTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                //Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (!LokiPoe.Me.IsDead)
                return false;

            Log.Debug($"[{Name}] Dead, executing resurrection logic");

            if (LokiPoe.InGameState.ResurrectPanel.IsOpened)
            {
                Log.Debug($"[{Name}] Resurrection panel is opened, resurrecting at checkpoint");
                var resurrectResult = LokiPoe.InGameState.ResurrectPanel.ResurrectToCheckPoint();
                Log.Debug($"[{Name}] Resurrect result: {resurrectResult}");
                await Wait.LatencySleep();
                if (resurrectResult != LokiPoe.InGameState.ResurrectResult.None)
                {
                    LokiPoe.InGameState.ResurrectPanel.ResurrectToTown();
                    await Wait.LatencySleep();
                }
            }

            return LokiPoe.Me.IsDead;
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

        public string Name => "RessurectionTask";
        public string Description => "Task for ressurection on death logic";
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
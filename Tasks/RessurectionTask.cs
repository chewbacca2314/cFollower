using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using log4net;

namespace TestPlugin
{
    public class RessurectionTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

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
        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }
            if (LokiPoe.Me.IsDead)
            {
                Log.Debug($"[{Name}] Dead, executing resurrection logic");
                if (LokiPoe.InGameState.ResurrectPanel.IsOpened)
                {
                    Log.Debug($"[{Name}] Resurrection panel is opened, resurrecting at checkpoint");
                    LokiPoe.InGameState.ResurrectPanel.ResurrectToCheckPoint();
                    
                    if (LokiPoe.Me.Health > 0)
                    {
                        Log.Debug($"[{Name}] Resurrected");
                    }
                    else
                    {
                        LokiPoe.InGameState.ResurrectPanel.ResurrectToTown();
                    }

                    if (LokiPoe.Me.Health > 0)
                    {
                        Log.Debug($"[{Name}] Resurrected");
                    }
                    else
                    {
                        Log.Debug($"[{Name}] Resurrection error");
                    }

                }
            } else
            {
                return false;
            }

            await Wait.SleepSafe(10, 15);
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

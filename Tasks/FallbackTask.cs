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
    public class FallbackTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Name => "Fallback Task";
        public string Description => "Fallback Task";
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
            Log.Debug($"[{Name}] Do nothing.");
            await Wait.Sleep(1000);
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

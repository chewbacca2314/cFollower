using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using log4net;

namespace TestPlugin
{
    public class TestTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Name => "Template Task";
        public string Description => "Template task";
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
            await Wait.Sleep(1);
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using log4net;
using TestPlugin.cRoutine;

namespace TestPlugin.cRoutine
{
    public class cRoutine : IRoutine
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        cRoutineGUI _gui;
        public UserControl Control => _gui ?? (_gui = new cRoutineGUI());

        public JsonSettings Settings => cRoutineSettings.Instance;
        public cRoutineSettings settings => cRoutineSettings.Instance;

        public string Name => "cRoutine";
        public string Description => "Routine for cFollower";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";
        public override string ToString() => $"{Name}: {Description}";
        public void Initialize()
        {
            Log.Info($"Initializing {Name} - {Description} by {Author}, version {Version}");
        }
        public void Deinitialize()
        {
            Log.Info($"Deinitializing {Name} - {Description} by {Author}, version {Version}");
        }
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

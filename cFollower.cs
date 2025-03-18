using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using log4net;

namespace cFollower
{
    internal class cFollower : IBot
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private cFollowerGUI _gui;
        public UserControl Control => _gui ?? (_gui = new cFollowerGUI());

        public JsonSettings Settings => cFollowerSettings.Instance;

        private readonly TaskManager _taskManager = new TaskManager();
        private Coroutine _coroutine;

        public void Start()
        {
            BotManager.MsBetweenTicks = 40;
            Log.Debug($"[Start] MS Between Ticks {BotManager.MsBetweenTicks}.");

            LokiPoe.ProcessHookManager.Enable();

            // Cache all bound keys.
            LokiPoe.Input.Binding.Update();

            ExilePather.Reload();

            _taskManager.Reset();
            PluginManager.Start();
            RoutineManager.Start();
            PlayerMoverManager.Start();

            AddTasks();
            _taskManager.Start();

            foreach (var plugin in PluginManager.EnabledPlugins)
            {
                Log.Debug($"[Start] The plugin {plugin.Name} is enabled.");
            }

        }

        public void Stop()
        {
            PluginManager.Stop();
            RoutineManager.Stop();
            PlayerMoverManager.Stop();
            _taskManager.Stop();

            LokiPoe.ProcessHookManager.Disable();

            // Cleanup the coroutine.
            if (_coroutine != null)
            {
                _coroutine.Dispose();
                _coroutine = null;
            }
        }

        public void Tick()
        {
            if (_coroutine == null)
            {
                _coroutine = new Coroutine(() => MainCoroutine());
            }

            ExilePather.Reload();

            PluginManager.Tick();
            RoutineManager.Tick();
            PlayerMoverManager.Tick();
            _taskManager.Tick();

            if (_coroutine.IsFinished)
            {
                Log.Debug($"The bot coroutine has finished in a state of {_coroutine.Status}");
                BotManager.Stop();
                return;
            }

            // Otherwise Resume the coroutine execution.
            try
            {
                _coroutine.Resume();
            }
            catch
            {
                var c = _coroutine;
                _coroutine = null;
                c.Dispose();
                throw;
            }
        }

        public void AddTasks()
        {
            _taskManager.Add(new PartyHandler());
            _taskManager.Add(new RessurectionTask());
            _taskManager.Add(new ZoneHandler());
            _taskManager.Add(new TradeTask());
            _taskManager.Add(new DepositTask());
            _taskManager.Add(new FollowTask());
            //_taskManager.Add(new LootTask());
            _taskManager.Add(new FallbackTask());
        }

        private async Task MainCoroutine()
        {
            while (true)
            {
                await _taskManager.Run(TaskGroup.Enabled, RunBehavior.UntilHandled);
                
                await Coroutine.Yield();
            }
        }

        public string Author => "chewbacca";
        public string Description => "follow bot";
        public string Name => "cFollower";
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

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await _taskManager.ProvideLogic(TaskGroup.Enabled, RunBehavior.UntilHandled, logic);
        }
    }
}
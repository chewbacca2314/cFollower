using System.Threading.Tasks;
using System.Windows.Controls;
using DPBDevHelper;
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

            LokiPoe.ProcessHookManager.Enable();

            LokiPoe.Input.Binding.Update();
            ExilePather.Reload();
            _taskManager.Reset();

            AddTasks();
            _taskManager.Start();

            PluginManager.Start();
            RoutineManager.Start();
            PlayerMoverManager.Start();

            DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.OnStartMessage.ToString());

            foreach (var plugin in PluginManager.EnabledPlugins)
            {
                Log.Debug($"[Start] The plugin {plugin.Name} is enabled.");
            }
        }

        public void Stop()
        {
            DreamPoeBot.Loki.Bot.Utility.BroadcastMessage(this, Enums.MessageType.OnStopMessage.ToString());

            _taskManager.Stop();

            PluginManager.Stop();
            RoutineManager.Stop();
            PlayerMoverManager.Stop();

            LokiPoe.ProcessHookManager.Disable();

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

            _taskManager.Tick();

            PluginManager.Tick();
            RoutineManager.Tick();
            PlayerMoverManager.Tick();

            if (_coroutine.IsFinished)
            {
                Log.Debug($"The bot coroutine has finished in a state of {_coroutine.Status}");
                BotManager.Stop();
                return;
            }

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
            ITask entityScanTask = new EntityScanTask();
            ITask handlePartyTask = new HandlePartyTask();
            ITask resurrectionTask = new ResurrectionTask();
            ITask handleAreaTask = new HandleAreaTask();
            ITask combatTask = new CombatTask();
            ITask tradeTask = new TradeTask();
            ITask depositTask = new DepositTask();
            ITask lootTask = new LootTask();
            ITask followTask = new FollowTask();

            if (cFollowerSettings.Instance.EntityScanTaskToggle)
            {
                _taskManager.Add(entityScanTask);
            }

            if (cFollowerSettings.Instance.HandlePartyTaskToggle)
            {
                _taskManager.Add(handlePartyTask);
            }

            if (cFollowerSettings.Instance.ResurrectionTaskToggle)
            {
                _taskManager.Add(resurrectionTask);
            }

            if (cFollowerSettings.Instance.HandleAreaTaskToggle)
            {
                _taskManager.Add(handleAreaTask);
            }

            if (cFollowerSettings.Instance.CombatTaskToggle)
            {
                _taskManager.Add(combatTask);
            }

            if (cFollowerSettings.Instance.TradeTaskToggle)
            {
                _taskManager.Add(tradeTask);
            }

            if (cFollowerSettings.Instance.DepositTaskToggle)
            {
                _taskManager.Add(depositTask);
            }

            if (cFollowerSettings.Instance.LootTaskToggle)
            {
                _taskManager.Add(lootTask);
            }

            if (cFollowerSettings.Instance.FollowTaskToggle)
            {
                _taskManager.Add(followTask);
            }
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
            foreach (string messageType in Enums.MessageTypeList)
            {
                if (message.Id == messageType)
                {
                    _taskManager.SendMessage(TaskGroup.Enabled, message);
                    //if (message.Id == Enums.MessageType.PointOfInterestListUpdate.ToString())
                    //{
                    //    Log.Warn($"[cMapper] Sent message with id {message.Id}");
                    //}
                    return MessageResult.Processed;
                }
            }

            return MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await _taskManager.ProvideLogic(TaskGroup.Enabled, RunBehavior.UntilHandled, logic);
        }
    }
}
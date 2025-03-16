using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Implementation.Content.SkillBlacklist;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using log4net;

namespace TestPlugin.cMover
{
    public class cMover : IPlayerMover
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private cMoverGUI _gui;
        public UserControl Control => _gui ?? (_gui = new cMoverGUI());

        public JsonSettings Settings => cMoverSettings.Instance;
        public cMoverSettings settings = cMoverSettings.Instance;

        private PathfindingCommand _cmd;
        public PathfindingCommand CurrentCommand => _cmd;

        public string Name => "cMover";
        public string Description => "Mover for cFollower";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";
        private static Stopwatch sw = Stopwatch.StartNew();
        private static Skill moveSkillOnPanel;
        private static System.Windows.Forms.Keys moveSkillOnPanelKey;
        private static int moveSkillOnPanelSlot;

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
            Log.Info($"[{Name}][Start] Loaded.");
            ClearLMB();
            moveSkillOnPanel = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill;
            if (moveSkillOnPanel != null)
            {
                moveSkillOnPanelKey = moveSkillOnPanel.BoundKey;
                moveSkillOnPanelSlot = moveSkillOnPanel.Slot;
                Log.Debug($"[{Name}] Move skill found at \"{moveSkillOnPanelKey}\"");
            }
        }
        public void Stop()
        {

        }

        public bool MoveTowards(Vector2i position, params dynamic[] user)
        {
            if (!LokiPoe.IsInGame)
            {
                Log.Info($"[{Name}] LokiPoe.IsInGame = {LokiPoe.IsInGame}");
                return false;
            }

            if (position == Vector2i.Zero)
            {
                Log.Error($"[{Name}.MoveTowards] Recieved [0, 0] as position, return false");
                return false;
            }

            var myPosition = LokiPoe.MyPosition;

            if (
                _cmd == null ||
                _cmd.Path == null ||
                _cmd.EndPoint != position ||
                (sw.IsRunning && sw.ElapsedMilliseconds > settings.PathRefreshRate) ||
                _cmd.Path.Count <= 2 ||
                _cmd.Path.All(p => myPosition.Distance(p) > 4)
                )
            {
                _cmd = new PathfindingCommand(myPosition, position, 3);
                if (!ExilePather.FindPath(ref _cmd))
                {
                    sw.Restart();
                    Log.Error($"[{Name}.MoveTowards] ExilePather.FindPath failed from {myPosition} to {position}");
                    return false;
                }
                sw.Restart();
            }

            
            while (_cmd.Path.Count > 1)
            {
                if  (ExilePather.PathDistance(_cmd.Path[0], myPosition) < settings.MinMoveDistance)
                {
                    //Log.Debug($"[{Name}] Deleting first point");
                    _cmd.Path.RemoveAt(0);
                } else
                {
                    break;
                }
            }


            //Log.Debug($"[{Name}] Move Range: {settings.MinMoveDistance}");
            var point = _cmd.Path[0];

            if (settings.RandomizeMove)
            {
                Log.Debug($"[{Name}] Randomize movement enabled");
                point += new Vector2i(LokiPoe.Random.Next(-2, 2), LokiPoe.Random.Next(-2, 2));
                if (ExilePather.PathExistsBetween(myPosition, point))
                {
                    _cmd.Path[0] = point;
                }
                else
                {
                    point = _cmd.Path[0] + new Vector2i(LokiPoe.Random.Next(-1, 1), LokiPoe.Random.Next(-1, 1));
                    if (ExilePather.PathExistsBetween(myPosition, point))
                    {
                        _cmd.Path[0] = point;
                    }
                }
            }


            UseMove(point);
            return true;
            //return BasicMove(myPosition, point);
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public void Tick()
        {
            if (!LokiPoe.IsInGame)
                return;
        }

        public override string ToString() => $"{Name}: {Description}";
        public void UseMove(Vector2i point)
        {
            if (moveSkillOnPanel == null || moveSkillOnPanelKey == System.Windows.Forms.Keys.None)
            {
                Log.Debug($"[{Name}] Move skill on panel is NULL. Clearing slot 4");
                LokiPoe.InGameState.SkillBarHud.ClearSlot(4);
                var moveSkill = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(x => x.InternalName == "Move");
                Log.Debug($"[{Name}] Move skill on panel is NULL. Setting move to slot 4");
                LokiPoe.InGameState.SkillBarHud.SetSlot(4, moveSkill);
                Wait.LatencySleep();
                moveSkillOnPanel = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill;
                Wait.LatencySleep();
                moveSkillOnPanelKey = moveSkillOnPanel.BoundKey;
                moveSkillOnPanelSlot = moveSkillOnPanel.Slot;
                Log.Debug($"[{Name}] Move skill key is now {moveSkillOnPanelKey}");
            }
            else
            {
                if (!LokiPoe.ProcessHookManager.IsKeyDown(moveSkillOnPanelKey))
                {
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    LokiPoe.InGameState.SkillBarHud.BeginUseAt(moveSkillOnPanelSlot, false, point);
                }
                else
                {
                    MouseManager.SetMousePos("cMover.UseMove", point, false);
                }
            }
        }

        public void ClearLMB()
        {
            for (int i = 1; i < 3; i++)
            {
                if (i == 1 && LokiPoe.InGameState.SkillBarHud.Slot(1).InternalName != "Melee")
                {
                    LokiPoe.InGameState.SkillBarHud.ClearSlot(i);
                    Log.Debug($"[{Name}] Clear slot {i}");
                }
                if (LokiPoe.InGameState.SkillBarHud.Slot(i) != null && LokiPoe.InGameState.SkillBarHud.Slot(i).InternalName == "Move")
                {
                    LokiPoe.InGameState.SkillBarHud.ClearSlot(i);
                    Log.Debug($"[{Name}] Clear slot {i}");
                }
            }

            Log.Debug($"[{Name}] LMB is clear");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DPBDevHelper;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace cFollower
{
    public class CombatTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private HashSet<PlayerInfo> playerList = new HashSet<PlayerInfo>();
        private HashSet<Monster> monsterList = new HashSet<Monster>();
        private HashSet<Monster> filteredMonsterList = new HashSet<Monster>();
        private IRoutine currentRoutine;

        public void Tick()
        {
            if (monsterList.Count > 0)
            {
                filteredMonsterList = FilterMonsterList(monsterList);
            }

            if (filteredMonsterList.Count > 0)
            {
                ClearInvalidMonsters(filteredMonsterList);
            }
        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame)
            {
                return false;
            }

            //if (filteredMonsterList.Count == 0)
            //{
            //    return false;
            //}

            LogicResult combatResult = await currentRoutine.Logic(new Logic(Enums.LogicType.FollowerCombat.ToString(), this, filteredMonsterList, playerList));
            return combatResult != LogicResult.Provided;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Enums.MessageType.PlayerListUpdate.ToString())
            {
                //Log.Debug("[CombatTask] Message with monster list received");

                if (message.TryGetInput<HashSet<PlayerInfo>>(0, out playerList))
                {
                    //Log.Debug($"[CombatTask] Monster list processed, count {monsterList.Count}");
                    return MessageResult.Processed;
                };
            }

            if (message.Id == Enums.MessageType.MonsterListUpdate.ToString())
            {
                //Log.Debug("[CombatTask] Message with monster list received");

                if (message.TryGetInput<HashSet<Monster>>(0, out monsterList))
                {
                    //Log.Debug($"[CombatTask] Monster list processed, count {monsterList.Count}");
                    return MessageResult.Processed;
                };
            }

            return MessageResult.Processed;
        }

        private void ClearInvalidMonsters(HashSet<Monster> monsterList)
        {
            foreach (Monster monster in new HashSet<Monster>(monsterList))
            {
                if (!CombatHelper.ValidateMonster(monster) || !CombatHelper.FilterMonster(monster, 40))
                {
                    monsterList.Remove(monster);
                }
            }
        }

        private HashSet<Monster> FilterMonsterList(HashSet<Monster> sourceList)
        {
            HashSet<Monster> resultMonsterList = new HashSet<Monster>();

            foreach (Monster monster in sourceList)
            {
                if (!CombatHelper.ValidateMonster(monster))
                {
                    continue;
                };

                if (monster.IsImprisoned)
                {
                    continue;
                }

                if (CombatHelper.FilterMonster(monster, 40))
                {
                    resultMonsterList.Add(monster);
                }
            }

            return resultMonsterList;
        }

        #region skip

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await Task.FromResult(LogicResult.Unprovided);

        }

        public string Name => "Combat Task";
        public string Description => "Template task";
        public string Author => "chewbacca";
        public string Version => "0.0.0.1";

        public void Start()
        {
            Log.Info($"[{Name}] Task Loaded.");
            currentRoutine = RoutineManager.Current;
        }

        public void Stop()
        {
        }

        #endregion skip
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace cFollower
{
    public class ZoneHelper
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public static bool IsLeaderInHideout()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsHideoutArea;
            }

            return false;
        }

        public static bool IsLeaderOnMap()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsMap;
            }

            return false;
        }

        public static bool IsLeaderInCombatArea()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsCombatArea;
            }

            return false;
        }

        public static bool IsLeaderInLab()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsLabyrinthArea;
            }

            return false;
        }

        public static bool IsLeaderInTown()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return leader.PlayerEntry.Area.IsTown;
            }

            return false;
        }

        public static bool IsInSameZoneWithLeader()
        {
            var leader = Utility.GetLeaderPartyMember();
            if (leader != null)
            {
                return LokiPoe.InGameState.PartyHud.IsInSameZone(leader.PlayerEntry.Name);
            }

            return false;
        }
    }
}

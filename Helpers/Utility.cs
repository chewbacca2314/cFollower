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
    public class Utility
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public static PartyMember GetLeaderPartyMember()
        {
            return LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.PlayerEntry.Name == cFollowerSettings.Instance.LeaderName);
        }
        public static Player GetLeaderPlayer()
        {
            return LokiPoe.ObjectManager.GetObjectsByType<Player>().FirstOrDefault(x => x.Name == cFollowerSettings.Instance.LeaderName);
        }

        public static async Task<bool> OpenSocialPanel()
        {
            var key = LokiPoe.Input.Binding.open_social_panel;
            LokiPoe.Input.SimulateKeyEvent(key);
            await Wait.LatencySleep();
            LokiPoe.Input.SimulateKeyEvent(key, false);

            return LokiPoe.InGameState.SocialUi.IsOpened;
        }

        public static async Task<bool> CloseSocialPanel()
        {
            if (LokiPoe.InGameState.SocialUi.IsOpened)
            {
                var closeAllWindowKey = LokiPoe.Input.Binding.close_panels;
                LokiPoe.Input.SimulateKeyEvent(closeAllWindowKey);
                await Wait.LatencySleep();
                LokiPoe.Input.SimulateKeyEvent(closeAllWindowKey, false);
            }

            return LokiPoe.InGameState.SocialUi.IsOpened;
        }

        public static void AddObstacle(NetworkObject obj)
        {
            var renderComponent = obj.Components.RenderComponent;
            var num = 10.86957f;
            var val = renderComponent.InteractSize.X / num;
            var val2 = renderComponent.InteractSize.Y / num;
            var radius = 1.75f * Math.Max(val, val2);
            ExilePather.PolyPathfinder.AddObstacle(obj.Position, radius);
            ExilePather.PolyPathfinder.UpdateObstacles();
        }

        public static void RemoveObstacle(NetworkObject obj)
        {
            ExilePather.PolyPathfinder.RemoveObstacle(obj.Position);
            ExilePather.PolyPathfinder.UpdateObstacles();
        }

        public static Dictionary<string, string> TransitionCheckAreas = new Dictionary<string, string>
        {
            { "Sepulchre", "Arena" },
            { "Steaming Spring", "Metadata/MiscellaneousObjects/BossForceFieldDoor"}
        };

        public static Regex CreateRegex(string tgtName)
        {
            return new Regex(Regex.Escape(tgtName).Replace(@"\?", "[0-9]"));
        }


        public static List<Vector2i> FindAllTgts(string tgtName)
        {
            var regex = CreateRegex(tgtName);
            var positions = new List<Vector2i>();
            var tgtEntries = LokiPoe.TerrainData.TgtEntries;
            for (int i = 0; i < tgtEntries.GetLength(0); i++)
            {
                for (int j = 0; j < tgtEntries.GetLength(1); j++)
                {
                    var entry = tgtEntries[i, j];
                    if (entry == null)
                        continue;
                    if (regex.IsMatch(entry.TgtName))
                    {
                        positions.Add(new Vector2i(i * 23, j * 23));
                    }
                }
            }
            return positions;
        }

        public static Vector2i ValidTgt(List<Vector2i> tgts)
        {
            return tgts.Where(x => ExilePather.IsWalkable(x)).OrderBy(x => x.Distance(LokiPoe.MyPosition)).FirstOrDefault();
        }

        public static async Task<bool> MovePosition(Vector2i pos)
        {
            var fastWalkable = ExilePather.FastWalkablePositionFor(pos);
            var fastWalkableDist = fastWalkable.Distance(LokiPoe.MyPosition);
            while (fastWalkableDist < 30)
            {
                PlayerMoverManager.Current.MoveTowards(fastWalkable);
            }

            await Wait.SleepSafe(50, 60);

            return fastWalkableDist < 30;
        }

        public static List<string> ParseByDivider(string _str, char divider)
        {
            return _str.Split(divider).ToList();
        }
    }
}

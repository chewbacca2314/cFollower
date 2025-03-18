using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using log4net;

namespace cFollower
{
    public static class Wait
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public static async Task Sleep(int ms)
        {
            await Coroutine.Sleep(ms);
        }

        public static async Task SleepSafe(int ms)
        {
            await Coroutine.Sleep(Math.Max(LatencyTracker.Current, ms));
        }

        public static async Task SleepSafe(int min, int max)
        {
            int latency = LatencyTracker.Current;
            if (latency > max)
            {
                await Coroutine.Sleep(latency);
            }
            else
            {
                await Coroutine.Sleep(LokiPoe.Random.Next(min, max + 1));
            }
        }

        public static async Task LatencySleep()
        {
            var ms = Math.Max((int)(LatencyTracker.Current * 1.15), 25);
            Log.Debug($"[LatencySleep] {ms} ms.");
            await Coroutine.Sleep(ms);
        }

        public static async Task<bool> For(Func<bool> condition, string desc, int step = 100, int timeout = 3000)
        {
            return await For(condition, desc, () => step, timeout);
        }
        private static async Task<bool> For(Func<bool> condition, string desc, Func<int> step, int timeout = 3000)
        {
            if (condition())
                return true;

            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < timeout)
            {
                Log.Debug($"[WaitFor] Waiting for {desc} ({Math.Round(timer.ElapsedMilliseconds / 100f, 2)}/{timeout / 100f})");
                if (condition())
                    return true;
            }
            Log.Error($"[WaitFor] Wait for {desc} timeout.");
            return false;
        }
    }
}

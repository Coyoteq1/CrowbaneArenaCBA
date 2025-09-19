using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Collections;
using Unity.Entities;
using ProjectM;

namespace CrowbanePackPlugin.Services
{
    internal static class PlayerMetricsService
    {
        static bool _started;
        static readonly object _lock = new object();

        static string EventsPath => Path.Combine(global::CrowbanePackPlugin.CrowbanePackPlugin.ConfigPath, "player_events.jsonl");
        static string StatusPath => Path.Combine(global::CrowbanePackPlugin.CrowbanePackPlugin.ConfigPath, "player_status.json");

        public static void TryStart()
        {
            if (_started) return;
            _started = true;
            try { Directory.CreateDirectory(global::CrowbanePackPlugin.CrowbanePackPlugin.ConfigPath); } catch { }
            try { Core.StartCoroutine(Ticker()); } catch { _started = false; }
        }

        static IEnumerator Ticker()
        {
            var wait = new UnityEngine.WaitForSecondsRealtime(30f);
            while (true)
            {
                try { WriteStatus(); } catch { }
                yield return wait;
            }
        }

        public static void AppendEvent(string kind, string name, int online)
        {
            try
            {
                var rec = new Dictionary<string, object>
                {
                    ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["event"] = kind,
                    ["name"] = name,
                    ["online"] = online
                };
                var line = JsonSerializer.Serialize(rec);
                lock (_lock)
                {
                    File.AppendAllText(EventsPath, line + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void WriteStatus()
        {
            try
            {
                var names = new List<string>();
                int total = 0;
                foreach (var user in Core.Players.GetCachedUsersOnline())
                {
                    try
                    {
                        total++;
                        if (Core.EntityManager.HasComponent<User>(user))
                        {
                            var u = Core.EntityManager.GetComponentData<User>(user);
                            if (!u.CharacterName.IsEmpty) names.Add(u.CharacterName.ToString());
                        }
                    }
                    catch { }
                }
                var status = new Dictionary<string, object>
                {
                    ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["total"] = total,
                    ["names"] = names
                };
                var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
                lock (_lock)
                {
                    File.WriteAllText(StatusPath, json);
                }
            }
            catch { }
        }
    }
}

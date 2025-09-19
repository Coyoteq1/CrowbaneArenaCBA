using System;
using System.IO;
using System.Text.Json;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Collections;

namespace CrowbanePackPlugin.Services
{
    internal static class KillfeedService
    {
        static readonly object _fileLock = new object();

        static string KillfeedFilePath
        {
            get
            {
                try
                {
                    var dir = CrowbanePackPlugin.ConfigPath;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    return Path.Combine(dir, "killfeed.jsonl");
                }
                catch { return "killfeed.jsonl"; }
            }
        }

        static void AppendKillRecord(string killer, string victim, StatChangeReason reason)
        {
            try
            {
                var rec = new
                {
                    ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    killer,
                    victim,
                    reason = reason.ToString()
                };
                var line = JsonSerializer.Serialize(rec);
                lock (_fileLock)
                {
                    File.AppendAllText(KillfeedFilePath, line + Environment.NewLine);
                }
            }
            catch { }
        }
        static string GetEntityName(EntityManager em, Entity e)
        {
            try
            {
                if (em.Exists(e))
                {
                    if (em.HasComponent<PlayerCharacter>(e))
                    {
                        var user = em.GetComponentData<PlayerCharacter>(e).UserEntity;
                        if (em.Exists(user) && em.HasComponent<User>(user))
                        {
                            var u = em.GetComponentData<User>(user);
                            if (!u.CharacterName.IsEmpty) return u.CharacterName.ToString();
                        }
                    }
                    // Fallback to prefab name
                    var name = Helper.GetPrefabGUID(e).LookupName();
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
            }
            catch { }
            return "Unknown";
        }

        public static void BroadcastKill(EntityManager em, Entity victim, Entity killer, StatChangeReason reason)
        {
            var s = CrowbanePackPlugin.Settings;
            if (s == null || !s.EnableKillfeed) return;
            if (s.UseExternalKillfeed && IntegrationService.BestKillfeedPresent) return; // defer to external

            var victimName = GetEntityName(em, victim);
            var killerName = GetEntityName(em, killer);
            var color = string.IsNullOrWhiteSpace(s.KillfeedColor) ? "#C94F4F" : s.KillfeedColor;

            FixedString512Bytes message = $"<color={color}>[Kill]</color> {killerName} -> {victimName}";
            try { ServerChatUtils.SendSystemMessageToAllClients(em, ref message); } catch { }

            try
            {
                if (s.WriteKillfeedFile)
                {
                    AppendKillRecord(killerName, victimName, reason);
                }
            }
            catch { }
        }
    }
}

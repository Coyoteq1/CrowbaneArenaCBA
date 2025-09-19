using System;
using System.Linq;

namespace CrowbanePackPlugin.Services
{
    internal static class IntegrationService
    {
        public static bool BestKillfeedPresent { get; private set; }
        public static bool VRolePresent { get; private set; }
        public static bool BloodyBossPresent { get; private set; }
        public static bool PenumbraPresent { get; private set; }

        public static void DetectAndLog()
        {
            try
            {
                // Resolve Chainloader via reflection to avoid hard dependency
                var list = new System.Collections.Generic.List<object>();
                try
                {
                    var t = Type.GetType("BepInEx.Bootstrap.Chainloader, BepInEx.Core")
                            ?? Type.GetType("BepInEx.Bootstrap.Chainloader, BepInEx.Unity.IL2CPP");
                    if (t != null)
                    {
                        var prop = t.GetProperty("PluginInfos", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var dict = prop?.GetValue(null) as System.Collections.IDictionary;
                        if (dict != null)
                        {
                            foreach (System.Collections.DictionaryEntry de in dict)
                            {
                                var pi = de.Value;
                                list.Add(pi);
                            }
                        }
                    }
                }
                catch { }
                bool Contains(string s, string needle) => s?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

                bool Match(dynamic pi, string term) {
                    try { string n = pi.Metadata.Name; if (Contains(n, term)) return true; } catch { }
                    try { string g = pi.Metadata.GUID; if (Contains(g, term)) return true; } catch { }
                    return false;
                }
                BestKillfeedPresent = list.Any(pi => Match(pi, "killfeed"));
                VRolePresent        = list.Any(pi => Match(pi, "vrole") || Match(pi, "role"));
                BloodyBossPresent   = list.Any(pi => Match(pi, "bloodyboss") || Match(pi, "bloody"));
                PenumbraPresent     = list.Any(pi => Match(pi, "penumbra"));

                var log = Core.Log;
                log.LogInfo($"Integration scan: BestKillfeed={BestKillfeedPresent}, vrole={VRolePresent}, BloodyBoss={BloodyBossPresent}, Penumbra={PenumbraPresent}");
            }
            catch (Exception e)
            {
                Core.Log.LogWarning($"Integration detection failed: {e.Message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CrowbanePackPlugin.Data;
using CrowbanePackPlugin.Helpers;
using ProjectM;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrowbanePackPlugin.Services
{
    internal class ArenaZoneService
    {
        readonly Dictionary<string, ArenaData> _arenas = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<Entity, string> _inside = new();

        public ArenaZoneService()
        {
            LoadArenas();
            Core.StartCoroutine(WatchLoop());
        }

        void LoadArenas()
        {
            try
            {
                _arenas.Clear();
                if (File.Exists(CrowbanePackPlugin.ArenasConfigPath))
                {
                    var json = File.ReadAllText(CrowbanePackPlugin.ArenasConfigPath);
                    var arenas = JsonSerializer.Deserialize<Dictionary<string, ArenaData>>(json);
                    if (arenas != null)
                    {
                        foreach (var kv in arenas) _arenas[kv.Key] = kv.Value;
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogWarning($"ArenaZoneService: failed to load arenas.json: {e.Message}");
            }
        }

        System.Collections.IEnumerator WatchLoop()
        {
            var wait = new WaitForSecondsRealtime(0.5f);
            while (true)
            {
                try
                {
                    Tick();
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"ArenaZoneService tick error: {e.Message}");
                }
                yield return wait;
            }
        }

        void Tick()
        {
            Core.EnsureInitialized();
            var em = Core.EntityManager;

            // Build a snapshot of players in world
            var players = Helper.GetEntitiesByComponentType<PlayerCharacter>(includeDisabled: true);

            // Track who remains inside after this tick
            var stillInside = new HashSet<Entity>();

            foreach (var character in players)
            {
                if (!em.Exists(character)) continue;
                float3 pos;
                try { pos = em.GetComponentData<Unity.Transforms.LocalToWorld>(character).Position; }
                catch { continue; }

                // Determine if inside any arena
                string inArena = null;
                foreach (var kv in _arenas)
                {
                    var a = kv.Value;
                    var center = new float2(a.Position.X, a.Position.Z);
                    var p2 = new float2(pos.x, pos.z);
                    if (math.distance(center, p2) <= a.Radius)
                    {
                        inArena = kv.Key;
                        break;
                    }
                }

                if (inArena != null)
                {
                    stillInside.Add(character);
                    if (!_inside.ContainsKey(character))
                    {
                        // Entered an arena
                        OnEnterArena(character, inArena);
                        _inside[character] = inArena;
                    }
                }
            }

            players.Dispose();

            // Handle exits
            var toRemove = new List<Entity>();
            foreach (var kv in _inside)
            {
                if (!stillInside.Contains(kv.Key))
                {
                    OnExitArena(kv.Key, kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var ch in toRemove) _inside.Remove(ch);
        }

        void OnEnterArena(Entity character, string arenaName)
        {
            try
            {
                var em = Core.EntityManager;
                var user = em.GetComponentData<PlayerCharacter>(character).UserEntity;

                // Save session state
                var pos = PluginHelpers.GetCharacterPosition(em, character);
                var previousKit = CrowbanePackPlugin.GetLastAppliedKit(character);
                var session = new ArenaSession
                {
                    ArenaName = arenaName,
                    ReturnPosition = SerializableVector3.From(pos),
                    PreviousKit = previousKit,
                    SpellsUnlocked = false
                };
                session.ProgressionSnapshot = PluginHelpers.SnapshotUnlockedProgression(em, user);
                CrowbanePackPlugin.StartArenaSession(character, session);

                // Unlock spells and give max arena gear
                PluginHelpers.UnlockAllForArena(em, user, character);
                session.SpellsUnlocked = true;
                PluginHelpers.GiveArenaMaxGear(em, character, user);

                // Apply requested max stats while inside arena: attack speed, spell power, movement speed
                Core.BoostedPlayerService.SetAttackSpeedMultiplier(character, 5f);
                Core.BoostedPlayerService.SetDamageBoost(character, 10000f); // also boosts SpellPower via damageBuffs
                Core.BoostedPlayerService.SetSpeedBoost(character, 15f);
                Core.BoostedPlayerService.UpdateBoostedPlayer(character);
            }
            catch (Exception e)
            {
                Core.Log.LogError($"OnEnterArena failure: {e.Message}");
            }
        }

        void OnExitArena(Entity character, string arenaName)
        {
            try
            {
                var em = Core.EntityManager;
                var user = em.GetComponentData<PlayerCharacter>(character).UserEntity;
                var session = CrowbanePackPlugin.EndArenaSession(character);
                if (session == null) return;

                // Restore previous kit if captured
                if (session.PreviousKit.HasValue)
                {
                    PluginHelpers.ApplyKit(em, character, session.PreviousKit.Value, silent: true);
                }

                if (session.SpellsUnlocked && session.ProgressionSnapshot != null)
                {
                    PluginHelpers.RestoreUnlockedProgression(em, user, session.ProgressionSnapshot);
                }

                // Remove arena granted items if still in inventory (not equipped)
                if (session.GrantedItemPrefabIds != null)
                {
                    PluginHelpers.RemoveGrantedArenaItems(em, character, session.GrantedItemPrefabIds);
                }

                // Clear arena stat boosts
                Core.BoostedPlayerService.RemoveAttackSpeedMultiplier(character);
                Core.BoostedPlayerService.RemoveDamageBoost(character);
                Core.BoostedPlayerService.RemoveSpeedBoost(character);
                Core.BoostedPlayerService.UpdateBoostedPlayer(character);
            }
            catch (Exception e)
            {
                Core.Log.LogError($"OnExitArena failure: {e.Message}");
            }
        }
    }
}

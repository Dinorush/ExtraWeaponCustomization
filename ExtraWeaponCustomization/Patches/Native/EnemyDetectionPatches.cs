using System.Collections.Generic;
using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon;
using Player;
using Agents;
using System.Linq;
using System;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime.Runtime;
using GTFO.API;
using EWC.API;
using EWC.Attributes;

namespace EWC.Patches.Native
{
    internal static class EnemyDetectionPatches
    {
        private static INativeDetour? DetectOnNoiseDetour;
        private static d_DetectOnNoise? orig_DetectOnNoise;
        private unsafe delegate void d_DetectOnNoise(IntPtr _this, IntPtr agentTarget, float movementDetectionDistance, float shootDetectionDistance, float delta, out float output, Il2CppMethodInfo* methodInfo);

        // Can't harmony patch the function due to out parameter so need a native detour
        [InvokeOnLoad]
        private unsafe static void ApplyNativePatch()
        {
            DetectOnNoiseDetour = INativeDetour.CreateAndApply(
                (nint)Il2CppAPI.GetIl2CppMethod<EnemyDetection>(
                    nameof(EnemyDetection.DetectOnNoiseDistance_Conditional_AnimatedWindow),
                    typeof(void).Name,
                    false,
                    new[] {
                        nameof(AgentTarget),
                        typeof(float).Name,
                        typeof(float).Name,
                        typeof(float).Name,
                        typeof(float).MakeByRefType().Name
                    }),
                DetectOnNoisePatch,
                out orig_DetectOnNoise
                );
            NativePatchAPI.AddDetectPostfix(Post_DetectAgentNoise);
        }

        private unsafe static void DetectOnNoisePatch(IntPtr _this, IntPtr agentTarget, float movementDetectionDistance, float shootDetectionDistance, float delta, out float output, Il2CppMethodInfo* methodInfo)
        {
            output = 0f;
            EnemyDetection detection = new(_this);
            AgentTarget target = new(agentTarget);
            bool runOrig = NativePatchAPI.RunDetectPrefix(detection, target, movementDetectionDistance, shootDetectionDistance, delta, ref output);
            if (runOrig)
                orig_DetectOnNoise!(_this, agentTarget, movementDetectionDistance, shootDetectionDistance, delta, out output, methodInfo);
            NativePatchAPI.RunDetectPostfix(detection, target, movementDetectionDistance, shootDetectionDistance, delta, ref output);
        }

        struct CWCHolder
        {
            public CustomWeaponComponent? primary;
            public bool hasPrimary;
            public CustomWeaponComponent? special;
            public bool hasSpecial;

            public readonly bool IsValid => primary != null == hasPrimary && special != null == hasSpecial;
        }
        // Only applies to guns atm, so no need for 3 weapon support (switch to list if ever needed)
        private static readonly Dictionary<int, CWCHolder> s_cachedCWCs = new();

        private static void UpdateCache()
        {
            if (s_cachedCWCs.Count != PlayerManager.PlayerAgentsInLevel.Count
             || s_cachedCWCs.Values.Any(holder => !holder.IsValid))
            {
                s_cachedCWCs.Clear();
                foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
                {
                    if (!PlayerBackpackManager.TryGetBackpack(player.Owner, out var backpack)) continue;

                    if (!backpack.TryGetBackpackItem(InventorySlot.GearStandard, out var primary)
                     || !backpack.TryGetBackpackItem(InventorySlot.GearSpecial, out var special)) continue;

                    CustomWeaponComponent? primaryCWC = primary.Instance?.GetComponent<CustomWeaponComponent>();
                    CustomWeaponComponent? specialCWC = special.Instance?.GetComponent<CustomWeaponComponent>();
                    s_cachedCWCs.Add(player.GlobalID, new CWCHolder()
                    {
                        primary = primaryCWC,
                        hasPrimary = primaryCWC != null,
                        special = specialCWC,
                        hasSpecial = specialCWC != null
                    });
                }
            }
        }

        private static void Post_DetectAgentNoise(EnemyDetection __instance, AgentTarget agentTarget, float _mv, float _wp, float _delta, ref float output)
        {
            UpdateCache();
            if (!s_cachedCWCs.TryGetValue(agentTarget.m_globalID, out var CWCs)
             || !CWCs.hasPrimary && !CWCs.hasSpecial) return;

            WeaponStealthUpdateContext context = new(__instance.m_ai.m_enemyAgent, __instance.m_noiseDetectionOn, output);
            CWCs.primary?.Invoke(context);
            CWCs.special?.Invoke(context);
            output = context.Output;
        }
    }
}

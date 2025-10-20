using Agents;
using EWC.CustomWeapon;
using HarmonyLib;
using Player;

namespace EWC.Patches.Player
{
    [HarmonyPatch]
    internal static class PlayerBackpackPatches
    {
        [HarmonyPatch(typeof(PlayerBackpack), nameof(PlayerBackpack.SpawnAndEquipGearAsync))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SpawnGearCallback(PlayerBackpack __instance, InventorySlot slot)
        {
            var playerAgent = __instance.Owner.HasPlayerAgent ? __instance.Owner.PlayerAgent.Cast<PlayerAgent>() : null;
            if (playerAgent == null || !__instance.TryGetBackpackItem(slot, out var bpItem)) return;

            CustomWeaponManager.AddEquippedItem(playerAgent, bpItem.Instance.Cast<ItemEquippable>());
        }

        [HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SpawnPlayerCallback(PlayerSync __instance)
        {
            var agent = __instance.m_agent;
            if (!PlayerBackpackManager.TryGetBackpack(agent.Owner, out var backpack)) return;

            foreach (var slot in CustomDataManager.ValidSlots)
            {
                if (!backpack.TryGetBackpackItem(slot, out var bpItem)) continue;

                CustomWeaponManager.AddEquippedItem(agent, bpItem.Instance.Cast<ItemEquippable>());
            }
        }
    }
}

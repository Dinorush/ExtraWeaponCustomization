using EWC.API.Accuracy;
using EWC.CustomWeapon.CustomShot;
using EWC.Utils.Extensions;
using Player;
using System;
using System.Diagnostics.CodeAnalysis;

namespace EWC.API
{
    public class AccuracyStats
    {
        public readonly WeaponAccuracy Main;
        public readonly WeaponAccuracy Special;
        public readonly WeaponAccuracy Tool;
        // Always has a player.
        public PlayerAgent Owner { get; internal set; }

        public WeaponAccuracy this[AmmoType ammoType] => this[ammoType.ToInventorySlot()];
        public WeaponAccuracy this[InventorySlot slot] => slot switch
            {
                InventorySlot.GearStandard => Main,
                InventorySlot.GearSpecial => Special,
                InventorySlot.GearClass => Tool,
                _ => throw new ArgumentException($"Cannot get accuracy stats for unsupported inventory slot {slot}!")
            };

        public AccuracyStats(PlayerAgent owner)
        {
            Owner = owner;
            Main = new(InventorySlot.GearStandard, this);
            Special = new(InventorySlot.GearSpecial, this);
            Tool = new(InventorySlot.GearClass, this);
        }
    }

    public static class AccuracyAPI
    {
        public delegate void AccuracyCallback(WeaponAccuracy stats, WeaponDelta delta);

        public static event Action? OnAccuracyReset;
        public static event AccuracyCallback? OnLocalAccuracyUpdate;
        public static event AccuracyCallback? OnAccuracyUpdate;

        public static bool TryGetStats(ulong playerLookup, [MaybeNullWhen(false)] out AccuracyStats stats) => AccuracyManager.TryGetStats(playerLookup, out stats);

        internal static void InvokeAccuracyUpdate(WeaponAccuracy stats, WeaponDelta delta)
        {
            if (stats.ParentStats.Owner.IsLocallyOwned)
                OnLocalAccuracyUpdate?.Invoke(stats, delta);
            OnAccuracyUpdate?.Invoke(stats, delta);
        }

        internal static void InvokeAccuracyReset() => OnAccuracyReset?.Invoke();
    }
}

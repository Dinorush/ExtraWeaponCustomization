using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.CustomShot;
using System;
using System.Diagnostics.CodeAnalysis;

namespace EWC.API
{
    public class AccuracyStats
    {
        public class StatInfo
        {
            public int Hits { get; internal set; }
            public int Crits { get; internal set; }
            public int Count { get; internal set; }

            public StatInfo() { }
            public StatInfo(StatInfo statInfo)
            {
                Hits = statInfo.Hits;
                Crits = statInfo.Crits;
                Count = statInfo.Count;
            }
        }

        public readonly StatInfo Shots;
        public readonly StatInfo FullShots;
        public readonly StatInfo Groups;
        // Always has a player.
        public readonly IOwnerComp Owner;

        public AccuracyStats(IOwnerComp owner)
        {
            Owner = owner;
            Shots = new();
            FullShots = new();
            Groups = new();
        }

        public AccuracyStats(AccuracyStats stats)
        {
            Owner = stats.Owner;
            Shots = new(stats.Shots);
            FullShots = new(stats.FullShots);
            Groups = new(stats.Groups);
        }
    }

    public static class AccuracyAPI
    {
        public delegate void AccuracyCallback(AccuracyStats stats);

        public static event Action? OnAccuracyReset;
        public static event AccuracyCallback? OnLocalAccuracyUpdate;
        public static event AccuracyCallback? OnAccuracyUpdate;

        public static bool TryGetStats(ulong playerLookup, [MaybeNullWhen(false)] out AccuracyStats stats) => AccuracyManager.TryGetStats(playerLookup, out stats);
        public static bool TryGetSentryStats(ulong playerLookup, [MaybeNullWhen(false)] out AccuracyStats stats) => AccuracyManager.TryGetSentryStats(playerLookup, out stats);

        internal static void InvokeAccuracyUpdate(AccuracyStats stats)
        {
            if (stats.Owner.IsType(CustomWeapon.Enums.OwnerType.Local))
                OnLocalAccuracyUpdate?.Invoke(stats);
            OnAccuracyUpdate?.Invoke(stats);
        }

        internal static void InvokeAccuracyReset() => OnAccuracyReset?.Invoke();
    }
}

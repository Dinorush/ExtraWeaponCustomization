using EWC.API;
using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.Enums;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.CustomShot
{
    internal static class AccuracyManager
    {
        struct Stats
        {
            public bool hasHit;
            public bool hasCrit;

            public Stats()
            {
                hasHit = false;
                hasCrit = false;
            }
        }

        private const int FullCacheLen = 4096;
        private const int CacheLen = 2048;

        private static readonly AccuracyStats[] _fullCache = new AccuracyStats[4096];
        private static readonly Stats[] _cache = new Stats[2048];
        private static readonly Stats[] _groupCache = new Stats[2048];
        private static uint _lastOrigin = 0;
        private static uint _lastGroup = 0;

        private static readonly Dictionary<ulong, AccuracyStats> _stats = new();
        private static readonly Dictionary<ulong, AccuracyStats> _sentryStats = new();
        private static readonly Dictionary<ulong, AccuracyStats> _checkpointStats = new();
        private static readonly Dictionary<ulong, AccuracyStats> _checkpointSentryStats = new();
        private const DamageType ValidTypes = DamageType.Enemy | DamageType.Object;
        private const DamageType BlacklistTypes = DamageType.DOT;

        [InvokeOnBuildDone]
        private static void OnBuildDone()
        {
            _stats.Clear();
            _sentryStats.Clear();
            _checkpointStats.Clear();
            _checkpointSentryStats.Clear();
            AccuracyAPI.InvokeAccuracyReset();
        }

        [InvokeOnCheckpointReached]
        private static void OnCheckpointReached()
        {
            _checkpointStats.Clear();
            foreach ((ulong lookup, var stats) in _stats)
                _checkpointStats.Add(lookup, new(stats));
            _checkpointSentryStats.Clear();
            foreach ((ulong lookup, var stats) in _sentryStats)
                _checkpointSentryStats.Add(lookup, new(stats));
        }

        [InvokeOnCheckpointReloaded]
        private static void OnCheckpointReloaded()
        {
            _stats.Clear();
            foreach ((ulong lookup, var stats) in _checkpointStats)
            {
                _stats.Add(lookup, new(stats));
                AccuracyAPI.InvokeAccuracyUpdate(stats);
            }
            _sentryStats.Clear();
            foreach ((ulong lookup, var stats) in _checkpointSentryStats)
            {
                _sentryStats.Add(lookup, new(stats));
                AccuracyAPI.InvokeAccuracyUpdate(stats);
            }
        }

        public static bool TryGetStats(ulong playerLookup, [MaybeNullWhen(false)] out AccuracyStats stats) => _stats.TryGetValue(playerLookup, out stats);
        public static bool TryGetSentryStats(ulong playerLookup, [MaybeNullWhen(false)] out AccuracyStats stats) => _sentryStats.TryGetValue(playerLookup, out stats);

        public static void InitShot(IOwnerComp owner, (uint id, uint originID, uint groupID) idSet)
        {
            if (owner.Player == null || idSet.id == 0) return;

            var statsDict = owner.IsType(OwnerType.Sentry) ? _sentryStats : _stats;
            var lookup = owner.Player.Owner.Lookup;
            if (!statsDict.TryGetValue(lookup, out var stats))
                statsDict.Add(lookup, stats = new(owner));

            _fullCache[idSet.id % FullCacheLen] = stats;
            stats.FullShots.Count++;
            if (idSet.originID > _lastOrigin)
            {
                _lastOrigin = idSet.originID;
                _cache[idSet.originID % CacheLen] = new();
                stats.Shots.Count++;
            }
            if (idSet.groupID > _lastGroup)
            {
                _lastGroup = idSet.groupID;
                _groupCache[idSet.groupID % CacheLen] = new();
                stats.Groups.Count++;
            }

            AccuracyAPI.InvokeAccuracyUpdate(stats);
        }

        public static void AddHit(DamageType damageType, ShotInfo.Const shotInfo)
        {
            if (!damageType.HasAnyFlag(ValidTypes) || damageType.HasAnyFlag(BlacklistTypes) || shotInfo.ID == 0) return;

            bool crit = damageType.HasFlag(DamageType.Weakspot);
            var stats = _fullCache[shotInfo.ID % FullCacheLen];
            stats.FullShots.Hits++;
            if (crit)
                stats.FullShots.Crits++;

            var index = shotInfo.OriginID % CacheLen;
            if (!_cache[index].hasHit)
            {
                _cache[index].hasHit = true;
                stats.Shots.Hits++;
            }
            if (crit && !_cache[index].hasCrit)
            {
                _cache[index].hasCrit = true;
                stats.Shots.Crits++;
            }
            index = shotInfo.GroupID % CacheLen;
            if (!_groupCache[index].hasHit)
            {
                _groupCache[index].hasHit = true;
                stats.Groups.Hits++;
            }
            if (crit && !_groupCache[index].hasCrit)
            {
                _groupCache[index].hasCrit = true;
                stats.Groups.Crits++;
            }

            AccuracyAPI.InvokeAccuracyUpdate(stats);
        }
    }
}

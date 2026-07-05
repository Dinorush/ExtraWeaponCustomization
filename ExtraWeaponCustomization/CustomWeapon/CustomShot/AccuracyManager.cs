using EWC.API;
using EWC.API.Accuracy;
using EWC.Attributes;
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

        private const int CacheLen = 4096;

        private static readonly WeaponAccuracy[] _fullCache = new WeaponAccuracy[CacheLen];
        private static readonly Stats[] _cache = new Stats[CacheLen];
        private static readonly Stats[] _groupCache = new Stats[CacheLen];
        private static uint _lastOrigin = 0;
        private static uint _lastGroup = 0;

        private static readonly Dictionary<ulong, AccuracyStats> _stats = new();
        private const DamageType ValidTypes = DamageType.Enemy | DamageType.Object;
        private const DamageType BlacklistTypes = DamageType.DOT;

        [InvokeOnBuildDone]
        private static void OnBuildDone()
        {
            _stats.Clear();
            AccuracyAPI.InvokeAccuracyReset();
        }

        public static bool TryGetStats(ulong playerLookup, [MaybeNullWhen(false)] out AccuracyStats stats) => _stats.TryGetValue(playerLookup, out stats);

        public static void InitShot(CustomWeaponComponent cwc, (uint id, uint originID, uint groupID) idSet)
        {
            if (cwc.Owner.Player == null || idSet.id == 0) return;

            var lookup = cwc.Owner.Player.Owner.Lookup;
            if (!_stats.TryGetValue(lookup, out var stats))
                _stats.Add(lookup, stats = new(cwc.Owner.Player));

            var weaponInfo = stats[cwc.Weapon.InventorySlot];
            _fullCache[idSet.id % CacheLen] = weaponInfo;

            WeaponDelta delta = new();
            delta.FullShots.Count++;
            if (idSet.originID > _lastOrigin)
            {
                _lastOrigin = idSet.originID;
                _cache[idSet.originID % CacheLen] = new();
                delta.Shots.Count++;
            }
            if (idSet.groupID > _lastGroup)
            {
                _lastGroup = idSet.groupID;
                _groupCache[idSet.groupID % CacheLen] = new();
                delta.Groups.Count++;
            }

            weaponInfo.Add(delta);
            AccuracyAPI.InvokeAccuracyUpdate(weaponInfo, delta);
        }

        public static void CancelShot(CustomWeaponComponent cwc, ShotInfo shotInfo)
        {
            if (cwc.Owner.Player == null || shotInfo.ID == 0) return;

            var lookup = cwc.Owner.Player.Owner.Lookup;
            if (!_stats.TryGetValue(lookup, out var stats)) return;

            var weaponInfo = stats[cwc.Weapon.InventorySlot];
            WeaponDelta delta = new();
            delta.FullShots.Count--;
            delta.Shots.Count--;
            if (_lastGroup == shotInfo.GroupID)
            {
                delta.Groups.Count--;
                _lastGroup--;
            }

            weaponInfo.Add(delta);
            AccuracyAPI.InvokeAccuracyUpdate(weaponInfo, delta);
        }

        public static void AddHit(DamageType damageType, ShotInfo.Const shotInfo)
        {
            if (!damageType.HasAnyFlag(ValidTypes) || damageType.HasAnyFlag(BlacklistTypes) || shotInfo.ID == 0) return;

            bool crit = damageType.HasFlag(DamageType.Weakspot);
            var weaponInfo = _fullCache[shotInfo.ID % CacheLen];
            WeaponDelta delta = new();

            delta.FullShots.Hits++;
            if (crit)
                delta.FullShots.Crits++;

            var index = shotInfo.OriginID % CacheLen;
            if (!_cache[index].hasHit)
            {
                _cache[index].hasHit = true;
                delta.Shots.Hits++;
            }
            if (crit && !_cache[index].hasCrit)
            {
                _cache[index].hasCrit = true;
                delta.Shots.Crits++;
            }
            index = shotInfo.GroupID % CacheLen;
            if (!_groupCache[index].hasHit)
            {
                _groupCache[index].hasHit = true;
                delta.Groups.Hits++;
            }
            if (crit && !_groupCache[index].hasCrit)
            {
                _groupCache[index].hasCrit = true;
                delta.Groups.Crits++;
            }

            weaponInfo.Add(delta);
            AccuracyAPI.InvokeAccuracyUpdate(weaponInfo, delta);
        }
    }
}

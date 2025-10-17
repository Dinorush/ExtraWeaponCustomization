using Agents;
using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.HitTracker
{
    internal static class HitTrackerManager
    {
        private static readonly Dictionary<IOwnerComp, PlayerHitTracker> _trackers = new();

        [InvokeOnCleanup(onCheckpoint: true)]
        private static void Cleanup()
        {
            _trackers.Clear();
        }

        public static void RegisterHit(IOwnerComp owner, CustomWeaponComponent cwc, WeaponHitDamageableContext hitContext)
        {
            if (!_trackers.TryGetValue(owner, out var tracker))
                _trackers.Add(owner, tracker = new());
            tracker.RegisterHit(cwc, hitContext);
        }

        public static void RunKillContexts(Agent? enemy)
        {
            foreach (var tracker in _trackers.Values)
                tracker.RunKillContexts(enemy);
        }

        public static void RunStaggerContexts(Agent? enemy, bool limbBreak)
        {
            foreach (var tracker in _trackers.Values)
                tracker.RunStaggerContexts(enemy, limbBreak);
        }
    }
}

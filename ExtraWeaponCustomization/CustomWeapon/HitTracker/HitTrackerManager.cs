using Agents;
using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;

namespace EWC.CustomWeapon.HitTracker
{
    internal static class HitTrackerManager
    {
        private static readonly HitTracker _killTracker = new();
        private static readonly HitTracker _staggerTracker = new();
        private static ObjectWrapper<Agent> TempWrapper => ObjectWrapper<Agent>.SharedInstance;
        private static ObjectWrapper<CustomWeaponComponent> TempCWCWrapper => ObjectWrapper<CustomWeaponComponent>.SharedInstance;

        public static void RegisterHit(CustomWeaponComponent cwc, WeaponHitDamageableContext hitContext)
        {
            EnemyAgent? enemy = hitContext.Damageable.GetBaseAgent()?.TryCast<EnemyAgent>();
            if (enemy == null || !cwc.IsLocal) return;

            // Tag the enemy to ensure KillIndicatorFix tracks hit correctly.
            KillAPIWrapper.TagEnemy(enemy, cwc.Weapon, hitContext.LocalPosition);

            TempCWCWrapper.Set(cwc);
            TempWrapper.Set(enemy);
            _killTracker.RegisterHit(TempCWCWrapper, hitContext, TempWrapper);
            _staggerTracker.RegisterHit(TempCWCWrapper, hitContext, TempWrapper);
        }

        public static void RunKillContexts(Agent? enemy)
        {
            if (enemy == null || !_killTracker.TryGetContexts(TempWrapper.Set(enemy), out var hitsDict, out var lastCWCPtr)) return;

            foreach ((var cwc, (var context, float time)) in hitsDict)
                cwc.Object!.Invoke(new WeaponPostKillContext(context, time, cwc.Pointer == lastCWCPtr));
        }

        public static void RunStaggerContexts(Agent? enemy, bool limbBreak)
        {
            if (enemy == null || !_staggerTracker.TryGetContexts(TempWrapper.Set(enemy), out var hitsDict, out var lastCWCPtr)) return;

            foreach ((var cwc, (var context, float time)) in hitsDict)
                cwc.Object!.Invoke(new WeaponPostStaggerContext(context, time, cwc.Pointer == lastCWCPtr, limbBreak));
        }
    }
}

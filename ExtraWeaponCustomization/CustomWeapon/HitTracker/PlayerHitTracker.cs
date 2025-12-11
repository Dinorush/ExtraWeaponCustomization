using Agents;
using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;

namespace EWC.CustomWeapon.HitTracker
{
    internal class PlayerHitTracker
    {
        private readonly HitTracker _killTracker = new(onlyOnce: true);
        private readonly HitTracker _staggerTracker = new(onlyOnce: false);
        private static ObjectWrapper<Agent> TempWrapper => ObjectWrapper<Agent>.SharedInstance;
        private static ObjectWrapper<CustomWeaponComponent> TempCWCWrapper => ObjectWrapper<CustomWeaponComponent>.SharedInstance;

        public void RegisterHit(CustomWeaponComponent cwc, WeaponHitDamageableContext hitContext)
        {
            EnemyAgent? enemy = hitContext.Damageable.GetBaseAgent()?.TryCast<EnemyAgent>();
            if (enemy == null) return;

            // Tag the enemy to ensure KillIndicatorFix tracks hit correctly.
            if (cwc.Owner.Player?.IsLocallyOwned == true)
                KillAPIWrapper.TagEnemy(enemy, cwc.Weapon.Component.Cast<ItemEquippable>(), hitContext.LocalPosition);

            TempCWCWrapper.Set(cwc);
            TempWrapper.Set(enemy);
            _killTracker.RegisterHit(TempCWCWrapper, hitContext, TempWrapper);
            _staggerTracker.RegisterHit(TempCWCWrapper, hitContext, TempWrapper);
        }

        public void RunKillContexts(Agent? enemy)
        {
            if (enemy == null || !_killTracker.TryGetContexts(TempWrapper.Set(enemy), out var hitsDict, out var lastCWCPtr)) return;

            foreach ((var cwc, (var context, float time)) in hitsDict)
                cwc.Object!.Invoke(new WeaponPostKillContext(context, time, cwc.Pointer == lastCWCPtr));
        }

        public void RunStaggerContexts(Agent? enemy, bool limbBreak)
        {
            if (enemy == null || !_staggerTracker.TryGetContexts(TempWrapper.Set(enemy), out var hitsDict, out var lastCWCPtr)) return;

            foreach ((var cwc, (var context, float time)) in hitsDict)
                cwc.Object!.Invoke(new WeaponPostStaggerContext(context, time, cwc.Pointer == lastCWCPtr, limbBreak));
        }
    }
}

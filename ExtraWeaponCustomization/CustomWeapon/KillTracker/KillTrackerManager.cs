using Agents;
using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using System.Collections.Generic;
using System.Linq;

namespace EWC.CustomWeapon.KillTracker
{
    internal static class KillTrackerManager
    {
        private static readonly Dictionary<ObjectWrapper<Agent>, Dictionary<ObjectWrapper<CustomWeaponComponent>, (WeaponHitDamageableContext context, float time)>> _lastHits = new();
        private static readonly Dictionary<ObjectWrapper<Agent>, bool> _shownHits = new();
        private static ObjectWrapper<Agent> TempWrapper => ObjectWrapper<Agent>.SharedInstance;
        private static ObjectWrapper<CustomWeaponComponent> TempCWCWrapper => ObjectWrapper<CustomWeaponComponent>.SharedInstance;
        public static void ClearHit(EnemyAgent enemy)
        {
            TempWrapper.Set(enemy);
            _lastHits.Remove(TempWrapper);
            _shownHits.Remove(TempWrapper);
        }

        public static void RegisterHit(CustomWeaponComponent cwc, WeaponHitDamageableContext hitContext)
        {
            EnemyAgent? enemy = hitContext.Damageable.GetBaseAgent()?.TryCast<EnemyAgent>();
            if (enemy == null || !cwc.IsLocal) return;

            // Tag the enemy to ensure KillIndicatorFix tracks hit correctly.
            KillAPIWrapper.TagEnemy(enemy, cwc.Weapon, hitContext.LocalPosition);

            if (_lastHits.TryGetValue(TempWrapper.Set(enemy), out var hitInfo))
            {
                TempCWCWrapper.Set(cwc);
                if (hitInfo.ContainsKey(TempCWCWrapper))
                    hitInfo[TempCWCWrapper] = (hitContext, Clock.Time);
                else
                    hitInfo[new ObjectWrapper<CustomWeaponComponent>(cwc)] = (hitContext, Clock.Time);
            }
            else
            {
                ObjectWrapper<Agent> wrapper = new(enemy);
                _lastHits[wrapper] = new()
                {
                    [new ObjectWrapper<CustomWeaponComponent>(cwc)] = (hitContext, Clock.Time)
                };
                _shownHits[wrapper] = false;
            }
        }

        public static Dictionary<ObjectWrapper<CustomWeaponComponent>, (WeaponHitDamageableContext context, float time)>? GetKillHitContexts(Agent? enemy)
        {
            _lastHits.Keys
                .Where(wrapper => wrapper.Object == null)
                .ToList()
                .ForEach(wrapper =>
                {
                    _lastHits.Remove(wrapper);
                    _shownHits.Remove(wrapper);
                });

            if (enemy == null) return null;

            if (!_shownHits.ContainsKey(TempWrapper.Set(enemy)) || _shownHits[TempWrapper])
                return null;

            _shownHits[TempWrapper] = true;
            return _lastHits[TempWrapper];
        }
    }
}

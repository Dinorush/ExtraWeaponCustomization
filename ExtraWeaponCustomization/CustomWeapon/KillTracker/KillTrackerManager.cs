using Agents;
using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using System.Collections.Generic;
using System.Linq;

namespace EWC.CustomWeapon.KillTracker
{
    public static class KillTrackerManager
    {
        private static readonly Dictionary<ObjectWrapper<Agent>, (ItemEquippable Weapon, WeaponHitDamageableContext Context)> _lastHits = new();
        private static readonly Dictionary<ObjectWrapper<Agent>, bool> _shownHits = new();
        private static ObjectWrapper<Agent> TempWrapper => ObjectWrapper<Agent>.SharedInstance;

        public static void ClearHit(EnemyAgent enemy)
        {
            TempWrapper.SetObject(enemy);
            _lastHits.Remove(TempWrapper);
            _shownHits.Remove(TempWrapper);
        }

        public static void RegisterHit(ItemEquippable weapon, WeaponHitDamageableContext hitContext)
        {
            EnemyAgent? enemy = hitContext.Damageable.GetBaseAgent()?.TryCast<EnemyAgent>();
            if (enemy == null || !weapon.Owner.IsLocallyOwned) return;

            // Tag the enemy to ensure KillIndicatorFix tracks hit correctly.
            KillAPIWrapper.TagEnemy(enemy, weapon, hitContext.LocalPosition);

            // Still need to track weapon since KIF doesn't do that for host (only uses wielded, which may not be right for DoT)
            TempWrapper.SetObject(enemy);
            if (_lastHits.ContainsKey(TempWrapper))
                _lastHits[TempWrapper] = (weapon, hitContext);
            else
            {
                ObjectWrapper<Agent> wrapper = new(enemy);
                _lastHits[wrapper] = (weapon, hitContext);
                _shownHits[wrapper] = false;
            }
        }

        public static (ItemEquippable, WeaponHitDamageableContext)? GetKillHitContext(Agent? enemy)
        {
            _lastHits.Keys
                .Where(wrapper => wrapper.Object == null || _lastHits[wrapper].Weapon == null)
                .ToList()
                .ForEach(wrapper =>
                {
                    _lastHits.Remove(wrapper);
                    _shownHits.Remove(wrapper);
                });

            if (enemy == null) return null;

            TempWrapper.SetObject(enemy);
            if (!_shownHits.ContainsKey(TempWrapper) || _shownHits[TempWrapper])
                return null;

            _shownHits[TempWrapper] = true;
            return _lastHits[TempWrapper];
        }
    }
}

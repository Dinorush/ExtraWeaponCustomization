using Agents;
using Enemies;
using ExtraWeaponCustomization.CustomWeapon.ObjectWrappers;
using ExtraWeaponCustomization.Dependencies;
using Gear;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.KillTracker
{
    public static class KillTrackerManager
    {
        private static readonly Dictionary<AgentWrapper, WeaponHitWrapper> _lastHits = new();
        private static AgentWrapper TempWrapper => AgentWrapper.SharedInstance;

        public static void RegisterHit(Agent? agent, Vector3? localHitPosition, BulletWeapon? weapon, bool precHit = false)
        {
            EnemyAgent? enemy = agent?.TryCast<EnemyAgent>();
            if (enemy == null || weapon == null || !weapon.Owner.IsLocallyOwned) return;

            // Tag the enemy to ensure KillIndicatorFix tracks hit correctly.
            KillAPIWrapper.TagEnemy(enemy, weapon, localHitPosition);

            // Still need to track weapon since KIF doesn't do that for host (only uses wielded, which may not be right for DoT)
            TempWrapper.SetAgent(enemy);
            if (_lastHits.ContainsKey(TempWrapper))
                _lastHits[TempWrapper] = new WeaponHitWrapper(weapon, precHit);
            else
                _lastHits[new AgentWrapper(enemy)] = new WeaponHitWrapper(weapon, precHit);
        }

        public static WeaponHitWrapper? GetKillWeaponWrapper(Agent? enemy)
        {
            _lastHits.Keys
                .Where(wrapper => wrapper.Agent == null || _lastHits[wrapper].Weapon == null)
                .ToList()
                .ForEach(wrapper =>
                {
                    _lastHits.Remove(wrapper);
                });

            if (enemy == null) return null;

            return _lastHits.GetValueOrDefault(new AgentWrapper(enemy));
        }
    }
}

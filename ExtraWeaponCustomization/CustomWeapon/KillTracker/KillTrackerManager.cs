using Agents;
using Enemies;
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

        public static void RegisterHit(Agent? agent, Vector3? localHitPosition, BulletWeapon? weapon, bool precHit = false)
        {
            EnemyAgent? enemy = agent?.TryCast<EnemyAgent>();
            if (enemy == null || weapon == null || !weapon.Owner.IsLocallyOwned) return;

            // Tag the enemy to ensure KillIndicatorFix tracks hit correctly.
            KillAPIWrapper.TagEnemy(enemy, weapon, localHitPosition);
            
            // Still need to track weapon since KIF doesn't do that for host (only uses wielded, which may not be right for DoT)
            AgentWrapper wrapper = new(enemy);
            _lastHits[wrapper] = new WeaponHitWrapper(weapon, precHit);
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

        sealed class AgentWrapper
        {
            public int ID { get; }
            public Agent Agent { get; }

            public AgentWrapper(Agent agent)
            {
                ID = agent.GetInstanceID();
                Agent = agent;
            }

            public override int GetHashCode()
            {
                return ID;
            }

            public override bool Equals(object? obj)
            {
                return obj is AgentWrapper wrapper && wrapper.ID == ID;
            }
        }
    }
}

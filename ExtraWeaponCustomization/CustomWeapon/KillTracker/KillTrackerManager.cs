using Agents;
using ExtraWeaponCustomization.Utils;
using Gear;
using System.Collections.Generic;
using System.Linq;

namespace ExtraWeaponCustomization.CustomWeapon.KillTracker
{
    public static class KillTrackerManager
    {
        private static readonly Dictionary<AgentWrapper, WeaponHitWrapper> _lastHits = new();

        public static void RegisterHit(Agent? enemy, BulletWeapon? weapon, bool precHit = false)
        {
            if (enemy == null || weapon == null) return;

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

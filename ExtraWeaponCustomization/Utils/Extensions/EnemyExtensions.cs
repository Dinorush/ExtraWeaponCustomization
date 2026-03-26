using Agents;
using Enemies;
using UnityEngine;

namespace EWC.Utils.Extensions
{
    internal static class EnemyExtensions
    {
        // Equivalent to enemy.OnTakeDamage but with the cooldown check
        public static void OnTakeCustomDamage(this EnemyAgent enemy, float damage, Agent? damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool setCooldowns = true)
        {
            if (setCooldowns)
                enemy.Abilities.OnTakeDamage(damage);
            enemy.AI.m_behaviour.CurrentState?.OnTakeDamage(damage, damageSource, position, direction, hitreact);
            enemy.TookDamage?.Invoke(damage, hitreact);
        }
    }
}

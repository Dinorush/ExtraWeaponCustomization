using Enemies;
using Player;
using System;

namespace EWC.API
{
    public static class DamageAPI
    {
        public static event Action<float, EnemyAgent, PlayerAgent?>? OnDOTDamage;
        public static event Action<float, EnemyAgent, PlayerAgent?>? OnExplosiveDamage;

        internal static void FireDOTCallbacks(float damage, EnemyAgent enemy, PlayerAgent? source) => OnDOTDamage?.Invoke(damage, enemy, source);
        internal static void FireExplosiveCallbacks(float damage, EnemyAgent enemy, PlayerAgent? source) => OnExplosiveDamage?.Invoke(damage, enemy, source);
    }
}

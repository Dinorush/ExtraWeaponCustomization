using Enemies;
using Player;
using System;

namespace EWC.API
{
    public static class DamageAPI
    {
        public static event Action<float, EnemyAgent, PlayerAgent?>? PreDOTDamage;
        public static event Action<float, EnemyAgent, PlayerAgent?>? PostDOTDamage;
        public static event Action<float, EnemyAgent, PlayerAgent?>? PreExplosiveDamage;
        public static event Action<float, EnemyAgent, PlayerAgent?>? PostExplosiveDamage;

        internal static void FirePreDOTCallbacks(float damage, EnemyAgent enemy, PlayerAgent? source) => PreDOTDamage?.Invoke(damage, enemy, source);
        internal static void FirePostDOTCallbacks(float damage, EnemyAgent enemy, PlayerAgent? source) => PostDOTDamage?.Invoke(damage, enemy, source);

        internal static void FirePreExplosiveCallbacks(float damage, EnemyAgent enemy, PlayerAgent? source) => PreExplosiveDamage?.Invoke(damage, enemy, source);
        internal static void FirePostExplosiveCallbacks(float damage, EnemyAgent enemy, PlayerAgent? source) => PostExplosiveDamage?.Invoke(damage, enemy, source);
    }
}

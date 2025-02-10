﻿using Enemies;
using Player;

namespace EWC.API
{
    public static class DamageAPI
    {
        public delegate void DamageCallback(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source);

        public static event DamageCallback? PreDOTDamage;
        public static event DamageCallback? PostDOTDamage;
        public static event DamageCallback? PreExplosiveDamage;
        public static event DamageCallback? PostExplosiveDamage;

        internal static void FirePreDOTCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source) => PreDOTDamage?.Invoke(damage, enemy, limb, source);
        internal static void FirePostDOTCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source) => PostDOTDamage?.Invoke(damage, enemy, limb, source);

        internal static void FirePreExplosiveCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source) => PreExplosiveDamage?.Invoke(damage, enemy, limb, source);
        internal static void FirePostExplosiveCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source) => PostExplosiveDamage?.Invoke(damage, enemy, limb, source);
    }
}

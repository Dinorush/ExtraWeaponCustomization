using Enemies;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Structs;
using Player;

namespace EWC.API
{
    public static class DamageAPI
    {
        public delegate void DamageCallback(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc);

        public static event DamageCallback? PreLocalDOTDamage;
        public static event DamageCallback? PreDOTDamage;
        public static event DamageCallback? PostDOTDamage;
        public static event DamageCallback? PreLocalExplosiveDamage;
        public static event DamageCallback? PreExplosiveDamage;
        public static event DamageCallback? PostExplosiveDamage;
        public static event DamageCallback? PreLocalShrapnelDamage;
        public static event DamageCallback? PreShrapnelDamage;
        public static event DamageCallback? PostShrapnelDamage;

        internal static void FirePreDOTCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PreDOTDamage?.Invoke(damage, enemy, limb, source, cwc);
        internal static void FirePreLocalDOTCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PreLocalDOTDamage?.Invoke(damage, enemy, limb, source, cwc);
        internal static void FirePostDOTCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PostDOTDamage?.Invoke(damage, enemy, limb, source, cwc);

        internal static void FirePreExplosiveCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PreExplosiveDamage?.Invoke(damage, enemy, limb, source, cwc);
        internal static void FirePreLocalExplosiveCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PreLocalExplosiveDamage?.Invoke(damage, enemy, limb, source, cwc);
        internal static void FirePostExplosiveCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PostExplosiveDamage?.Invoke(damage, enemy, limb, source, cwc);

        internal static void FirePreShrapnelCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PreShrapnelDamage?.Invoke(damage, enemy, limb, source, cwc);
        internal static void FirePreLocalShrapnelCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PreLocalShrapnelDamage?.Invoke(damage, enemy, limb, source, cwc);
        internal static void FirePostShrapnelCallbacks(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => PostShrapnelDamage?.Invoke(damage, enemy, limb, source, cwc);
    }
}

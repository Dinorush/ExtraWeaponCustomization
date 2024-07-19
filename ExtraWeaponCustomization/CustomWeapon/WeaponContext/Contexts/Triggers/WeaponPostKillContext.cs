using Enemies;
using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponDamageFlagContext
    {
        public EnemyAgent Enemy { get; }

        public WeaponPostKillContext(EnemyAgent enemy, BulletWeapon weapon, DamageFlag flag = DamageFlag.Any) : base(weapon, flag)
        {
            Enemy = enemy;
        }
    }
}

using Enemies;
using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponDamageTypeContext
    {
        public EnemyAgent Enemy { get; }

        public WeaponPostKillContext(EnemyAgent enemy, BulletWeapon weapon, DamageType flag = DamageType.Any) : base(weapon, flag)
        {
            Enemy = enemy;
        }
    }
}

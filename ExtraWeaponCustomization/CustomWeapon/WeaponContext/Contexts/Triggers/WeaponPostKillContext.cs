using Enemies;
using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponTriggerContext
    {
        public EnemyAgent Enemy { get; }

        public WeaponPostKillContext(EnemyAgent enemy, BulletWeapon weapon, TriggerType type = TriggerType.OnKill) : base(weapon, type)
        {
            Enemy = enemy;
        }
    }
}

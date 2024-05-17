using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponTriggerContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public TriggerType Type { get; }

        public WeaponTriggerContext(BulletWeapon weapon, TriggerType type)
        {
            Weapon = weapon;
            Type = type;
        }
    }
}

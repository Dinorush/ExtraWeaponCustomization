using EWC.CustomWeapon.ComponentWrapper;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponInitContext : WeaponTriggerContext
    {
        public readonly IWeaponComp Weapon;
        public readonly IOwnerComp Owner;

        public WeaponInitContext(IOwnerComp owner, IWeaponComp weapon) : base()
        {
            Weapon = weapon;
            Owner = owner;
        }
    }
}

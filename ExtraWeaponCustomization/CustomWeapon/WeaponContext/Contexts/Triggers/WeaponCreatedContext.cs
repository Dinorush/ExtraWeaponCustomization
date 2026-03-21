using EWC.CustomWeapon.ComponentWrapper;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponCreatedContext : WeaponTriggerContext
    {
        public readonly IWeaponComp Weapon;
        public readonly IOwnerComp Owner;

        public WeaponCreatedContext(IOwnerComp owner, IWeaponComp weapon) : base()
        {
            Weapon = weapon;
            Owner = owner;
        }
    }
}

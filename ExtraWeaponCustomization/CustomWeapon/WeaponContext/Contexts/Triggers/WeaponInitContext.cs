using EWC.CustomWeapon.ComponentWrapper;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponInitContext : WeaponTriggerContext
    {
        public readonly IWeaponComp Weapon;
        public readonly IOwnerComp Owner;

        public WeaponInitContext(CustomWeaponComponent cwc) : base()
        {
            Weapon = cwc.Weapon;
            Owner = cwc.Owner;
        }
    }
}

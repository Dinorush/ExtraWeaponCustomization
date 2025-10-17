using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [ParentContext(typeof(WeaponTriggerContext))]
    [AllowAbstract]
    public abstract class WeaponTriggerContext : IWeaponContext
    {
        public WeaponTriggerContext()
        {
        }
    }
}

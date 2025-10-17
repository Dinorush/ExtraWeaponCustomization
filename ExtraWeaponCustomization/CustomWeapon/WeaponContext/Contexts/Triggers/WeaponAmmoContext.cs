using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Managed, validWeaponType: Enums.WeaponType.Gun | Enums.WeaponType.SentryHolder)]
    public sealed class WeaponAmmoContext : WeaponTriggerContext
    {
        public int Clip { get; }
        public int ClipMax { get; }
        public float ClipRel { get; }

        public WeaponAmmoContext(int clip, int clipMax)
        {
            Clip = clip;
            ClipMax = clipMax;
            ClipRel = (float)clip / clipMax;
        }
    }
}

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
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

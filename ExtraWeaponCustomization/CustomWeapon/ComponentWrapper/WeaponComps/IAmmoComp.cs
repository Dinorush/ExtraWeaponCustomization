namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public interface IAmmoComp : IWeaponComp
    {
        public int GetCurrentClip();
        public int GetMaxClip();
        public int GetMaxClip(out bool overflow)
        {
            overflow = false;
            return GetMaxClip();
        }
        public void SetCurrentClip(int clip);
    }
}

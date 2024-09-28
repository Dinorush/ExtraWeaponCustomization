namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreAmmoUIContext : IWeaponContext
    {
        public int Clip { get; set; }
        public int Reserve { get; set; }
        public float TotalRel { get; set; }
        public bool ShowClip { get; set; }
        public bool ShowReserve { get; set; }
        public bool ShowRel { get; set; }
        public bool ShowInfinite { get; set; }

        public WeaponPreAmmoUIContext(int clip, int reserve, float totalRel, bool showClip, bool showReserve, bool showRel, bool showInfinite)
        {
            Clip = clip;
            Reserve = reserve;
            TotalRel = totalRel;
            ShowClip = showClip;
            ShowReserve = showReserve;
            ShowRel = showRel;
            ShowInfinite = showInfinite;
        }
    }
}

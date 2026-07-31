namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponSentryStateContext : WeaponTriggerContext
    {
        public bool Deployed { get; }
        public WeaponSentryStateContext(bool deployed) : base()
        {
            Deployed = deployed;
        }
    }
}

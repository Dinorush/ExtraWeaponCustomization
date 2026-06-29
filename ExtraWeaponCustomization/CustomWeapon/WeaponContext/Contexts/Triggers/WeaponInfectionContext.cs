namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponInfectionContext : WeaponTriggerContext
    {
        public float Infection { get; }

        public WeaponInfectionContext(Dam_PlayerDamageBase damBase) : base()
        {
            Infection = damBase.Infection;
        }
    }
}

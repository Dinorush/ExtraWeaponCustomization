namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHealthContext : WeaponTriggerContext
    {
        public float Health { get; }
        public float HealthMax { get; }

        public WeaponHealthContext(Dam_PlayerDamageBase damBase) : base()
        {
            Health = damBase.Health;
            HealthMax = damBase.HealthMax;
        }
    }
}

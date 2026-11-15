namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponCombatContext : WeaponTriggerContext
    {
        public bool InCombat { get; }

        public WeaponCombatContext(bool state) : base()
        {
            InCombat = state;
        }
    }
}

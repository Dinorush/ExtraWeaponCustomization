using Gear;

namespace EWC.API
{
    public static class FireRateAPI
    {
        public delegate void CooldownSetCallback(BulletWeapon weapon, float shotDelay, float burstDelay, float cooldownDelay);

        public static event CooldownSetCallback? CooldownSet;

        internal static void FireCooldownSetCallback(BulletWeapon weapon, float shotDelay, float burstDelay, float cooldownDelay) => CooldownSet?.Invoke(weapon, shotDelay, burstDelay, cooldownDelay);
    }
}

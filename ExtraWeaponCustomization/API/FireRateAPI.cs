using Gear;

namespace EWC.API
{
    public static class FireRateAPI
    {
        public delegate void CooldownSetCallback(BulletWeapon weapon, float shotDelay, float burstDelay, float cooldownDelay);
        public delegate void CooldownInterruptCallback(BulletWeapon weapon);

        public static event CooldownSetCallback? CooldownSet;
        public static event CooldownInterruptCallback? CooldownInterrupt;

        internal static void FireCooldownSetCallback(BulletWeapon weapon, float shotDelay, float burstDelay, float cooldownDelay) => CooldownSet?.Invoke(weapon, shotDelay, burstDelay, cooldownDelay);
        internal static void FireCooldownInterruptCallback(BulletWeapon weapon) => CooldownInterrupt?.Invoke(weapon);
    }
}

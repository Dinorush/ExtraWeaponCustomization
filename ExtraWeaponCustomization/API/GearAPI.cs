using EWC.CustomWeapon;

namespace EWC.API
{
    public static class GearAPI
    {
        public delegate void GearSpawnedCallback(ItemEquippable item, CustomWeaponComponent? cwc);

        public static event GearSpawnedCallback? OnGearSpawned;
        internal static void FireGearSpawnedCallback(ItemEquippable item, CustomWeaponComponent? cwc) => OnGearSpawned?.Invoke(item, cwc);
    }
}

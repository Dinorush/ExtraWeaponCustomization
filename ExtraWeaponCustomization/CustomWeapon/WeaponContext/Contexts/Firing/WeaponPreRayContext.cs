using ExtraWeaponCustomization.Utils;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreRayContext : IWeaponContext
    {
        public HitData Data { get; }
        public Vector3 Position { get; }

        public WeaponPreRayContext(HitData hitData, Vector3 position)
        {
            Data = hitData;
            Position = position;
        }
    }
}

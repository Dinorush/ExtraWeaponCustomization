using ExtraWeaponCustomization.Utils;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostRayContext : IWeaponContext
    {
        public HitData Data { get; }
        public Vector3 Position { get; }
        public bool Result { get; set; }

        public WeaponPostRayContext(HitData hitData, Vector3 position, bool result)
        {
            Data = hitData;
            Position = position;
            Result = result;
        }
    }
}

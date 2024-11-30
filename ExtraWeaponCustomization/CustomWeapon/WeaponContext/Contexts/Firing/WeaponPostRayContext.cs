using EWC.Utils;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostRayContext : IWeaponContext
    {
        public HitData Data { get; }
        public Vector3 Position { get; }
        public bool Result { get; set; }
        public IntPtr IgnoreEnt { get; } = IntPtr.Zero;

        public WeaponPostRayContext(HitData hitData, Vector3 position, bool result, IntPtr ignoreEnt = default)
        {
            Data = hitData;
            Position = position;
            Result = result;
            IgnoreEnt = ignoreEnt;
        }
    }
}

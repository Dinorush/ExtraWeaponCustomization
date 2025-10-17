using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponReferencePosContext : WeaponReferenceContext, IPositionContext
    {
        public Vector3 Position { get; }

        public Vector3 Direction { get; }

        public Vector3 Normal { get; }

        public float Falloff { get; }

        public ShotInfo.Const ShotInfo { get; }

        public WeaponReferencePosContext(uint id, uint callbackID, Vector3 position, Vector3 direction, Vector3 normal, float falloff, ShotInfo.Const shotInfo, float mod = 1f) : base(id, callbackID, mod)
        {
            Position = position;
            Direction = direction;
            Normal = normal;
            Falloff = falloff;
            ShotInfo = shotInfo;
        }
    }
}

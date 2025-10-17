using EWC.CustomWeapon.CustomShot;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts.Base
{
    internal interface IPositionContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public Vector3 Normal { get; }
        public float Falloff { get; }
        public ShotInfo.Const ShotInfo { get; }
    }
}

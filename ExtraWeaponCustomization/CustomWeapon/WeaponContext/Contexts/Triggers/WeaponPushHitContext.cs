using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Attributes;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredWeaponType: Enums.WeaponType.Melee)]
    internal class WeaponPushHitContext : WeaponTriggerContext, IPositionContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public Vector3 Normal { get; }
        public float Falloff => 1f;
        public ShotInfo.Const ShotInfo { get; }

        public WeaponPushHitContext(HitData data) : base()
        {
            Position = data.hitPos;
            Direction = data.fireDir;
            Normal = data.RayHit.normal;
            ShotInfo = data.shotInfo;
        }
    }
}

using AIGraph;
using EWC.CustomWeapon.Enums;
using Player;
using UnityEngine;

namespace EWC.CustomWeapon.ComponentWrapper.OwnerComps
{
    public sealed class LocalOwnerComp : OwnerComp<PlayerAgent>
    {
        public LocalOwnerComp(PlayerAgent agent, Transform muzzleAlign) : base(agent, muzzleAlign) { }

        public override PlayerAgent Player => Value;
        public override OwnerType Type => OwnerType.Player | OwnerType.Local | OwnerType.Managed;
        public override AIG_CourseNode CourseNode => Value.CourseNode;
        public override Vector3 FirePos => Value.FPSCamera.Position;
        public override Vector3 FireDir => Value.FPSCamera.CameraRayDir;
    }
}

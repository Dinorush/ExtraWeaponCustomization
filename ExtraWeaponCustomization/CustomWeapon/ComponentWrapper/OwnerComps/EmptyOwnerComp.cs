using AIGraph;
using EWC.CustomWeapon.Enums;
using Player;
using UnityEngine;

namespace EWC.CustomWeapon.ComponentWrapper.OwnerComps
{
    public class EmptyOwnerComp : OwnerComp<PlayerAgent>
    {
        public EmptyOwnerComp() : base(PlayerManager.GetLocalPlayerAgent(), Globals.Global.Current.transform) { }

        public override PlayerAgent Player => Value;
        public override OwnerType Type => OwnerType.Any;
        public override AIG_CourseNode CourseNode => AIG_CourseNode.s_allNodes[0];
        public override Vector3 FirePos => Vector3.zero;
        public override Vector3 FireDir => Vector3.zero;
    }
}

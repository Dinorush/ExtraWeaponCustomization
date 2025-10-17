using AIGraph;
using EWC.CustomWeapon.Enums;
using Player;
using UnityEngine;

namespace EWC.CustomWeapon.ComponentWrapper.OwnerComps
{
    public sealed class SyncedOwnerComp : OwnerComp<PlayerAgent>
    {
        private readonly OwnerType _type;
        public SyncedOwnerComp(PlayerAgent agent, Transform muzzleAlign) : base(agent, muzzleAlign)
        {
            _type = OwnerType.Player;
            if (agent.Owner.IsBot && SNetwork.SNet.IsMaster)
                _type |= OwnerType.Managed;
            else
                _type |= OwnerType.Unmanaged;
        }

        public override PlayerAgent Player => Value;
        public override OwnerType Type => _type;
        public override AIG_CourseNode CourseNode => Value.CourseNode;
        public override Vector3 FirePos => MuzzleAlign.position;
        public override Vector3 FireDir => MuzzleAlign.forward;
    }
}

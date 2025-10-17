using AIGraph;
using EWC.CustomWeapon.Enums;
using Player;
using UnityEngine;

namespace EWC.CustomWeapon.ComponentWrapper.OwnerComps
{
    public sealed class SentryOwnerComp : OwnerComp<SentryGunInstance>
    {
        private readonly OwnerType _type;
        public SentryOwnerComp(SentryGunInstance sentry) : base(sentry, sentry.MuzzleAlign)
        {
            _type = OwnerType.Sentry;
            if (SNetwork.SNet.IsMaster)
                _type |= OwnerType.Managed;
            else
                _type |= OwnerType.Unmanaged;
        }

        public override PlayerAgent Player => Value.Owner;
        public override OwnerType Type => _type;
        public override AIG_CourseNode CourseNode => Value.CourseNode;
        public override Vector3 FirePos => MuzzleAlign.position;
        public override Vector3 FireDir => MuzzleAlign.forward;
    }
}

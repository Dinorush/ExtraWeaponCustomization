using Enemies;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponDamageTypeContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public Vector3 LocalPosition { get; }
        public EnemyAgent Enemy { get; }
        public float Falloff { get; }
        public float Backstab { get; }

        public WeaponPostKillContext(WeaponPreHitDamageableContext hitContext) : base(hitContext.DamageType)
        {
            Enemy = hitContext.Damageable.GetBaseAgent().TryCast<EnemyAgent>()!;

            Position = hitContext.LocalPosition + Enemy.Position;
            Direction = hitContext.Direction;
            LocalPosition = hitContext.LocalPosition;
            Falloff = hitContext.Falloff;
            Backstab = hitContext.Backstab;
        }
    }
}

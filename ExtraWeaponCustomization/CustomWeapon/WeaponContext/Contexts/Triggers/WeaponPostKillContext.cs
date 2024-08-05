using Enemies;
using Gear;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponDamageTypeContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public Vector3 LocalPosition { get; }
        public EnemyAgent Enemy { get; }
        public float Falloff { get; }
        public float Backstab { get; }

        public WeaponPostKillContext(WeaponPreHitEnemyContext hitContext) : base(hitContext.Weapon, hitContext.DamageType)
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

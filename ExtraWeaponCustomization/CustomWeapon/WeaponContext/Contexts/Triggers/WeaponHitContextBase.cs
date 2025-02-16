using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.Utils;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts.Triggers
{
    public class WeaponHitContextBase : WeaponDamageTypeContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public Vector3 Normal { get; }
        public float Falloff { get; }
        private Collider? _collider;
        public Collider Collider { get => _collider ??= SetCollider(); }
        protected virtual Collider SetCollider() => throw new NotImplementedException();

        public WeaponHitContextBase(Collider collider, Vector3 position, Vector3 direction, Vector3 normal, float falloff, ShotInfo.Const info, DamageType flag) :
            base(flag, info)
        {
            _collider = collider;
            Position = position;
            Direction = direction.normalized;
            Normal = normal.normalized;
            Falloff = falloff;
        }

        public WeaponHitContextBase(Vector3 position, Vector3 direction, Vector3 normal, float falloff, ShotInfo.Const info, DamageType flag) :
            base(flag, info)
        {
            Position = position;
            Direction = direction.normalized;
            Normal = normal.normalized;
            Falloff = falloff;
        }
    }

    public class WeaponHitDamageableContextBase : WeaponHitContextBase
    {
        public Vector3 LocalPosition { get; }
        public IDamageable Damageable { get; }
        public float Backstab { get; }
        protected override Collider SetCollider() => Damageable.Cast<MonoBehaviour>().GetComponent<Collider>();

        public WeaponHitDamageableContextBase(IDamageable damageable, Vector3 position, Vector3 direction, Vector3 normal, float backstab, float falloff, ShotInfo.Const info, DamageType flag) :
            base(position, direction, normal, falloff, info, flag.WithSubTypes(damageable))
        {
            Damageable = damageable ?? throw new ArgumentNullException(nameof(damageable));
            LocalPosition = position - damageable.GetBaseAgent()?.Position ?? Vector3.zero;
            Backstab = backstab;
        }

        public WeaponHitDamageableContextBase(HitData data, float backstab, DamageType flag) :
            this(data.damageable!, data.hitPos, data.fireDir.normalized, data.RayHit.normal, backstab, data.falloff, data.shotInfo, flag.WithSubTypes(data.damageable!))
        { }

        public WeaponHitDamageableContextBase(WeaponHitDamageableContextBase context) :
            this(context.Damageable, context.Position, context.Direction, context.Normal, context.Backstab, context.Falloff, context.ShotInfo, context.DamageType)
        { }
    }
}

using EWC.Utils;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts.Triggers
{
    public class WeaponHitContextBase : WeaponDamageTypeContext
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float Falloff { get; }
        public WeaponHitContextBase(Vector3 position, Vector3 direction, float falloff) :
            base(Properties.Effects.Triggers.DamageType.Any)
        {
            Position = position;
            Direction = direction;
            Falloff = falloff;
        }
    }

    public class WeaponHitDamageableContextBase : WeaponHitContextBase
    {
        public Vector3 LocalPosition { get; }
        public IDamageable Damageable { get; }
        public float Backstab { get; }

        public WeaponHitDamageableContextBase(IDamageable damageable, Vector3 position, Vector3 direction, float backstab, float falloff) :
            base(position, direction, falloff)
        {
            Damageable = damageable ?? throw new ArgumentNullException(nameof(damageable));
            LocalPosition = position - damageable.GetBaseAgent()?.Position ?? Vector3.zero;
            Backstab = backstab;
        }

        public WeaponHitDamageableContextBase(HitData data, float backstab) :
            this(data.damageable!, data.hitPos, data.fireDir.normalized, backstab, data.falloff)
        { }
    }
}

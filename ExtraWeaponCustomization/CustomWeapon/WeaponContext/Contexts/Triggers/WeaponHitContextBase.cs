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

        public WeaponHitContextBase(Vector3 position, Vector3 localPosition, Vector3 direction, float falloff) :
            base(Properties.Effects.Triggers.DamageType.Any)
        {
            Position = position;
            Direction = direction;
            Falloff = falloff;
        }

        public WeaponHitContextBase(Vector3 position, Vector3 direction, float falloff, IDamageable? damageable) :
            base(Properties.Effects.Triggers.DamageType.Any)
        {
            Position = position;
            Direction = direction;
            Falloff = falloff;
        }

        public WeaponHitContextBase(HitData data) :
            this(data.hitPos, data.fireDir.normalized, data.falloff, data.damageable)
        { }
    }

    public class WeaponHitDamageableContextBase : WeaponHitContextBase
    {
        public Vector3 LocalPosition { get; }
        public IDamageable Damageable { get; }

        public WeaponHitDamageableContextBase(IDamageable damageable, Vector3 position, Vector3 direction, float falloff) :
            base(position, direction, falloff, damageable)
        {
            Damageable = damageable ?? throw new ArgumentNullException(nameof(damageable));
            LocalPosition = position - damageable.GetBaseAgent()?.Position ?? Vector3.zero;
        }

        public WeaponHitDamageableContextBase(HitData data) :
            this(data.damageable!, data.hitPos, data.fireDir.normalized, data.falloff)
        { }
    }
}

using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.Utils;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts.Base
{
    public abstract class WeaponHitDamageableContextBase : WeaponHitContextBase
    {
        public Vector3 LocalPosition { get; }
        public IDamageable Damageable { get; }
        public float Backstab { get; }
        protected override GameObject SetGameObject() => Damageable.Cast<MonoBehaviour>().gameObject;

        public WeaponHitDamageableContextBase(IDamageable damageable, Vector3 position, Vector3 direction, Vector3 normal, float backstab, float falloff, ShotInfo.Const info, DamageType flag) :
            base(position, direction, normal, falloff, info, flag.WithSubTypes(damageable))
        {
            Damageable = damageable ?? throw new ArgumentNullException(nameof(damageable));
            LocalPosition = position - damageable.GetBaseAgent()?.Position ?? Vector3.zero;
            Backstab = backstab;
        }

        public WeaponHitDamageableContextBase(HitData data, float backstab) :
            this(data.damageable!, data.hitPos, data.fireDir.normalized, data.RayHit.normal, backstab, data.falloff, data.shotInfo, data.damageType)
        { }

        public WeaponHitDamageableContextBase(WeaponHitDamageableContextBase context) :
            this(context.Damageable, context.Position, context.Direction, context.Normal, context.Backstab, context.Falloff, context.ShotInfo, context.DamageType)
        { }
    }
}

﻿using EWC.CustomWeapon.CustomShot;
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
        private GameObject? _gameObject;
        public GameObject GameObject { get => _gameObject ??= SetGameObject(); }
        protected virtual GameObject SetGameObject() => throw new NotImplementedException();

        public WeaponHitContextBase(GameObject gameObject, Vector3 position, Vector3 direction, Vector3 normal, float falloff, ShotInfo.Const info, DamageType flag) :
            base(flag, info)
        {
            _gameObject = gameObject;
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

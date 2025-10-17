using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts.Base
{
    public abstract class WeaponHitContextBase : WeaponDamageTypeContext, IPositionContext
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
}

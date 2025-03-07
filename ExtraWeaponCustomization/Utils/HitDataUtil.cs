﻿using EWC.CustomWeapon.CustomShot;
using EWC.Utils.Extensions;
using Gear;
using Player;
using UnityEngine;
using static Weapon;

namespace EWC.Utils
{
    public sealed class HitData
    {
        public float damage;
        public Vector2 damageFalloff;
        public float falloff;
        public float precisionMulti;
        public float staggerMulti;
        public float maxRayDist;
        public PlayerAgent owner;
        public Vector3 fireDir;
        public Vector3 hitPos;
        public IDamageable? damageable;
        public Collider collider;
        private RaycastHit _rayHit;
        public RaycastHit RayHit
        { 
            get { return _rayHit; } 
            set 
            { 
                _rayHit = value;
                hitPos = _rayHit.point;
                collider = _rayHit.collider;
                damageable = DamageableUtil.GetDamageableFromRayHit(_rayHit);
            }
        }
        public ShotInfo shotInfo = new();

        private WeaponHitData? _weaponHitData;
        private MeleeWeaponFirstPerson? _meleeWeapon;

#pragma warning disable CS8618
        // All used fields are set
        public HitData(WeaponHitData hitData, float additionalDist = 0) => Setup(hitData, additionalDist);
        public HitData(MeleeWeaponFirstPerson melee, MeleeWeaponDamageData hitData) => Setup(melee, hitData);
        
        // Class responsible for using this should ensure fields are set!
        public HitData() { }
#pragma warning restore CS8618

        public void Setup(WeaponHitData hitData, float additionalDist = 0)
        {
            _weaponHitData = hitData;
            _meleeWeapon = null;

            shotInfo = ShotManager.GetVanillaShotInfo(hitData);
            damage = hitData.damage;
            damageFalloff = hitData.damageFalloff;
            precisionMulti = hitData.precisionMulti;
            staggerMulti = hitData.staggerMulti;
            owner = hitData.owner;
            fireDir = hitData.fireDir;
            maxRayDist = hitData.maxRayDist;
            RayHit = hitData.rayHit;
            SetFalloff(additionalDist);
        }

        public void Setup(MeleeWeaponFirstPerson melee, MeleeWeaponDamageData hitData)
        {
            _weaponHitData = null;
            _meleeWeapon = melee;

            damage = melee.m_damageToDeal;
            precisionMulti = melee.m_precisionMultiToDeal;
            staggerMulti = melee.m_staggerMultiToDeal;
            falloff = 1f;
            fireDir = hitData.hitPos - hitData.sourcePos;
            hitPos = hitData.hitPos;
            // Don't need to use overriden null check since we only care about when damageComp isn't set at all
            damageable = hitData.damageComp ?? hitData.damageGO.GetComponent<IDamageable>();
        }

        public void Apply()
        {
            if (_weaponHitData != null)
                Apply(_weaponHitData);
            else if (_meleeWeapon != null)
                Apply(_meleeWeapon);
        }

        public WeaponHitData Apply(WeaponHitData hitData)
        {
            hitData.owner = owner;
            hitData.damage = damage;
            hitData.precisionMulti = precisionMulti;
            hitData.staggerMulti = staggerMulti;
            hitData.rayHit = RayHit;
            hitData.fireDir = fireDir;
            hitData.maxRayDist = maxRayDist;
            return hitData;
        }

        public MeleeWeaponFirstPerson Apply(MeleeWeaponFirstPerson melee)
        {
            melee.m_damageToDeal = damage;
            melee.m_precisionMultiToDeal = precisionMulti;
            melee.m_staggerMultiToDeal = staggerMulti;
            return melee;
        }

        public void SetFalloff(float additionalDist = 0)
        {
            falloff = (RayHit.distance + additionalDist).Map(damageFalloff.x, damageFalloff.y, 1f, BulletWeapon.s_falloffMin);
        }

        public WeaponHitData ToWeaponHitData()
        {
            return new WeaponHitData()
            {
                damage = damage,
                damageFalloff = damageFalloff,
                precisionMulti = precisionMulti,
                staggerMulti = staggerMulti,
                owner = owner,
                rayHit = RayHit,
                fireDir = fireDir,
                maxRayDist = maxRayDist
            };
        }
    }
}

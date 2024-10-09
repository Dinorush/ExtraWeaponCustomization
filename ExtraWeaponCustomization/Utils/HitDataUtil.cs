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
        private RaycastHit _rayHit;
        public RaycastHit RayHit
        { 
            get { return _rayHit; } 
            set 
            { 
                _rayHit = value;
                hitPos = _rayHit.point;
                damageable = DamageableUtil.GetDamageableFromRayHit(_rayHit);
            }
        }

        private WeaponHitData? _weaponHitData;
        private MeleeWeaponFirstPerson? _meleeWeapon;

#pragma warning disable CS8618 // All used fields are set
        public HitData(WeaponHitData hitData, float additionalDist = 0) => Setup(hitData, additionalDist);

        public HitData(MeleeWeaponFirstPerson melee, MeleeWeaponDamageData hitData) => Setup(melee, hitData);
        
        // Class responsible for using this should ensure fields are set!
        public HitData() { }
#pragma warning restore CS8618

        public void Setup(WeaponHitData hitData, float additionalDist = 0)
        {
            _weaponHitData = hitData;
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
            _meleeWeapon = melee;
            damage = melee.m_damageToDeal;
            precisionMulti = melee.m_precisionMultiToDeal;
            staggerMulti = melee.m_staggerMultiToDeal;
            falloff = 1f;
            fireDir = hitData.hitPos - hitData.sourcePos;
            hitPos = hitData.hitPos;

            damageable = DamageableUtil.GetDamageableFromGO(hitData.damageGO);
        }

        public void Apply()
        {
            if (_weaponHitData != null)
                Apply(_weaponHitData);
            else if (_meleeWeapon != null)
                Apply(_meleeWeapon);
        }

        private void Apply(WeaponHitData hitData)
        {
            hitData.damage = damage;
            hitData.precisionMulti = precisionMulti;
            hitData.staggerMulti = staggerMulti;
            hitData.rayHit = RayHit;
            hitData.fireDir = fireDir;
            hitData.maxRayDist = maxRayDist;
        }

        private void Apply(MeleeWeaponFirstPerson melee)
        {
            melee.m_damageToDeal = damage;
            melee.m_precisionMultiToDeal = precisionMulti;
            melee.m_staggerMultiToDeal = staggerMulti;
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

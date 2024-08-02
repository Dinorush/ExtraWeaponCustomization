using Agents;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using ExtraWeaponCustomization.Patches;
using ExtraWeaponCustomization.Utils;
using FX_EffectSystem;
using GameData;
using Gear;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public sealed class EWCProjectileHitbox
    {
        private readonly EWCProjectileComponentBase _base;

        // Set on init
        private Projectile? _settings;
        private CustomWeaponComponent _baseCWC;
        private BulletWeapon _weapon;
        private readonly HashSet<int> _initialPlayers = new(4);
        private uint _searchID = 0;
        private WeaponHitData _hitData = new();
        private bool _enabled = false;

        // Variables
        private int _pierceCount = 1;
        private float _distanceMoved;
        private float _baseDamage;
        private float _basePrecision;
        private float _lastFixedTime;

        // Static
        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private readonly static HashSet<int> s_playerCheck = new(4);

#pragma warning disable CS8618
        // Hidden null warnings since other methods will initialize members prior to Update
        public EWCProjectileHitbox(EWCProjectileComponentBase comp) 
        {
            _base = comp;
        }
#pragma warning restore CS8618

        public void Init(CustomWeaponComponent cwc, Projectile projBase)
        {
            if (_enabled) return;

            if (!cwc.Weapon.Owner.IsLocallyOwned) return;

            _enabled = true;
            _settings = projBase;
            _baseCWC = cwc;
            _weapon = cwc.Weapon;

            Vector3 pos = _weapon.Owner.Position;
            Vector3 dir = _weapon.Owner.FPSCamera.CameraRayDir;
            int ownerID = _weapon.Owner.GetInstanceID();
            _initialPlayers.Add(ownerID);
            foreach (PlayerAgent agent in PlayerManager.PlayerAgentsInLevel)
            {
                Vector3 diff = agent.Position - pos;
                if (agent.GetInstanceID() != ownerID && Vector3.Dot(dir, diff) > 0)
                    _initialPlayers.Add(agent.GetInstanceID());
            }

            if (_weapon.ArchetypeData.PiercingBullets && _weapon.ArchetypeData.PiercingDamageCountLimit > 1)
            {
                _searchID = _weapon.m_damageSearchID;
                _pierceCount = _weapon.ArchetypeData.PiercingDamageCountLimit;
            }
            else
            {
                _searchID = 0;
                _pierceCount = 1;
            }

            ArchetypeDataBlock archData = _weapon.ArchetypeData;
            _hitData.owner = _weapon.Owner;
            _hitData.damage = archData.Damage;
            _hitData.damageFalloff = archData.DamageFalloff;
            _hitData.staggerMulti = archData.StaggerDamageMulti;
            _hitData.precisionMulti = archData.PrecisionDamageMulti;

            _baseDamage = _hitData.damage;
            _basePrecision = _hitData.precisionMulti;
            _distanceMoved = 0;
            _lastFixedTime = Time.fixedTime;
        }

        public void Die()
        {
            _enabled = false;
            _initialPlayers.Clear();
        }

        public void Update(Vector3 position, Vector3 velocityDelta)
        {
            if (!_enabled) return;

            if (_settings == null)
            {
                _base.Die();
                return;
            }

            s_ray.origin = position;
            s_ray.direction = velocityDelta;

            s_playerCheck.Clear();
            if (_settings.Size == _settings.SizeWorld)
                CheckCollision(_settings.Size, EWCProjectileManager.MaskEntityAndWorld);
            else
            {
                CheckCollision(_settings.Size, EWCProjectileManager.MaskEntity);
                if (_pierceCount <= 0) return;
                CheckCollision(_settings.SizeWorld, EWCProjectileManager.MaskWorld);
            }

            // Player moves on fixed time so only remove on fixed time
            if (_lastFixedTime != Time.fixedTime && _initialPlayers.Count != 0)
            {
                _initialPlayers.RemoveWhere(id => !s_playerCheck.Contains(id));
                _lastFixedTime = Time.fixedTime;
            }

            _distanceMoved += velocityDelta.magnitude;
        }

        private void CheckCollision(float size, int layerMask)
        {
            RaycastHit[] hits;
            if (size == 0)
                hits = Physics.RaycastAll(s_ray, 1f, layerMask);
            else
                hits = Physics.SphereCastAll(s_ray, size, 1f, layerMask);

            Array.Sort(hits, DistanceCompare);
            foreach (RaycastHit hit in hits)
            {
                s_rayHit = hit;
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(s_rayHit);

                if (damageable != null)
                    DoDamage(damageable);
                else // Hit a wall
                {
                    BulletHit(null);
                    _base.Die();
                    return;
                }

                if (_pierceCount <= 0) break;
            }
        }

        private static int DistanceCompare(RaycastHit a, RaycastHit b)
        {
            if (a.distance == b.distance) return 0;
            return a.distance < b.distance ? -1 : 1;
        }

        private void DoDamage(IDamageable damageable, bool cast = false)
        {
            if (!ShouldDamage(damageable))
                return;

            BulletHit(damageable);

            if (--_pierceCount <= 0)
                _base.Die();
        }

        private bool ShouldDamage(IDamageable damageable)
        {
            Agent? agent = damageable.GetBaseAgent();
            if (agent != null)
            {
                if (agent.Type == AgentType.Player && _initialPlayers.Contains(agent.GetInstanceID()))
                {
                    s_playerCheck.Add(agent.GetInstanceID());
                    return false;
                }
                else if (!agent.Alive)
                    return false;
            }

            if (_searchID == 0) return true;

            IDamageable? baseDamageable = damageable.GetBaseDamagable();
            if (baseDamageable != null)
            {
                if (baseDamageable.TempSearchID == _searchID)
                    return false;
                else
                    baseDamageable.TempSearchID = _searchID;
            }

            return true;
        }

        private void DoImpactFX(IDamageable? damageable)
        {
            GameObject gameObject = s_rayHit.collider.gameObject;
            var colliderMaterial = gameObject.GetComponent<ColliderMaterial>();
            bool isDecalsAllowed = (LayerManager.MASK_VALID_FOR_DECALS & gameObject.gameObject.layer) == 0;

            FX_GroupName impactFX = FX_GroupName.Impact_Concrete;
            if (colliderMaterial != null)
                impactFX = (FX_GroupName)colliderMaterial.MaterialId;
            else if (damageable?.GetBaseAgent()?.Type == AgentType.Player)
                impactFX = FX_GroupName.Impact_PlayerBody;

            FX_Manager.PlayEffect(false, impactFX, null, s_rayHit.point, Quaternion.LookRotation(s_rayHit.normal), isDecalsAllowed);
        }

        // Can't call base game bullet hit since WeaponPatches assumes hitscan bullets for its logic
        private void BulletHit(IDamageable? damageable)
        {
            _hitData.damage = _baseDamage;
            _hitData.precisionMulti = _basePrecision;
            _hitData.fireDir = s_ray.direction.normalized;
            _hitData.rayHit = s_rayHit;

            DoImpactFX(damageable);

            WeaponPatches.ApplyEWCBulletHit(_baseCWC!, damageable, ref _hitData, _distanceMoved, _searchID, ref _baseDamage);
            float damage = _hitData.damage * _hitData.Falloff(_distanceMoved + _hitData.rayHit.distance);
            damageable?.BulletDamage(damage, _hitData.owner, _hitData.rayHit.point, _hitData.fireDir, _hitData.rayHit.normal, allowDirectionalBonus: true, _hitData.staggerMulti, _hitData.precisionMulti);
        }
    }
}

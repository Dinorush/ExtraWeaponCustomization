﻿using Agents;
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
        private readonly HashSet<int> _initialPlayers = new();
        private readonly HashSet<int> _hitEnts = new();
        private WeaponHitData _hitData = new();
        private int _entityLayer;
        private bool _enabled = false;

        // Variables
        private bool _pierce;
        private int _pierceCount = 1;
        private float _distanceMoved;
        private float _baseDamage;
        private float _basePrecision;
        private float _lastFixedTime;

        // Static
        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private static float s_velMagnitude;
        private readonly static HashSet<int> s_playerCheck = new();
        private const float SightCheckMinSize = 0.5f;
        private const float CollisionInitialMinSize = 2f;

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

            _entityLayer = LayerManager.MASK_ENEMY_DAMAGABLE;
            _initialPlayers.Clear();
            int ownerID = _weapon.Owner.GetInstanceID();
            if (projBase.DamageOwner)
            {
                _initialPlayers.Add(ownerID);
                _entityLayer |= EWCProjectileManager.MaskOwner;
            }

            if (projBase.DamageFriendly)
            {
                _entityLayer |= EWCProjectileManager.MaskFriendly;
                foreach (PlayerAgent agent in PlayerManager.PlayerAgentsInLevel)
                {
                    Vector3 diff = agent.Position - pos;
                    if (agent.GetInstanceID() != ownerID && Vector3.Dot(dir, diff) > 0)
                        _initialPlayers.Add(agent.GetInstanceID());
                }
            }

            if (projBase.HitSize == projBase.HitSizeWorld)
                _entityLayer |= EWCProjectileManager.MaskWorld;

            _hitEnts.Clear();
            if (_weapon.ArchetypeData.PiercingBullets && _weapon.ArchetypeData.PiercingDamageCountLimit > 1)
            {
                _pierce = true;
                _pierceCount = _weapon.ArchetypeData.PiercingDamageCountLimit;
            }
            else
            {
                _pierce = false;
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

            CheckCollisionInitial();
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
            s_velMagnitude = Math.Max(velocityDelta.magnitude, 0.01f);

            s_playerCheck.Clear();
            CheckCollision();
            if (_pierceCount <= 0) return;

            CheckCollisionWorld();

            // Player moves on fixed time so only remove on fixed time
            if (_lastFixedTime != Time.fixedTime && _initialPlayers.Count != 0)
            {
                _initialPlayers.RemoveWhere(id => !s_playerCheck.Contains(id));
                _lastFixedTime = Time.fixedTime;
            }

            _distanceMoved += velocityDelta.magnitude;
        }

        private void CheckCollision()
        {
            RaycastHit[] hits;
            if (_settings!.HitSize == 0)
                hits = Physics.RaycastAll(s_ray, s_velMagnitude, _entityLayer);
            else
                hits = Physics.SphereCastAll(s_ray, _settings.HitSize, s_velMagnitude, _entityLayer);

            bool checkLOS = _settings.HitSize >= SightCheckMinSize;
            Array.Sort(hits, DistanceCompare);
            foreach (RaycastHit hit in hits)
            {
                if (checkLOS && Physics.Linecast(s_ray.origin, hit.point, EWCProjectileManager.MaskWorld)) continue;

                s_rayHit = hit;
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(s_rayHit);

                if (damageable != null)
                    DoDamage(damageable);

                if (_pierceCount <= 0) break;
            }
        }

        private void CheckCollisionWorld()
        {
            bool hit;
            if (_settings!.HitSizeWorld == 0)
                hit = Physics.Raycast(s_ray, out s_rayHit, s_velMagnitude, EWCProjectileManager.MaskWorld);
            else
                hit = Physics.SphereCast(s_ray, _settings!.HitSizeWorld, out s_rayHit, s_velMagnitude, EWCProjectileManager.MaskWorld);

            if (hit)
            {
                BulletHit(null);
                _base.Die();
            }
        }

        private static int DistanceCompare(RaycastHit a, RaycastHit b)
        {
            if (a.distance == b.distance) return 0;
            return a.distance < b.distance ? -1 : 1;
        }

        private void CheckCollisionInitial()
        {
            if (_settings!.HitSize < CollisionInitialMinSize) return;

            Vector3 pos = _weapon.Owner.Position;
            Collider[] colliders = Physics.OverlapSphere(pos, _settings.HitSize, _entityLayer);
            PriorityQueue<RaycastHit, float> hitQueue = new();

            s_ray.origin = pos;
            foreach (var collider in colliders)
            {
                if (Physics.Linecast(pos, collider.transform.position, EWCProjectileManager.MaskWorld)) continue;

                s_ray.direction = collider.transform.position - pos;
                if (!collider.Raycast(s_ray, out RaycastHit hit, _settings.HitSize)) continue;

                hitQueue.Enqueue(hit, hit.distance);
            }

            while (hitQueue.TryDequeue(out s_rayHit, out var _))
            {
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(s_rayHit);

                if (damageable != null)
                    DoDamage(damageable);

                if (_pierceCount <= 0) break;
            }
        }

        private void DoDamage(IDamageable damageable, bool cast = false)
        {
            if (!ShouldDamage(damageable)) return;

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

            if (!_pierce) return true;

            MonoBehaviour? baseDamageable = damageable.GetBaseDamagable().TryCast<MonoBehaviour>();
            if (baseDamageable != null)
            {
                if (_hitEnts.Contains(baseDamageable.GetInstanceID()))
                    return false;
                else
                    _hitEnts.Add(baseDamageable.GetInstanceID());
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

            bool backstab = false;
            WeaponPatches.ApplyEWCBulletHit(_baseCWC!, damageable, ref _hitData, _distanceMoved, _pierce, ref _baseDamage, ref backstab);
            float damage = _hitData.damage * _hitData.Falloff(_distanceMoved + _hitData.rayHit.distance);
            damageable?.BulletDamage(damage, _hitData.owner, _hitData.rayHit.point, _hitData.fireDir, _hitData.rayHit.normal, backstab, _hitData.staggerMulti, _hitData.precisionMulti);
        }
    }
}

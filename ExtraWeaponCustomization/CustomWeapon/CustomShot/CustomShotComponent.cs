﻿using Agents;
using EWC.API;
using EWC.CustomWeapon.Properties.Traits;
using EWC.Utils;
using FX_EffectSystem;
using Gear;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.CustomShot
{
    public sealed class CustomShotComponent
    {
        private readonly BulletWeapon _gun;
        private readonly CustomWeaponComponent _cwc;

        public CustomShotComponent(CustomWeaponComponent cwc)
        {
            _cwc = cwc;
            _gun = cwc.Gun!;
        }

        private int _normalCancel = 0;
        public bool CancelNormalShot
        {
            get => _normalCancel > 0;
            set => _normalCancel += value ? 1 : -1;
        }
        private int _normalOverride = 0;
        public bool OverrideNormalShot
        {
            get => _normalOverride > 0;
            set => _normalOverride += value ? 1 : -1;
        }
        private int _fxCancel = 0;
        public bool CancelAllFX
        {
            get => _fxCancel > 0;
            set => _fxCancel += value ? 1 : -1;
        }

        public bool OverrideVanillaShot => CancelNormalShot || OverrideNormalShot;

        private Properties.Traits.Projectile? _projectile;
        public Properties.Traits.Projectile? Projectile
        {
            get => _projectile;
            set
            {
                _projectile = value;
                OverrideNormalShot = value != null;
            }
        }

        private WallPierce? _wallPierce;
        public WallPierce? WallPierce
        {
            get => _wallPierce;
            set
            {
                _wallPierce = value;
                OverrideNormalShot = value != null;
            }
        }

        private ThickBullet? _thickBullet;
        public ThickBullet? ThickBullet
        {
            get => _thickBullet;
            set
            {
                _thickBullet = value;
                OverrideNormalShot = value != null;
            }
        }

        private static RaycastHit s_rayHit;

        public void FireVanilla(HitData hitData, Vector3 origin)
        {
            if (!CancelNormalShot)
                Fire(new Ray(origin, hitData.fireDir), hitData, LayerUtil.MaskFriendly);
        }

        public void FireSpread(Ray fireRay, HitData hitData, int friendlyMask = 0, IntPtr ignoreEnt = default)
        {
            CalcRayDir(ref fireRay, hitData.angOffsetX, hitData.angOffsetY, hitData.randomSpread);
            hitData.fireDir = fireRay.direction;
            Fire(fireRay, hitData, friendlyMask, ignoreEnt);
        }

        public void Fire(Ray fireRay, HitData hitData, int friendlyMask = 0, IntPtr ignoreEnt = default)
        {
            if (!_cwc.IsLocal)
            {
                if (Projectile == null)
                    FireVisual(fireRay, hitData);
                return;
            }

            if (Projectile != null)
            {
                Projectile.Fire(fireRay, hitData, ignoreEnt);
                return;
            }

            FireShotAPI.FirePreShotFiredCallback(hitData, fireRay);
            new ShotHitbox(this, hitData, fireRay, friendlyMask, ignoreEnt);
        }

        private void FireVisual(Ray fireRay, HitData hitData)
        {
            FireShotAPI.FirePreShotFiredCallback(hitData, fireRay);

            if (Physics.Raycast(fireRay, out s_rayHit, 20f, LayerUtil.MaskEntityAndWorld))
            {
                FX_Manager.EffectTargetPosition = s_rayHit.point;
                hitData.RayHit = s_rayHit;
                BulletWeapon.BulletHit(hitData.ToWeaponHitData(), false);
            }
            else
                FX_Manager.EffectTargetPosition = fireRay.origin + fireRay.direction * 20f;

            FireShotAPI.FireShotFiredCallback(hitData, fireRay.origin, FX_Manager.EffectTargetPosition);
            FX_Manager.PlayLocalVersion = false;
            BulletWeapon.s_tracerPool.AquireEffect().Play(null, _gun.MuzzleAlign.position, Quaternion.LookRotation(fireRay.direction));
        }

        public Vector3 CalcRayDir(Vector3 fireDir, float x, float y, float spread)
        {
            Transform cameraTransform = _cwc.IsLocal ? _gun.Owner.FPSCamera.transform : _gun.transform;
            Vector3 up = cameraTransform.up;
            Vector3 right = cameraTransform.right;
            if (x != 0)
                fireDir = Quaternion.AngleAxis(x, up) * fireDir;
            if (y != 0)
                fireDir = Quaternion.AngleAxis(y, right) * fireDir;
            if (spread != 0)
            {
                Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
                insideUnitCircle *= spread;
                fireDir = Quaternion.AngleAxis(insideUnitCircle.x, up) * fireDir;
                fireDir = Quaternion.AngleAxis(insideUnitCircle.y, right) * fireDir;
            }
            return fireDir;
        }

        public void CalcRayDir(ref Ray ray, float x, float y, float spread)
        {
            ray.direction = CalcRayDir(ray.direction, x, y, spread);
        }

        class ShotHitbox
        {
            private readonly CustomShotComponent _parent;
            private readonly BulletWeapon _gun;
            private readonly PlayerAgent _owner;
            private readonly HashSet<IntPtr>? _hitEnts;
            private readonly HitData _hitData;
            private readonly Ray _ray;
            private readonly WallPierce? _wallPierce;
            private readonly float _hitSize;
            private readonly float _hitSizeFriendly;
            private readonly SearchSetting _friendlySetting;
            private readonly int _friendlyMask;

            private int _pierceCount;

            private static RaycastHit s_rayHit;
            private const float SightCheckMinSize = 0.5f;

            public ShotHitbox(CustomShotComponent parent, HitData hitData, Ray fireRay, int friendlyMask, IntPtr ignoreEnt)
            {
                _parent = parent;
                _gun = _parent._gun;
                _owner = _gun.Owner;

                _ray = fireRay;
                _hitData = hitData;

                _pierceCount = 1;
                if (_gun.ArchetypeData.PiercingBullets)
                    _pierceCount = Math.Max(1, _gun.ArchetypeData.PiercingDamageCountLimit);

                if (_pierceCount > 1 || ignoreEnt != IntPtr.Zero)
                {
                    _hitEnts = new(_pierceCount);
                    if (ignoreEnt != IntPtr.Zero)
                        _hitEnts.Add(ignoreEnt);
                }

                _wallPierce = _parent.WallPierce;
                _hitSize = 0;
                _hitSizeFriendly = 0;
                _friendlySetting = SearchSetting.None;
                _friendlyMask = friendlyMask & (LayerUtil.MaskFriendly | LayerUtil.MaskOwner);
                if ((_friendlyMask & LayerUtil.MaskFriendly) != 0)
                    _friendlySetting |= SearchSetting.CheckFriendly;
                if ((_friendlyMask & LayerUtil.MaskOwner) != 0)
                    _friendlySetting |= SearchSetting.CheckOwner;

                if (parent.ThickBullet != null)
                {
                    _hitSize = parent.ThickBullet.HitSize;
                    _hitSizeFriendly = parent.ThickBullet.HitSizeFriendly;
                }

                Fire();
            }

            public void Fire()
            {
                // Stops at padlocks but that's the same behavior as vanilla so idc
                Vector3 fxPos;
                bool hitWall;
                if ((hitWall = Physics.Raycast(_ray, out var wallRayHit, _hitData.maxRayDist, LayerUtil.MaskWorld)) && _wallPierce == null)
                    fxPos = wallRayHit.point;
                else
                    fxPos = _ray.origin + _ray.direction * _hitData.maxRayDist;

                FireShotAPI.FirePreShotFiredCallback(_hitData, _ray);
                Fire_Internal(ref fxPos, hitWall, wallRayHit);
                FireShotAPI.FireShotFiredCallback(_hitData, _ray.origin, fxPos);
                if (!_parent.CancelAllFX)
                {
                    FX_Manager.EffectTargetPosition = fxPos;
                    FX_Manager.PlayLocalVersion = false;
                    BulletWeapon.s_tracerPool.AquireEffect().Play(null, _gun.MuzzleAlign.position, Quaternion.LookRotation(_ray.direction));
                }
            }

            public void Fire_Internal(ref Vector3 fxPos, bool hitWall, RaycastHit wallRayHit)
            {
                CheckCollisionInitial(ref fxPos);

                if (_pierceCount == 0) return;
                float maxDist = (_ray.origin - fxPos).magnitude;

                List<(IDamageable damageable, RaycastHit hit)> hits = new();
                // Get enemy/lock hits
                RaycastHit[] castHits;
                if (_hitSize == 0)
                {
                    castHits = RaycastOneOrAll(_ray, maxDist, LayerUtil.MaskEnemyDynamic);
                    for (int i = 0; i < castHits.Length; i++)
                    {
                        IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(castHits[i]);
                        if (damageable != null && !AlreadyHit(damageable))
                            hits.Add((damageable, castHits[i]));
                    }
                }
                else
                {
                    castHits = SpherecastOneOrAll(_ray, _hitSize, maxDist, LayerUtil.MaskEnemyDynamic);
                    for (int i = 0; i < castHits.Length; i++)
                    {
                        IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(castHits[i]);
                        if (damageable != null && !AlreadyHit(damageable) && castHits[i].distance > 0)
                        {
                            castHits[i].distance += _hitSize;
                            hits.Add((damageable, castHits[i]));
                        }
                    }
                }

                // Get friendly hits
                if (_hitSizeFriendly == 0)
                {
                    castHits = RaycastOneOrAll(_ray, maxDist, _friendlyMask);
                    for (int i = 0; i < castHits.Length; i++)
                    {
                        IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(castHits[i]);
                        if (damageable != null && !AlreadyHit(damageable))
                            hits.Add((damageable, castHits[i]));
                    }
                }
                else
                {
                    castHits = SpherecastOneOrAll(_ray, _hitSizeFriendly, maxDist, _friendlyMask);
                    for (int i = 0; i < castHits.Length; i++)
                    {
                        IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(castHits[i]);
                        if (damageable != null && !AlreadyHit(damageable) && castHits[i].distance > 0)
                        {
                            castHits[i].distance += _hitSizeFriendly;
                            hits.Add((damageable, castHits[i]));
                        }
                    }
                }

                if (_hitSize > 0)
                    SortUtil.SortWithWeakspotBuffer(hits);
                else
                    hits.Sort(SortUtil.RayhitTuple);

                foreach ((var damageable, var hit) in hits)
                {
                    Agent? agent = damageable.GetBaseAgent();
                    if (agent != null && _wallPierce?.IsTargetReachable(_owner.CourseNode, agent.CourseNode) == false) continue;

                    float hitSize = agent?.Type == AgentType.Player ? _hitSizeFriendly : _hitSize;
                    if (_wallPierce == null && !CheckLineOfSight(hit.collider, hit.point + hit.normal * hitSize, fxPos, hitSize, true)) continue;

                    if (hitSize != 0 && agent != null && CheckDirectHit(agent, ref s_rayHit))
                    {
                        _hitData.RayHit = s_rayHit;
                        fxPos = s_rayHit.point;
                    }
                    else
                        _hitData.RayHit = hit;

                    if (ShotManager.BulletHit(_hitData))
                        _pierceCount--;

                    if (_pierceCount <= 0) return;
                }

                if (_pierceCount > 0 && hitWall && !AlreadyHit(DamageableUtil.GetDamageableFromRayHit(wallRayHit)))
                {
                    fxPos = wallRayHit.point;
                    if (_wallPierce == null)
                    {
                        _hitData.RayHit = wallRayHit;
                        ShotManager.BulletHit(_hitData);
                    }
                }
            }

            // Check for targets within the initial sphere (if hitsize is big enough)
            private void CheckCollisionInitial(ref Vector3 fxPos)
            {
                if (_hitSize == 0 && _hitSizeFriendly == 0) return;

                Vector3 origin = _ray.origin;
                List<(Agent agent, RaycastHit hit)> hits = new();

                if (_hitSize > 0)
                {
                    var enemies = SearchUtil.GetEnemyHitsInRange(_ray, _hitSize, 180f, _owner.CourseNode);
                    hits.EnsureCapacity(enemies.Count);
                    foreach ((var enemy, var hit) in enemies)
                        hits.Add((enemy.Cast<Agent>(), hit));
                }
               
                if (_hitSizeFriendly > 0)
                {
                    var players = SearchUtil.GetPlayerHitsInRange(_ray, _hitSizeFriendly, 180f, _friendlySetting);
                    hits.EnsureCapacity(players.Count + hits.Count);
                    foreach ((var player, var hit) in players)
                        hits.Add((player.Cast<Agent>(), hit));
                }

                hits.Sort(SortUtil.RayhitTuple);

                foreach ((var agent, var hit) in hits)
                {
                    _hitData.RayHit = hit;
                    if (AlreadyHit(_hitData.damageable)) continue;
                    if (_wallPierce?.IsTargetReachable(_owner.CourseNode, agent.CourseNode) == false) continue;

                    float hitSize = agent.Type == AgentType.Player ? _hitSizeFriendly : _hitSize;
                    if (_wallPierce == null && !CheckLineOfSight(hit.collider, origin, fxPos, hitSize)) continue;

                    if (CheckDirectHit(agent, ref s_rayHit))
                    {
                        _hitData.RayHit = s_rayHit;
                        fxPos = s_rayHit.point;
                    }

                    if (ShotManager.BulletHit(_hitData))
                        _pierceCount--;

                    if (_pierceCount <= 0) break;
                }

                if (_hitSize == 0) return;

                List<RaycastHit> lockHits = SearchUtil.GetLockHitsInRange(_ray, _hitSize, 180f);
                lockHits.Sort(SortUtil.Rayhit);

                foreach (var hit in lockHits)
                {
                    _hitData.RayHit = hit;
                    if (AlreadyHit(_hitData.damageable)) continue;
                    if (_wallPierce == null && !CheckLineOfSight(hit.collider, origin, fxPos, _hitSize, true)) continue;

                    if (ShotManager.BulletHit(_hitData))
                        _pierceCount--;

                    if (_pierceCount <= 0) break;
                }
            }

            // Naive LOS check to ensure that some point on the bullet line can see the enemy
            private bool CheckLineOfSight(Collider collider, Vector3 startPos, Vector3 endPos, float hitSize, bool checkLock = false)
            {
                if (hitSize < SightCheckMinSize) return true;

                float remainingDist = (endPos - startPos).magnitude;
                float increment = Math.Max(0.1f, Math.Min(hitSize, remainingDist) / 10f);

                Vector3 colliderPos = collider.transform.position;
                Vector3 origin = startPos;

                float checkDistSqr = (origin - colliderPos).sqrMagnitude;
                float maxCheckDistSqr = checkDistSqr;
                int count = 0;
                while (remainingDist >= 0.1f && checkDistSqr <= maxCheckDistSqr)
                {
                    if (!Physics.Linecast(origin, collider.transform.position, out s_rayHit, LayerUtil.MaskWorld))
                        return true;
                    else if (checkLock && collider.gameObject.Pointer == s_rayHit.collider.gameObject.Pointer)
                        return true;

                    origin += _ray.direction * increment;
                    checkDistSqr = (origin - colliderPos).sqrMagnitude;
                    remainingDist -= increment;
                    count++;
                }

                return false;
            }

            private bool CheckDirectHit(Agent? agent, ref RaycastHit hit)
            {
                if (agent == null) return false;

                RaycastHit bestHit = new() { distance = float.MaxValue };

                foreach (Collider collider in agent.GetComponentsInChildren<Collider>())
                {
                    if (collider.GetComponent<IDamageable>() != null && collider.Raycast(_ray, out var tempHit, (collider.transform.position - _ray.origin).magnitude + 1f) && tempHit.distance < bestHit.distance)
                        bestHit = tempHit;
                }

                if (bestHit.distance != float.MaxValue)
                {
                    hit = bestHit;
                    return true;
                }
                return false;
            }

            private bool AlreadyHit(IDamageable? damageable)
            {
                if (damageable == null || _hitEnts == null) return false;
                return !_hitEnts.Add(damageable.GetBaseDamagable().Pointer);
            }

            private RaycastHit[] RaycastOneOrAll(Ray ray, float maxDist, int layerMask)
            {
                if (_hitEnts == null)
                {
                    if (Physics.Raycast(ray, out s_rayHit, maxDist, layerMask))
                        return new RaycastHit[] { s_rayHit };
                    else
                        return Array.Empty<RaycastHit>();
                }
                else
                    return Physics.RaycastAll(ray, maxDist, layerMask);
            }

            private RaycastHit[] SpherecastOneOrAll(Ray ray, float hitSize, float maxDist, int layerMask)
            {
                if (_hitEnts == null)
                {
                    if (Physics.SphereCast(ray, hitSize, out s_rayHit, maxDist, layerMask))
                        return new RaycastHit[] { s_rayHit };
                    else
                        return Array.Empty<RaycastHit>();
                }
                else
                    return Physics.SphereCastAll(ray, hitSize, maxDist, layerMask);
            }
        }
    }
}

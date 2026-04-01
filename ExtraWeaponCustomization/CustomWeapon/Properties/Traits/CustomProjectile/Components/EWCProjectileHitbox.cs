using Agents;
using AmorLib.Utils;
using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Patches.Gun;
using EWC.Utils;
using EWC.Utils.Extensions;
using FX_EffectSystem;
using Player;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public sealed class EWCProjectileHitbox
    {
        private readonly EWCProjectileComponentBase _base;

        // Set on init
        private Projectile _settings;
        private IOwnerComp _owner;
        private IntPtr _ownerPtr;
        private PlayerAgent? _ownerPlayer;
        private readonly HashSet<IntPtr> _initialPlayers = new();
        private ShotInfo.Const _origInfo;
        private int _friendlyLayer;
        private WallPierce? _wallPierce;
        private float _baseFalloff;
        private float _startLifetime;
        private eDimensionIndex _dimensionIndex;
        private bool _runHitTriggers = true;
        private Func<HitData, ContextController, bool>? _hitFuncOverride = null;
        private bool _enabled = false;
        public HitData HitData { get; private set; } = new(Enums.DamageType.Bullet);

        // Variables
        private int _pierceCount = 1;
        private int _ricochetCount = 0;
        private float _distanceMoved;
        private float _lastFixedTime;
        private SearchSetting _searchSettings = SearchSetting.ClosestHit | SearchSetting.IgnoreDupes;
        public readonly HashSet<IntPtr> HitEnts = new();
        private readonly Queue<(IntPtr, float)> _hitEntCooldowns = new();
        private float _ignoreWallsTime = 0f;

        // Static
        public const float MinCollisionDist = 0.05f;
        public const float MinCollisionSqrDist = MinCollisionDist * MinCollisionDist;
        public const float MinRicochetDist = 0.02f;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private static float s_velMagnitude;
        private readonly static List<RaycastHit> s_hits = new(50);
        private readonly static HashSet<IntPtr> s_playerCheck = new();
        private const float SightCheckMinSize = 0.5f;

#pragma warning disable CS8618
        // Hidden null warnings since other methods will initialize members prior to Update
        public EWCProjectileHitbox(EWCProjectileComponentBase comp) 
        {
            _base = comp;
        }
#pragma warning restore CS8618

        public void Init(Projectile projBase, Vector3 pos, Vector3 dir, HitData? hitData, IntPtr ignoreEnt, out RaycastHit? bounceHit)
        {
            bounceHit = null;
            if (_enabled || !_base.IsManaged) return;

            CustomWeaponComponent cwc = projBase.CWC;
            _enabled = true;
            _settings = projBase;
            _owner = cwc.Owner;
            _dimensionIndex = _owner.DimensionIndex;
            _runHitTriggers = true;
            _startLifetime = Time.time;

            // If it had temp properties, RunHitTriggers is permanently set on the copied context controller
            if (!cwc.HasTempProperties())
                _runHitTriggers = cwc.RunHitTriggers;

            s_ray.origin = pos;
            s_ray.direction = dir;
            _friendlyLayer = 0;
            _searchSettings = SearchSetting.ClosestHit | SearchSetting.IgnoreDupes;

            _ownerPlayer = _owner.IsType(Enums.OwnerType.Player) ? _owner.Player : null;
            _ownerPtr = _ownerPlayer?.Pointer ?? IntPtr.Zero;

            if (projBase.DamageFriendly)
            {
                if (!_owner.IsType(Enums.OwnerType.Local))
                    _friendlyLayer |= LayerUtil.MaskLocal;
                _searchSettings |= SearchSetting.CheckFriendly;
                _friendlyLayer |= LayerUtil.MaskFriendly;
                foreach (PlayerAgent agent in PlayerManager.PlayerAgentsInLevel)
                {
                    Vector3 diff = agent.Position - pos;
                    if (agent.Pointer != _ownerPtr && Vector3.Dot(dir, diff) < 0)
                        _initialPlayers.Add(agent.Pointer);
                }
            }

            if (projBase.DamageOwner && _ownerPtr != IntPtr.Zero)
            {
                if (_owner.IsType(Enums.OwnerType.Local))
                    _friendlyLayer |= LayerUtil.MaskLocal;
                else
                    _friendlyLayer |= LayerUtil.MaskFriendly;
                _searchSettings |= SearchSetting.CheckOwner;
                _initialPlayers.Add(_ownerPtr);
                _ownerPtr = IntPtr.Zero;
            }

            _wallPierce = _settings.WallPierce;
            _hitFuncOverride = projBase.HitFuncOverride;
            _pierceCount = hitData!.GetPierceOrFallback(cwc.Weapon);
            _ricochetCount = _settings.RicochetCount;
            HitData = new(hitData!);
            _baseFalloff = HitData.falloff;
            _origInfo = HitData.shotInfo.State;
            if (ignoreEnt != IntPtr.Zero)
                HitEnts.Add(ignoreEnt);

            _distanceMoved = 0;
            _lastFixedTime = Time.fixedTime;

            DoCollisionInitialWorld(out bounceHit);
            if (bounceHit != null && _ricochetCount-- <= 0)
                _base.Die();
        }

        public float GetFalloff() => HitData.CalcRawFalloff(_distanceMoved);
        public float GetMoveDist() => _distanceMoved;

        public void Die()
        {
            if (_base.IsManaged)
            {
                _settings.CWC.Invoke(new WeaponShotEndContext(Enums.DamageType.Bullet, HitData.shotInfo, _origInfo));
                _initialPlayers.Clear();
                HitEnts.Clear();
                _hitEntCooldowns.Clear();
                _ricochetCount = 0;
                _pierceCount = 0;
                _ignoreWallsTime = 0;
            }
            _enabled = false;
        }

        public bool Update(Vector3 position, Vector3 velocityDelta, out RaycastHit bounceHit)
        {
            bounceHit = default;
            RaycastHit? ricochet = null;
            if (!_enabled) return false;

            s_ray.origin = position;
            if (_settings.MaxRange > 0 && _distanceMoved + velocityDelta.magnitude >= _settings.MaxRange)
            {
                float finalDist = _settings.MaxRange - _distanceMoved;
                if (finalDist <= 0) return false;
                velocityDelta = velocityDelta.normalized * finalDist;
            }
            s_ray.direction = velocityDelta;
            s_velMagnitude = Math.Max(velocityDelta.magnitude, MinCollisionDist);

            s_playerCheck.Clear();
            bool hitWall = false;
            if (CheckCollisionWorld(ref ricochet))
            {
                hitWall = true;
                velocityDelta = ricochet.Value.point - position;
                s_velMagnitude = Math.Max(velocityDelta.magnitude, MinCollisionDist);
            }

            DoCollisionEntities(ref ricochet);

            if (_pierceCount <= 0) return false;

            if (hitWall)
                DoCollisionWorld(ref ricochet);

            if (ricochet != null)
            {
                if (_ricochetCount-- <= 0)
                    _base.Die();
                else
                {
                    if (_settings.RicochetIgnorePlayers)
                    {
                        _friendlyLayer &= ~(LayerUtil.MaskFriendly | LayerUtil.MaskLocal);
                        _searchSettings &= ~(SearchSetting.CheckFriendly | SearchSetting.CheckOwner);
                    }
                    if (_settings.RicochetResetHits)
                    {
                        HitEnts.Clear();
                        _hitEntCooldowns.Clear();
                    }
                }
            }

            if (!_enabled) return false; // Die on wall hit

            // Player moves on fixed time so only remove on fixed time
            if (_lastFixedTime != Time.fixedTime && _initialPlayers.Count != 0)
            {
                _initialPlayers.RemoveWhere(ptr => !s_playerCheck.Contains(ptr));
                _lastFixedTime = Time.fixedTime;
            }

            while (_hitEntCooldowns.TryPeek(out (IntPtr ptr, float endTime) pair) && pair.endTime < Clock.Time)
            {
                HitEnts.Remove(pair.ptr);
                _hitEntCooldowns.Dequeue();
            }

            _distanceMoved += velocityDelta.magnitude;
            if (ricochet != null)
            {
                bounceHit = ricochet.Value;
                return true;
            }
            return false;
        }

        private void DoCollisionEntities(ref RaycastHit? bounceHit)
        {
            SearchUtil.DupeCheckSet = HitEnts;
            if (_settings.HitSize == 0)
                s_hits.AddRange(SearchUtil.RaycastAll(s_ray, s_velMagnitude, LayerUtil.MaskEnemyDynamic, _searchSettings));
            else
            {
                // Get all enemies/players/locks inside the sphere as well as any we collide with on the cast.
                // Necessary to do every time since enemies inside the sphere on spawn might have LOS blocked.
                foreach ((_, RaycastHit hit) in SearchUtil.GetEnemyHitsInRange(s_ray, _settings.HitSize, 180f, CourseNodeUtil.GetCourseNode(s_ray.origin, _dimensionIndex), _searchSettings))
                    s_hits.Add(hit);
                s_hits.AddRange(SearchUtil.GetLockHitsInRange(s_ray, _settings.HitSize, 180f, _searchSettings));

                // Get all enemies/locks ahead of the projectile
                RaycastHit[] castHits = Physics.SphereCastAll(s_ray, _settings.HitSize, s_velMagnitude, LayerUtil.MaskEnemyDynamic);
                for (int i = 0; i < castHits.Length; i++)
                {
                    if (castHits[i].distance > 0) // Ignore anything overlapping the sphere (internal search hits these)
                    {
                        castHits[i].distance += _settings.HitSize; // Need the full distance to sort hits correctly
                        s_hits.Add(castHits[i]);
                    }
                }
            }

            if (_friendlyLayer != 0)
            {
                if (_settings.HitSizeFriendly == 0)
                    s_hits.AddRange(SearchUtil.RaycastAll(s_ray, s_velMagnitude, _friendlyLayer, _searchSettings));
                else
                {
                    foreach ((_, RaycastHit hit) in SearchUtil.GetPlayerHitsInRange(s_ray, _settings.HitSizeFriendly, 180f, _ownerPlayer, _searchSettings))
                        s_hits.Add(hit);

                    // Get all enemies/locks ahead of the projectile
                    RaycastHit[] castHits = Physics.SphereCastAll(s_ray, _settings.HitSizeFriendly, s_velMagnitude, _friendlyLayer);
                    for (int i = 0; i < castHits.Length; i++)
                    {
                        if (castHits[i].distance > 0) // Ignore anything overlapping the sphere (internal search hits these)
                        {
                            castHits[i].distance += _settings.HitSize; // Need the full distance to sort hits correctly
                            s_hits.Add(castHits[i]);
                        }
                    }
                }                    
            }

            int prevCount = _pierceCount;
            bool checkLOS = _settings.HitSize >= SightCheckMinSize;
            if (_settings.HitSize > 0)
                SortUtil.SortWithWeakspotBuffer(s_hits);
            else
                s_hits.Sort(SortUtil.Rayhit);

            foreach (RaycastHit hit in s_hits)
            {
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(hit);
                if (damageable == null) continue;
                if (AlreadyHit(damageable)) continue;
                if (checkLOS && _wallPierce == null
                    && Physics.Linecast(hit.point, s_ray.origin, out s_rayHit, LayerUtil.MaskWorld)
                    && s_rayHit.collider.gameObject.Pointer != hit.collider.gameObject.Pointer) // Needed for locks
                    continue;

                s_rayHit = hit;

                if (damageable != null)
                    DoDamage(damageable);

                if (_pierceCount <= 0) break;
            }

            if (_pierceCount > 0 && _pierceCount != prevCount)
            {
                if (_settings.RicochetOnHit)
                {
                    s_rayHit.point += (s_ray.origin - s_rayHit.point) * _settings.HitSize;
                    bounceHit = s_rayHit;
                }
                _base.Homing.UpdateOnPierce();
            }
            s_hits.Clear();
        }

        private bool CheckCollisionWorld([NotNullWhen(true)] ref RaycastHit? bounceHit)
        {
            if (_wallPierce != null || Clock.Time < _ignoreWallsTime) return false;

            bool hit;
            if (_settings.HitSizeWorld == 0)
                hit = Physics.Raycast(s_ray, out s_rayHit, s_velMagnitude, LayerUtil.MaskWorld);
            else
            {
                hit = Physics.SphereCast(s_ray, _settings.HitSizeWorld, out s_rayHit, s_velMagnitude, LayerUtil.MaskWorld);
                if (hit && s_rayHit.distance == 0)
                {
                    s_ray.direction = s_rayHit.point - s_ray.origin;
                    hit = Physics.Raycast(s_ray, out s_rayHit, _settings.HitSizeWorld, LayerUtil.MaskWorld);
                }
            }

            if (hit)
            {
                s_rayHit.point += (s_ray.origin - s_rayHit.point) * _settings.HitSizeWorld;
                bounceHit = s_rayHit;
            }
            return hit;
        }

        private void DoCollisionWorld(ref RaycastHit? bounceHit)
        {
            if (bounceHit == null) return;

            s_rayHit = bounceHit.Value;
            BulletHit(DamageableUtil.GetDamageableFromRayHit(s_rayHit), true);
        }

        private void DoCollisionInitialWorld(out RaycastHit? bounceHit)
        {
            bounceHit = null;
            if (_settings.HitSizeWorld == 0) return;

            Vector3 pos = s_ray.origin;
            Collider[] colliders = Physics.OverlapSphere(pos, _settings.HitSizeWorld, LayerUtil.MaskWorld);
            if (colliders.Length == 0) return;

            s_rayHit.distance = float.MaxValue;
            foreach (var collider in colliders)
            {
                s_ray.direction = collider.transform.position - pos;
                if (!collider.Raycast(s_ray, out var hit, _settings.HitSizeWorld)) continue;

                if (hit.distance < s_rayHit.distance)
                    s_rayHit = hit;
            }

            if (s_rayHit.distance == float.MaxValue) return;

            if (BulletHit(DamageableUtil.GetDamageableFromRayHit(s_rayHit), true))
                bounceHit = s_rayHit;
        }

        private void DoDamage(IDamageable damageable)
        {
            if (!ShouldDamage(damageable)) return;

            IntPtr basePtr = damageable.GetBaseDamagable().Pointer;
            HitEnts.Add(basePtr);
            if (_settings.HitCooldown >= 0)
                _hitEntCooldowns.Enqueue((basePtr, Clock.Time + _settings.HitCooldown));
            if (damageable.GetBaseAgent() != null)
                _ignoreWallsTime = Clock.Time + _settings.HitIgnoreWallsDuration;

            if (!BulletHit(damageable, false)) return;

            if (--_pierceCount <= 0)
                _base.Die();
        }

        private bool ShouldDamage(IDamageable damageable)
        {
            if (damageable.GetBaseDamagable().GetHealthRel() <= 0) return false;

            Agent? agent = damageable.GetBaseAgent();
            if (agent != null)
            {
                if (agent.Type == AgentType.Player)
                {
                    var agentPtr = agent.Pointer;
                    if (!_settings.DamageOwner && _ownerPtr == agentPtr)
                        return false;
                    else if (!_settings.DamageFriendly && _ownerPtr != agentPtr)
                        return false;

                    if (_initialPlayers.Contains(agentPtr))
                    {
                        s_playerCheck.Add(agentPtr);
                        return false;
                    }
                }
                
                if (!agent.Alive)
                    return false;
            }

            return _wallPierce?.IsTargetReachable(_owner.CourseNode, agent?.CourseNode) != false;
        }

        private bool AlreadyHit(IDamageable? damageable)
        {
            if (damageable == null) return false;
            return HitEnts.Contains(damageable.GetBaseDamagable().Pointer);
        }

        private void DoImpactFX(IDamageable? damageable)
        {
            GameObject gameObject = s_rayHit.collider.gameObject;
            var colliderMaterial = gameObject.GetComponent<ColliderMaterial>();
            bool isDecalsAllowed = (LayerUtil.MaskDecalValid & gameObject.gameObject.layer) == 0;

            FX_GroupName impactFX = FX_GroupName.Impact_Concrete;
            if (colliderMaterial != null)
                impactFX = (FX_GroupName)colliderMaterial.MaterialId;
            else if (damageable?.GetBaseAgent()?.Type == AgentType.Player)
                impactFX = FX_GroupName.Impact_PlayerBody;

            FX_Manager.PlayEffect(false, impactFX, null, s_rayHit.point, Quaternion.LookRotation(s_rayHit.normal), isDecalsAllowed);
        }

        private bool BulletHit(IDamageable? damageable, bool terrain)
        {
            HitData.ResetDamage();
            HitData.fireDir = (_settings.HitFromOwnerPos ? s_rayHit.point - _owner.FirePos : s_ray.direction).normalized;
            HitData.RayHit = s_rayHit;
            HitData.falloff = HitData.CalcFalloff(_distanceMoved) * _baseFalloff;

            if (!terrain || _settings.EnableTerrainHitFX)
                DoImpactFX(damageable);

            API.ProjectileAPI.FireProjectileHitCallback(_base, damageable);

            foreach (var statChange in _settings.StatChanges)
            {
                float mod = Time.time.Map(_startLifetime + statChange.Delay, _startLifetime + statChange.Delay + statChange.ChangeTime, 1f, statChange.EndFrac, statChange.Exponent);
                switch (statChange.StatType)
                {
                    case Enums.StatType.Damage:
                        HitData.damage *= mod;
                        break;
                    case Enums.StatType.Precision:
                        HitData.precisionMulti *= mod;
                        break;
                    case Enums.StatType.Stagger:
                        HitData.staggerMulti *= mod;
                        break;
                }
            }

            if (_hitFuncOverride != null)
            {
                var result = _hitFuncOverride(HitData, _base.ContextController);
                _base.OnHit(HitData.damageType);
                return result;
            }

            ToggleRunTriggers(false);
            WeaponPatches.ApplyEWCHit(_settings.CWC, _base.ContextController, HitData, out var backstab);
            ToggleRunTriggers(true);

            float damage = HitData.damage * HitData.falloff;
            if (damageable != null)
            {
                // EXP sentry checks won't work on projectiles
                if (_owner.IsType(Enums.OwnerType.Sentry) && _owner.Player?.IsLocallyOwned == true)
                    damage /= EXPAPIWrapper.GetDamageMod(true, Enums.WeaponType.BulletWeapon);

                damageable?.BulletDamage(damage, HitData.owner, HitData.hitPos, HitData.fireDir, HitData.RayHit.normal, backstab, HitData.staggerMulti, HitData.precisionMulti);
            }
            _base.OnHit(HitData.damageType);
            return true;
        }

        private void ToggleRunTriggers(bool enable)
        {
            if (!_runHitTriggers)
                _settings.CWC.RunHitTriggers = enable;
        }
    }
}

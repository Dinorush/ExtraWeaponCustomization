using Agents;
using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Patches.Gun;
using EWC.Utils;
using EWC.Utils.Extensions;
using FX_EffectSystem;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public sealed class EWCProjectileHitbox
    {
        private readonly EWCProjectileComponentBase _base;

        // Set on init
        private Projectile _settings;
        private IOwnerComp _owner;
        private readonly HashSet<IntPtr> _initialPlayers = new();
        private ShotInfo.Const _origInfo;
        private int _friendlyLayer;
        private WallPierce? _wallPierce;
        private float _baseFalloff;
        private float _startLifetime;
        private eDimensionIndex _dimensionIndex;
        private bool _runHitTriggers = true;
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

            CustomGunComponent cgc = projBase.CGC;
            _enabled = true;
            _settings = projBase;
            _owner = cgc.Owner;
            _dimensionIndex = _owner.DimensionIndex;
            _runHitTriggers = true;
            _startLifetime = Time.time;

            // If it had temp properties, RunHitTriggers is permanently set on the copied context controller
            if (!cgc.HasTempProperties())
                _runHitTriggers = cgc.RunHitTriggers;

            s_ray.origin = pos;
            s_ray.direction = dir;
            _friendlyLayer = 0;
            _searchSettings = SearchSetting.ClosestHit | SearchSetting.IgnoreDupes;
            IntPtr ownerPtr = _owner.Player.Pointer;
            if (projBase.DamageOwner)
            {
                _searchSettings |= SearchSetting.CheckOwner;
                _initialPlayers.Add(ownerPtr);
                _friendlyLayer |= LayerUtil.MaskOwner;
            }

            if (projBase.DamageFriendly)
            {
                _searchSettings |= SearchSetting.CheckFriendly;
                _friendlyLayer |= LayerUtil.MaskFriendly;
                foreach (PlayerAgent agent in PlayerManager.PlayerAgentsInLevel)
                {
                    Vector3 diff = agent.Position - pos;
                    if (agent.Pointer != ownerPtr && Vector3.Dot(dir, diff) < 0)
                        _initialPlayers.Add(agent.Pointer);
                }
            }

            _wallPierce = _settings.WallPierce;
            _pierceCount = _settings.PierceLimit;
            _ricochetCount = _settings.RicochetCount;
            HitData = new(hitData!);
            _baseFalloff = HitData.falloff;
            _origInfo = HitData.shotInfo.State;
            if (ignoreEnt != IntPtr.Zero)
                HitEnts.Add(ignoreEnt);

            _distanceMoved = 0;
            _lastFixedTime = Time.fixedTime;

            CheckCollisionInitialWorld(out bounceHit);
            if (bounceHit != null && _ricochetCount-- <= 0)
                _base.Die();
        }

        public float GetFalloff() => HitData.CalcRawFalloff(_distanceMoved);

        public void Die()
        {
            if (_base.IsManaged)
            {
                _settings.CWC.Invoke(new WeaponShotEndContext(Enums.DamageType.Bullet, HitData.shotInfo, _origInfo));
                _initialPlayers.Clear();
                HitEnts.Clear();
                _hitEntCooldowns.Clear();
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
            s_ray.direction = velocityDelta;
            s_velMagnitude = Math.Max(velocityDelta.magnitude, MinCollisionDist);

            s_playerCheck.Clear();
            CheckCollision(ref ricochet);
            if (_pierceCount <= 0) return false;

            if (_wallPierce == null && Clock.Time >= _ignoreWallsTime)
                CheckCollisionWorld(ref ricochet);

            if (ricochet != null && _ricochetCount-- <= 0)
                _base.Die();

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

        private void CheckCollision(ref RaycastHit? bounceHit)
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
                    foreach ((_, RaycastHit hit) in SearchUtil.GetPlayerHitsInRange(s_ray, _settings.HitSizeFriendly, 180f, _searchSettings))
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

        private void CheckCollisionWorld(ref RaycastHit? bounceHit)
        {
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

            if (hit && BulletHit(null))
            {
                s_rayHit.point += (s_ray.origin - s_rayHit.point) * _settings.HitSizeWorld;
                bounceHit = s_rayHit;
            }
        }

        private void CheckCollisionInitialWorld(out RaycastHit? bounceHit)
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

            if (BulletHit(null))
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

            if (!BulletHit(damageable)) return;

            if (--_pierceCount <= 0)
                _base.Die();
        }

        private bool ShouldDamage(IDamageable damageable)
        {
            if (damageable.GetBaseDamagable().GetHealthRel() <= 0) return false;

            Agent? agent = damageable.GetBaseAgent();
            if (agent != null)
            {
                if (agent.Type == AgentType.Player && _initialPlayers.Contains(agent.Pointer))
                {
                    s_playerCheck.Add(agent.Pointer);
                    return false;
                }
                else if (!agent.Alive)
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
            if (damageable == null && !_settings.EnableTerrainHitFX) return;

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

        private bool BulletHit(IDamageable? damageable)
        {
            HitData.ResetDamage();
            HitData.fireDir = (_settings.HitFromOwnerPos ? s_rayHit.point - _owner.FirePos : s_ray.direction).normalized;
            HitData.RayHit = s_rayHit;
            HitData.falloff = HitData.CalcFalloff(_distanceMoved) * _baseFalloff;

            DoImpactFX(damageable);

            API.ProjectileAPI.FireProjectileHitCallback(_base, damageable);

            if (_settings.HitFuncOverride != null)
                return _settings.HitFuncOverride(HitData, _base.ContextController);
            
            ToggleRunTriggers(false);
            WeaponPatches.ApplyEWCHit(_settings.CWC, _base.ContextController, HitData, out var backstab);
            ToggleRunTriggers(true);

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

            float damage = HitData.damage * HitData.falloff;
            damageable?.BulletDamage(damage, HitData.owner, HitData.hitPos, HitData.fireDir, HitData.RayHit.normal, backstab, HitData.staggerMulti, HitData.precisionMulti);
            return true;
        }

        private void ToggleRunTriggers(bool enable)
        {
            if (!_runHitTriggers)
                _settings.CWC.RunHitTriggers = enable;
        }
    }
}

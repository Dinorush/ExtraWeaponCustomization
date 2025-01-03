using Agents;
using EWC.CustomWeapon.WeaponContext;
using EWC.Patches;
using EWC.Utils;
using FX_EffectSystem;
using GameData;
using Gear;
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
        private Projectile? _settings;
        private BulletWeapon _weapon;
        private ContextController _contextController;
        private readonly HashSet<IntPtr> _initialPlayers = new();
        private readonly HitData _hitData = new();
        private int _entityLayer;
        private WallPierce? _wallPierce;
        private bool _enabled = false;

        // Variables
        private bool _pierce;
        private int _pierceCount = 1;
        private float _distanceMoved;
        private float _baseDamage;
        private float _basePrecision;
        private float _lastFixedTime;
        private SearchSetting _searchSettings = SearchSetting.CacheHit;
        public readonly HashSet<IntPtr> HitEnts = new();
        private readonly Queue<(IntPtr, float)> _hitEntCooldowns = new();
        private float _ignoreWallsTime = 0f;

        // Static
        public const float MinCollisionDist = 0.03f;
        public const float MinCollisionSqrDist = MinCollisionDist * MinCollisionDist;
        private static ContextController? s_currentController;
        private static float s_lastControllerTime = 0f;
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

        public void Init(Projectile projBase)
        {
            if (_enabled || !_base.IsLocal) return;

            CustomWeaponComponent cwc = projBase.CWC;
            _enabled = true;
            _settings = projBase;
            _weapon = cwc.Gun!;

            if (cwc.HasTempProperties())
            {
                // Properties can never change in the same frame, so we can batch shotguns.
                if (s_lastControllerTime != Clock.Time)
                {
                    s_currentController = new(_settings.CWC.GetContextController());
                    s_lastControllerTime = Clock.Time;
                }
                _contextController = s_currentController!;
            }
            else
                _contextController = cwc.GetContextController();

            Vector3 pos = _weapon.Owner.FPSCamera.Position;
            Vector3 dir = _weapon.Owner.FPSCamera.CameraRayDir;

            _entityLayer = LayerUtil.MaskEnemyDynamic;
            _searchSettings = SearchSetting.CacheHit;
            IntPtr ownerPtr = _weapon.Owner.Pointer;
            if (projBase.DamageOwner)
            {
                _searchSettings |= SearchSetting.CheckOwner;
                _initialPlayers.Add(ownerPtr);
                _entityLayer |= LayerUtil.MaskOwner;
            }

            if (projBase.DamageFriendly)
            {
                _searchSettings |= SearchSetting.CheckFriendly;
                _entityLayer |= LayerUtil.MaskFriendly;
                foreach (PlayerAgent agent in PlayerManager.PlayerAgentsInLevel)
                {
                    Vector3 diff = agent.Position - pos;
                    if (agent.Pointer != ownerPtr && Vector3.Dot(dir, diff) > 0)
                        _initialPlayers.Add(agent.Pointer);
                }
            }

            _wallPierce = cwc.GetTrait<WallPierce>();

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

            CheckCollisionInitialWorld();
        }

        public void Die()
        {
            _enabled = false;
            _initialPlayers.Clear();
            HitEnts.Clear();
            _hitEntCooldowns.Clear();
            _ignoreWallsTime = 0;
        }

        public void Update(Vector3 position, Vector3 velocityDelta)
        {
            // Enabled should be false if not local, but attempting a fix for client projectiles not showing up sometimes
            if (!_enabled || !_base.IsLocal) return;

            if (_settings == null || _weapon == null)
            {
                _base.Die();
                return;
            }

            s_ray.origin = position;
            s_ray.direction = velocityDelta;
            s_velMagnitude = Math.Max(velocityDelta.magnitude, MinCollisionDist);

            s_playerCheck.Clear();
            CheckCollision();
            if (_pierceCount <= 0) return;

            if (_wallPierce == null && Clock.Time >= _ignoreWallsTime)
                CheckCollisionWorld();

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
        }

        private void CheckCollision()
        {
            if (_settings!.HitSize == 0)
                s_hits.AddRange(Physics.RaycastAll(s_ray, s_velMagnitude, _entityLayer));
            else
            {
                // Get all enemies/players/locks inside the sphere as well as any we collide with on the cast.
                // Necessary to do every time since enemies inside the sphere on spawn might have LOS blocked.
                SearchUtil.DupeCheckSet = HitEnts;
                foreach ((_, RaycastHit hit) in SearchUtil.GetEnemyHitsInRange(s_ray, _settings.HitSize, 180f, SearchUtil.GetCourseNode(s_ray.origin, _weapon.Owner), _searchSettings | SearchSetting.IgnoreDupes))
                    s_hits.Add(hit);
                s_hits.AddRange(SearchUtil.GetLockHitsInRange(s_ray, _settings.HitSize, 180f));

                if (_settings.DamageFriendly || _settings.DamageOwner)
                    foreach ((_, RaycastHit hit) in SearchUtil.GetPlayerHitsInRange(s_ray, _settings.HitSize, 180f, _searchSettings))
                        s_hits.Add(hit);

                // Get all enemies/locks ahead of the projectile
                RaycastHit[] castHits = Physics.SphereCastAll(s_ray, _settings.HitSize, s_velMagnitude, _entityLayer);
                for (int i = 0; i < castHits.Length; i++)
                {
                    if (castHits[i].distance > 0) // Ignore anything overlapping the sphere (internal search hits these)
                    {
                        castHits[i].distance += _settings.HitSize; // Need the full distance to sort hits correctly
                        s_hits.Add(castHits[i]);
                    }
                }
            }

            int prevCount = _pierceCount;
            bool checkLOS = _settings.HitSize >= SightCheckMinSize;
            SortUtil.SortWithWeakspotBuffer(s_hits);
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
                _base.Homing.UpdateOnPierce();
            s_hits.Clear();
        }

        private void CheckCollisionWorld()
        {
            bool hit;
            if (_settings!.HitSizeWorld == 0)
                hit = Physics.Raycast(s_ray, out s_rayHit, s_velMagnitude, LayerUtil.MaskWorld);
            else
                hit = Physics.SphereCast(s_ray, _settings!.HitSizeWorld, out s_rayHit, s_velMagnitude, LayerUtil.MaskWorld);

            if (hit)
            {
                BulletHit(null);
                _base.Die();
            }
        }

        private void CheckCollisionInitialWorld()
        {
            if (_settings!.HitSizeWorld == 0) return;

            Vector3 pos = _weapon.Owner.FPSCamera.Position;
            Collider[] colliders = Physics.OverlapSphere(pos, _settings.HitSizeWorld, LayerUtil.MaskWorld);
            if (colliders.Length == 0) return;

            s_rayHit.distance = float.MaxValue;
            s_ray.origin = pos;
            foreach (var collider in colliders)
            {
                s_ray.direction = collider.transform.position - pos;
                if (!collider.Raycast(s_ray, out var hit, _settings.HitSizeWorld)) continue;

                if (hit.distance < s_rayHit.distance)
                    s_rayHit = hit;
            }

            if (s_rayHit.distance == float.MaxValue) return;

            BulletHit(null);
            _base.Die();
        }

        private void DoDamage(IDamageable damageable)
        {
            if (!ShouldDamage(damageable)) return;

            BulletHit(damageable);

            IntPtr basePtr = damageable.GetBaseDamagable().Pointer;
            HitEnts.Add(basePtr);
            if (_settings!.HitCooldown >= 0)
                _hitEntCooldowns.Enqueue((basePtr, Clock.Time + _settings.HitCooldown));
            if (damageable.GetBaseAgent() != null)
                _ignoreWallsTime = Clock.Time + _settings.HitIgnoreWallsDuration;

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

            return _wallPierce?.IsTargetReachable(_weapon.Owner.CourseNode, agent?.CourseNode) != false;
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

        // Can't call base game bullet hit since WeaponPatches assumes hitscan bullets for its logic
        private void BulletHit(IDamageable? damageable)
        {
            _hitData.damage = _baseDamage;
            _hitData.precisionMulti = _basePrecision;
            _hitData.fireDir = (_settings!.HitFromOwnerPos ? s_rayHit.point - _weapon.Owner.FPSCamera.Position : s_ray.direction).normalized;
            _hitData.RayHit = s_rayHit;
            _hitData.SetFalloff(_distanceMoved);

            DoImpactFX(damageable);

            WeaponPatches.ApplyEWCHit(_contextController, _weapon, _hitData, _pierce, ref _baseDamage, out bool backstab, _settings.RunHitTriggers);
            float damage = _hitData.damage * _hitData.falloff;
            damageable?.BulletDamage(damage, _hitData.owner, _hitData.hitPos, _hitData.fireDir, _hitData.RayHit.normal, backstab, _hitData.staggerMulti, _hitData.precisionMulti);
        }
    }
}

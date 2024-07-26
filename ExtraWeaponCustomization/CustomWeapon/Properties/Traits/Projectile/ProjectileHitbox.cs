using Agents;
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

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class ProjectileHitbox : MonoBehaviour
    {
        // Constants set on construction
        private Projectile? _Base;
        private CustomWeaponComponent _BaseCWC;
        private BulletWeapon _Weapon;
        private List<GameObject>? _HitEnemies;
        private HashSet<int> _InitialPlayers;
        private uint _SearchID;
        private WeaponHitData _HitData;

        // Variables
        private int _pierceCount;
        private float _distanceMoved;
        private float _baseDamage;
        private float _basePrecision;

        // Static
        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private static int MaskEntityAndWorld => LayerManager.MASK_BULLETWEAPON_RAY;
        private static int MaskEntity => LayerManager.MASK_BULLETWEAPON_PIERCING_PASS;
        private static int MaskWorld => LayerManager.MASK_BULLETWEAPON_PIERCING_PASS;
        private readonly static HashSet<int> s_PlayerCheck = new(4);
        private const float MaxLifetime = 20f;

#pragma warning disable CS8618
        // Hidden null warnings since other methods will initialize members prior to Update
        public ProjectileHitbox(IntPtr value) : base(value) { }
#pragma warning restore CS8618

        private void Start()
        {
            enabled = false;
        }

        public void CWCInit(CustomWeaponComponent cwc, Projectile projBase)
        {
            if (enabled) return;

            if (!cwc.Weapon.Owner.IsLocallyOwned)
            {
                Destroy(gameObject);
                return;
            }

            enabled = true;
            Destroy(gameObject, MaxLifetime);

            _Base = projBase;
            _BaseCWC = cwc;
            _Weapon = cwc.Weapon;
            _InitialPlayers = new(4);

            Vector3 pos = _Weapon.Owner.Position;
            Vector3 dir = _Weapon.Owner.FPSCamera.CameraRayDir;
            foreach (PlayerAgent agent in PlayerManager.PlayerAgentsInLevel)
            {
                Vector3 diff = agent.Position - pos;
                if (diff == Vector3.zero || Vector3.Dot(dir, diff) > 0)
                    _InitialPlayers.Add(agent.GetInstanceID());
            }

            _pierceCount = 1;
            if (_Weapon.ArchetypeData.PiercingBullets && _Weapon.ArchetypeData.PiercingDamageCountLimit > 1)
            {
                _SearchID = _Weapon.m_damageSearchID;
                _pierceCount = _Weapon.ArchetypeData.PiercingDamageCountLimit;
                _HitEnemies = new();
            }

            ArchetypeDataBlock archData = _Weapon.ArchetypeData;
            _HitData = new()
            {
                owner = _Weapon.Owner,
                damage = archData.Damage,
                damageFalloff = archData.DamageFalloff,
                staggerMulti = archData.StaggerDamageMulti,
                precisionMulti = archData.PrecisionDamageMulti
            };

            _baseDamage = _HitData.damage;
            _basePrecision = _HitData.precisionMulti;
            Init(dir * _Base.Speed, _Base.Gravity * Physics.gravity.y);
        }

        protected override void FixedUpdate()
        {
            if (_Base == null)
            {
                Destroy(gameObject);
                return;
            }

            base.FixedUpdate();
        }

        protected override void Update()
        {
            if (_Base == null)
            {
                Destroy(gameObject);
                return;
            }

            base.Update();

            s_ray.origin = Position;
            s_ray.direction = Velocity * Time.deltaTime;

            SetPierceCollisions(false);
            if (_Base!.Size == _Base.SizeWorld)
                CheckCollision(_Base.Size, MaskEntityAndWorld);
            else
            {
                CheckCollision(_Base.Size, MaskEntity);
                CheckCollision(_Base.SizeWorld, MaskWorld);
            }
            SetPierceCollisions(true);
            _distanceMoved += Velocity.magnitude * Time.deltaTime;
        }   

        private void CheckCollision(float size, int layerMask)
        {
            s_PlayerCheck.Clear();
            RaycastHit[] hits;
            if (size == 0)
                hits = Physics.RaycastAll(s_ray, 1f, layerMask);
            else
                hits = Physics.SphereCastAll(s_ray, size, 1f, layerMask);

            Array.Sort(hits, DistanceCompare);
            foreach (RaycastHit hit in hits)
            {
                if (_pierceCount <= 0) break;
                s_rayHit = hit;
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(s_rayHit);
                if (damageable != null)
                    DoDamage(damageable);
                else // Hit a wall
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (_InitialPlayers.Count != 0)
                _InitialPlayers.RemoveWhere(id => !s_PlayerCheck.Contains(id));
        }

        private static int DistanceCompare(RaycastHit a, RaycastHit b)
        {
            if (a.distance == b.distance) return 0;
            return a.distance < b.distance ? -1 : 1;
        }

        private void DoDamage(IDamageable damageable, bool cast = false)
        {
            if (!ShouldDamage(damageable)) return;

            BulletHit(damageable);

            if (--_pierceCount <= 0)
                Destroy(gameObject);
        }

        private bool ShouldDamage(IDamageable damageable)
        {
            Agent? agent = damageable.GetBaseAgent();
            if (agent == null || !agent.Alive) return false;

            if (agent.Type == AgentType.Player && _InitialPlayers.Contains(agent.GetInstanceID()))
            {
                s_PlayerCheck.Add(agent.GetInstanceID());
                return false;
            }

            if (_pierceCount <= 1) return true;

            IDamageable? baseDamageable = damageable.GetBaseDamagable();
            if (baseDamageable != null)
            {
                if (baseDamageable.TempSearchID == _SearchID)
                {
                    if (agent.Type == AgentType.Enemy)
                        EWCLogger.Error("Projectile found an already hit enemy!");
                    return false;
                }
                else
                    baseDamageable.TempSearchID = _SearchID;
            }

            if (agent.Type == AgentType.Enemy)
            {
                GameObject go = damageable.GetBaseAgent().gameObject;
                _HitEnemies.Add(go);
                SetPierceCollision(go, false);
            }

            return true;
        }

        private void SetPierceCollisions(bool collision)
        {
            if (_HitEnemies == null) return;

            foreach (var go in _HitEnemies)
                SetPierceCollision(go, collision);
        }

        private void SetPierceCollision(GameObject go, bool collision)
        {
            int layer = collision ? LayerManager.LAYER_ENEMY_DAMAGABLE : 0;
            go.layer = layer;
        }

        // Can't call base game bullet hit since WeaponPatches assumes hitscan bullets for its logic
        private void BulletHit(IDamageable damageable)
        {
            _HitData.damage = _baseDamage;
            _HitData.precisionMulti = _basePrecision;
            _HitData.fireDir = s_ray.direction.normalized;
            _HitData.rayHit = s_rayHit;

            GameObject gameObject = s_rayHit.collider.gameObject;
            var colliderMaterial = gameObject.GetComponent<ColliderMaterial>();
            bool isDecalsAllowed = (LayerManager.MASK_VALID_FOR_DECALS & gameObject.gameObject.layer) == 0;
            if (colliderMaterial != null)
                FX_Manager.PlayEffect(false, (FX_GroupName)colliderMaterial.MaterialId, null, s_rayHit.point, Quaternion.LookRotation(s_rayHit.normal), isDecalsAllowed);
            else
                FX_Manager.PlayEffect(false, FX_GroupName.Impact_Concrete, null, s_rayHit.point, Quaternion.LookRotation(s_rayHit.normal), isDecalsAllowed);

            WeaponPatches.ApplyEWCBulletHit(_BaseCWC!, damageable, ref _HitData, _distanceMoved, _SearchID, ref _baseDamage);
            float damage = _HitData.damage * _HitData.Falloff(_distanceMoved);
            damageable?.BulletDamage(damage, _HitData.owner, _HitData.rayHit.point, _HitData.fireDir, _HitData.rayHit.normal, allowDirectionalBonus: true, _HitData.staggerMulti, _HitData.precisionMulti);
        }
    }
}

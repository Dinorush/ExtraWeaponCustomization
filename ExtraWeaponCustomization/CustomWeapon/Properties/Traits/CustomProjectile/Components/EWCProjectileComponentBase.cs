using EWC.API;
using EWC.Attributes;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.Utils;
using EWC.Utils.Extensions;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using System;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public abstract class EWCProjectileComponentBase : MonoBehaviour
    {
        private static readonly System.Random Random = new();

#pragma warning disable CS8618 // Settings is set on Init call which will always run before it is used
        public EWCProjectileComponentBase(IntPtr ptr) : base(ptr)
        {
            Hitbox = new(this);
            Homing = new(this);
            _inactiveCallback = new(() => _inactiveLifetime, Cleanup);
        }
#pragma warning restore CS8618

        public readonly EWCProjectileHitbox Hitbox;
        public readonly EWCProjectileHoming Homing;
        [HideFromIl2Cpp]
        public Projectile Settings { get; private set; }

        private readonly DelayedCallback _inactiveCallback;
        private float _startLifetime;
        private float _endLifetime;
        protected float _inactiveLifetime = 0f;

        private readonly PhysicsInfo _fixedInfo = new();
        private readonly PhysicsInfo _deltaInfo = new();

        public Vector3 Position => _deltaInfo.Position;
        public Vector3 Dir => _deltaInfo.Dir;
        public Vector3 Velocity => _deltaInfo.Velocity;

        private float _lastUpdatePhysicsTime;
        private float _lerpProgress;
        private float _lerpTime;
        private Vector3 _positionVisualDiff;
        protected Vector3 _positionVisual;
        protected Vector3 _dirVisual;

        protected static Quaternion s_tempRot;
        private const int MaxCollisionCheck = 3;
        private const float MaxPhysicsInterval = 0.05f;
        private const float PhysicsIntervalLerpDelay = 1f;

        public bool IsLocal { get; private set; }
        public ushort SyncID { get; private set; }
        public ushort PlayerIndex { get; private set; }

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<EWCProjectileComponentBase>();
        }

        protected virtual void Awake()
        {
            enabled = false;
        }

        [HideFromIl2Cpp]
        public virtual void Init(ushort playerIndex, ushort ID, Projectile projBase, bool isLocal, Vector3 position, Vector3 dir, HitData? hitData = null, IntPtr ignoreEnt = default)
        {
            if (enabled) return;

            _inactiveCallback.Cancel();

            SyncID = ID;
            PlayerIndex = playerIndex;
            Settings = projBase;

            gameObject.transform.localScale = Vector3.one * projBase.ModelScale;
            s_tempRot.SetLookRotation(dir);
            gameObject.transform.SetPositionAndRotation(position, s_tempRot);
            gameObject.active = true;
            enabled = true;

            _startLifetime = Time.time;
            _endLifetime = _startLifetime + projBase.Lifetime;
            _lastUpdatePhysicsTime = _startLifetime;
            _lerpProgress = 1f;

            _fixedInfo.Set(projBase, position, dir);

            IsLocal = isLocal;

            ProjectileAPI.FireProjectileSpawnedCallback(this);
            Hitbox.Init(projBase, position, dir, hitData, ignoreEnt, out var bounceHit);
            if (bounceHit != null)
                _fixedInfo.BaseDir = Vector3.Reflect(_fixedInfo.BaseDir, bounceHit.Value.normal);

            Homing.Init(projBase, position, _fixedInfo.BaseDir);
            _deltaInfo.Copy(_fixedInfo);
        }

        public virtual void SetVisualPosition(Vector3 positionVisual, float lerpDist)
        {
            if (_deltaInfo.BaseVelocity.sqrMagnitude == 0) return;

            _lerpProgress = 0f;
            _lerpTime = lerpDist / _deltaInfo.BaseVelocity.magnitude;
            _positionVisual = positionVisual;
            _positionVisualDiff = _positionVisual - _deltaInfo.Position;
            _dirVisual = _deltaInfo.Position + _deltaInfo.BaseVelocity * _lerpTime - _positionVisual;
            s_tempRot.SetLookRotation(_dirVisual);
            gameObject.transform.SetPositionAndRotation(_positionVisual, s_tempRot);
        }

        public void SetPosition(Vector3 pos, Vector3 dir)
        {
            _deltaInfo.Position = pos;
            _deltaInfo.BaseDir = dir;
        }

        private void LerpVisualOffset()
        {
            if (_lerpProgress == 1f)
            {
                _positionVisual = _deltaInfo.Position;
                _dirVisual = Dir;
                s_tempRot.SetLookRotation(_dirVisual);
                return;
            }

            _lerpProgress = Math.Min(1f, _lerpProgress + Time.deltaTime / _lerpTime);
            float invLerpSqr = 1f - (1f - _lerpProgress) * (1f - _lerpProgress);
            Vector3 lastPos = _positionVisual;
            _positionVisual = _deltaInfo.Position + Vector3.Lerp(_positionVisualDiff, Vector3.zero, invLerpSqr);
            _dirVisual = (_positionVisual - lastPos).normalized;
            s_tempRot.SetLookRotation(_dirVisual);
        }

        private bool UpdatePhysics()
        {
            var time = Time.time;
            float interval;
            if (time > _startLifetime + PhysicsIntervalLerpDelay)
                interval = MaxPhysicsInterval;
            else
                interval = (time - _startLifetime) / PhysicsIntervalLerpDelay * MaxPhysicsInterval;

            if (!IsLocal || _lastUpdatePhysicsTime + interval >= time) return false;

            float delta = time - _lastUpdatePhysicsTime;
            Vector3 dir = _fixedInfo.Dir;
            Homing.Update(_fixedInfo.Position, delta, ref dir);
            if (dir != _fixedInfo.Dir)
                _fixedInfo.BaseDir = dir;

            Vector3 deltaMove = GetDeltaMove(_fixedInfo, delta);
            Vector3 collisionVel = deltaMove.sqrMagnitude > EWCProjectileHitbox.MinCollisionSqrDist ? deltaMove : Dir * EWCProjectileHitbox.MinCollisionDist;

            bool needSync = false;
            for (int i = 0; i < MaxCollisionCheck && Hitbox.Update(_fixedInfo.Position, collisionVel, out var bounce); i++)
            {
                float angleFactor = Math.Abs(Vector3.Dot(Dir, bounce.normal));
                _fixedInfo.BaseSpeed *= (1f - Settings.RicochetSpeedAngleFactor * (1f - angleFactor)).Lerp(1f, Settings.RicochetSpeedMod);
                float remainingDist = collisionVel.magnitude - (bounce.point - _fixedInfo.Position).magnitude;
                _fixedInfo.BaseDir = Vector3.Reflect(Dir, bounce.normal);
                deltaMove = Vector3.Reflect(collisionVel, bounce.normal).normalized * remainingDist;
                collisionVel = remainingDist > EWCProjectileHitbox.MinRicochetDist ? deltaMove : Dir * EWCProjectileHitbox.MinRicochetDist;
                _fixedInfo.Position = bounce.point;
                needSync = true;
            }

            if (!enabled) return true; // Died by hitbox

            _fixedInfo.Position += deltaMove;
            _deltaInfo.Copy(_fixedInfo);
            _lastUpdatePhysicsTime = time;

            if (needSync)
                EWCProjectileManager.DoProjectileBounce(PlayerIndex, SyncID, Position, Dir);
            return true;
        }

        protected virtual void Update()
        {
            if (Settings == null || (IsLocal && Settings.CWC.Weapon == null)) // JFS - if weapon is destroyed, this could possibly happen?
            {
                Die();
                return;
            }

            bool didUpdatePhysics = UpdatePhysics();
            if (!enabled) return;

            if (Time.time > _endLifetime)
            {
                Die();
                return;
            }

            if (!didUpdatePhysics)
            {
                float delta = Time.deltaTime;
                Vector3 dir = _deltaInfo.Dir;
                Homing.UpdateDir(_deltaInfo.Position, delta, ref dir);
                if (dir != _deltaInfo.Dir)
                    _deltaInfo.BaseDir = dir;

                var deltaMove = GetDeltaMove(_deltaInfo, delta);
                _deltaInfo.Position += deltaMove;
            }
            LerpVisualOffset();
        }

        [HideFromIl2Cpp]
        private Vector3 GetDeltaMove(PhysicsInfo info, float delta)
        {
            Vector3 deltaMove;
            // Need to calculate how much distance was covered including the acceleration
            if (Settings!.AccelScale != 1f && info.AccelProgress != 1f)
            {
                float deltaMod;
                // Get the progress we should have by the end of the delta
                float trgtProgress = Math.Min(info.AccelProgress + delta / Settings.AccelTime, 1f);

                // Integrate the accel time, calculating the effective delta needed (with base speed) to move as much as it should
                float newExpo = Settings.AccelExponent + 1;
                float progressMod = (float)(Settings.AccelTime * (Settings.AccelScale - 1.0) * (Math.Pow(trgtProgress, newExpo) - Math.Pow(info.AccelProgress, newExpo)) / newExpo);
                if (trgtProgress < 1f) // Didn't cap speed, can directly add modifier
                    deltaMod = delta + progressMod;
                else // Capped speed, interpolate between modifier during acceleration and modifer after acceleration
                {
                    float timeToAccel = (1f - info.AccelProgress) * Settings.AccelTime;
                    deltaMod = progressMod + timeToAccel + Settings.AccelScale * (delta - timeToAccel);
                }
                info.AccelProgress = trgtProgress;
                deltaMove = info.BaseDir * info.BaseSpeed * deltaMod;
            }
            else
                deltaMove = info.BaseVelocity * delta;
            deltaMove.y -= 0.5f * Settings.Gravity * delta * delta + info.GravityVel * delta;

            info.GravityVel += Settings.Gravity * delta;
            info.BaseVelocity = info.BaseDir * info.Speed;
            info.Velocity = info.BaseVelocity;
            info.Velocity.y -= info.GravityVel;
            info.Dir = info.Velocity.normalized;
            return deltaMove;
        }

        public virtual void Die()
        {
            if (!enabled) return;

            gameObject.transform.localScale = Vector3.zero;
            enabled = false;
            _inactiveCallback.Start();
            EWCProjectileManager.DoProjectileDestroy(PlayerIndex, SyncID, IsLocal);
            Hitbox.Die();
            Homing.Die();
            ProjectileAPI.FireProjectileDestroyedCallback(this);
        }

        protected virtual void Cleanup()
        {
            gameObject.active = false;
        }

        class PhysicsInfo
        {
            public Projectile Settings = null!;

            private Vector3 _baseDir;
            public Vector3 BaseDir
            {
                get => _baseDir;
                set
                {
                    _baseDir = value;
                    BaseVelocity = _baseDir * Speed;
                    GravityVel = 0;
                    Velocity = BaseVelocity;
                    Dir = Velocity.normalized;
                }
            }

            public float BaseSpeed;
            public float Speed => Settings.AccelScale == 1f ? BaseSpeed : BaseSpeed * Math.Pow(AccelProgress, Settings.AccelExponent).Lerp(1f, Settings.AccelScale);
            public Vector3 BaseVelocity;

            private Vector3 _dir;
            public Vector3 Dir
            {
                get => _dir;
                set { _dir = value == Vector3.zero ? _baseDir : value; }
            }
            public Vector3 Position;
            public Vector3 Velocity;
            public float AccelProgress;
            public float GravityVel;

            public void Set(Projectile settings, Vector3 position, Vector3 dir)
            {
                Settings = settings;

                AccelProgress = 0f;
                Position = position;
                BaseSpeed = settings.MaxSpeed > settings.MinSpeed ? Random.NextSingle().Lerp(settings.MinSpeed, settings.MaxSpeed) : settings.MinSpeed;
                BaseDir = dir;
            }

            public void Copy(PhysicsInfo info)
            {
                Settings = info.Settings;

                AccelProgress = info.AccelProgress;

                Position = info.Position;
                _dir = info._dir;
                Velocity = info.Velocity;
                GravityVel = info.GravityVel;
                BaseSpeed = info.BaseSpeed;
                BaseVelocity = info.BaseVelocity;
                _baseDir = info._baseDir;
            }
        }
    }
}

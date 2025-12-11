using EWC.API;
using EWC.Attributes;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using EWC.Utils.Extensions;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
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
        [HideFromIl2Cpp]
        public ContextController ContextController { get; private set; }
        private static ContextController? s_currentController;
        private static float s_lastControllerTime = 0f;
        private static ushort s_lastControllerIndex = 0;

        private readonly DelayedCallback _inactiveCallback;
        private float _startLifetime;
        private float _endLifetime;
        private bool _playingEndSound = false;
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

        private float _aliveTriggerTime;

        protected static Quaternion s_tempRot;
        private const int MaxCollisionCheck = 3;
        private const float MaxPhysicsInterval = 0.05f;
        private const float PhysicsIntervalDelayUntilMax = 1f;

        public bool IsManaged { get; private set; }
        public ushort SyncID { get; private set; }
        public ushort ShotIndex { get; private set; }
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
        public virtual void Init(ushort playerIndex, ushort shotIndex, ushort ID, Projectile projBase, bool isManaged, Vector3 position, Vector3 dir, HitData? hitData = null, IntPtr ignoreEnt = default)
        {
            if (enabled) return;

            _inactiveCallback.Cancel();
            _playingEndSound = false;

            SyncID = ID;
            PlayerIndex = playerIndex;
            ShotIndex = shotIndex;
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

            _fixedInfo.Set(projBase, position, dir, shotIndex);
            _deltaInfo.Set(projBase, position, dir, shotIndex);

            _aliveTriggerTime = _startLifetime + Settings.AliveTriggerDelay;
            IsManaged = isManaged;

            ProjectileAPI.FireProjectileSpawnedCallback(this);
            if (Settings.FlyingSoundID != 0)
                SoundPlayer.Post(Settings.FlyingSoundID, Position);

            if (isManaged)
            {
                var cwc = projBase.CWC;
                if (cwc.HasTempProperties())
                {
                    // Properties can never change in the same frame, so we can batch shotguns.
                    if (s_lastControllerTime != Clock.Time || playerIndex != s_lastControllerIndex)
                    {
                        s_currentController = new(cwc.GetContextController());
                        s_lastControllerTime = Clock.Time;
                        s_lastControllerIndex = playerIndex;
                    }
                    ContextController = s_currentController!;
                }
                else
                {
                    ContextController = cwc.GetContextController();
                }
            }
            
            Hitbox.Init(projBase, position, dir, hitData, ignoreEnt, out var bounceHit);
            if (bounceHit != null)
            {
                _fixedInfo.BaseDir = Vector3.Reflect(_fixedInfo.BaseDir, bounceHit.Value.normal);
                _deltaInfo.Copy(_fixedInfo);
            }

            Homing.Init(projBase, position, _fixedInfo.BaseDir);
        }

        protected virtual CellSoundPlayer SoundPlayer => throw new NotImplementedException("Base projectile component SoundPlayer called - child class should override!");

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
            SoundPlayer.UpdatePosition(_positionVisual);
        }

        public void ReceivePosition(Vector3 pos, Vector3 dir)
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
            if (!IsManaged) return false;

            var time = Time.time;
            float interval;
            if (time > _startLifetime + PhysicsIntervalDelayUntilMax)
                interval = MaxPhysicsInterval;
            else
                interval = (time - _startLifetime) / PhysicsIntervalDelayUntilMax * MaxPhysicsInterval;

            if (_lastUpdatePhysicsTime + interval >= time) return false;

            float delta = time - _lastUpdatePhysicsTime;
            Vector3 dir = _fixedInfo.Dir;
            Homing.Update(_fixedInfo.Position, delta, ref dir);
            foreach (var dirChange in _fixedInfo.DirChanges)
                dirChange.Update(ref dir);
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
            if (Settings == null || (IsManaged && Settings.CWC == null)) // JFS - if weapon is destroyed, this could possibly happen?
            {
                Die();
                return;
            }

            bool didUpdatePhysics = UpdatePhysics();
            if (!enabled) return;

            var time = Time.time;
            if (time > _endLifetime)
            {
                Die();
                return;
            }

            if (!didUpdatePhysics)
            {
                float delta = Time.deltaTime;
                Vector3 dir = _deltaInfo.Dir;
                Homing.UpdateDir(_deltaInfo.Position, delta, ref dir);
                foreach (var dirChange in _deltaInfo.DirChanges)
                    dirChange.Update(ref dir);
                if (dir != _deltaInfo.Dir)
                    _deltaInfo.BaseDir = dir;

                var deltaMove = GetDeltaMove(_deltaInfo, delta);
                _deltaInfo.Position += deltaMove;
            }

            if (IsManaged && _aliveTriggerTime <= time)
            {
                _aliveTriggerTime = time + Settings.AliveTriggerInterval;
                Settings.EventHelper.Invoke(
                    ContextController,
                    new WeaponReferencePosContext(
                        Settings.ID,
                        (uint) Projectile.Callback.Alive,
                        Position,
                        Dir,
                        Vector3.zero,
                        Hitbox.GetFalloff(),
                        Hitbox.HitData.shotInfo
                    )
                );
            }

            LerpVisualOffset();
            SoundPlayer.UpdatePosition(_positionVisual);
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

            Settings.EventHelper.Invoke(
                ContextController,
                new WeaponReferencePosContext(
                    Settings.ID,
                    (uint)Projectile.Callback.Destroyed,
                    Position,
                    Dir,
                    -Dir,
                    Hitbox.GetFalloff(),
                    Hitbox.HitData.shotInfo
                )
            );
            gameObject.transform.localScale = Vector3.zero;
            enabled = false;
            _inactiveCallback.Start();
            if (Settings.StopFlyingSoundOnDestroy)
                SoundPlayer.Stop();
            if (Settings.DestroyedSoundID != 0)
            {
                _playingEndSound = true;
                SoundPlayer.PostWithDoneCallback(Settings.DestroyedSoundID, Position, OnEndSoundDone);
            }
            EWCProjectileManager.DoProjectileDestroy(PlayerIndex, SyncID, IsManaged);
            Hitbox.Die();
            Homing.Die();
            ProjectileAPI.FireProjectileDestroyedCallback(this);
        }

        private void OnEndSoundDone()
        {
            _playingEndSound = false;
            Cleanup();
        }

        protected virtual void Cleanup()
        {
            if (_inactiveCallback.Active || _playingEndSound) return;

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
            public readonly List<ProjectileDirChange.State> DirChanges = new();

            public void Set(Projectile settings, Vector3 position, Vector3 dir, ushort shotIndex)
            {
                Settings = settings;

                DirChanges.Clear();
                DirChanges.EnsureCapacity(settings.DirChanges.Count);
                foreach (var moveChange in settings.DirChanges)
                    DirChanges.Add(moveChange.CreateState(dir, shotIndex));
                AccelProgress = 0f;
                Position = position;
                BaseSpeed = settings.MaxSpeed > settings.MinSpeed ? Random.NextSingle().Lerp(settings.MinSpeed, settings.MaxSpeed) : settings.MinSpeed;
                BaseDir = dir;
            }

            public void Copy(PhysicsInfo info)
            {
                Settings = info.Settings;

                for (int i = 0; i < DirChanges.Count; i++)
                    DirChanges[i].Copy(info.DirChanges[i]);
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

﻿using EWC.API;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.Utils;
using EWC.Utils.Extensions;
using Il2CppInterop.Runtime.Attributes;
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
        private float _endLifetime;
        protected float _inactiveLifetime = 0f;
        private Vector3 _baseDir;
        private Vector3 BaseDir
        {
            get => _baseDir;
            set 
            { 
                _baseDir = value;
                _baseVelocity = _baseDir * Speed;
                _gravityVel = 0;
                Velocity = _baseVelocity;
            }
        }

        private float _baseSpeed;
        private float Speed => Settings.AccelScale == 1f ? _baseSpeed : _baseSpeed * Math.Pow(_accelProgress, Settings.AccelExponent).Lerp(1f, Settings.AccelScale);
        private Vector3 _baseVelocity;

        private Vector3 _dir;
        public Vector3 Dir
        {
            get => _dir;
            private set { _dir = value == Vector3.zero ? _baseDir : value; }
        }
        private Vector3 _velocity;
        public Vector3 Velocity
        {
            get => _velocity;
            private set{ _velocity = value; Dir = _velocity.normalized; }
        }
        private float _accelProgress;

        private Vector3 _position;
        public Vector3 Position => _position;
        protected float _gravityVel;

        private float _lerpProgress;
        private float _lerpTime;
        private Vector3 _positionVisualDiff;
        protected Vector3 _positionVisual;
        protected Vector3 _dirVisual;

        protected static Quaternion s_tempRot;
        private const int MaxCollisionCheck = 3;

        public bool IsLocal { get; private set; }
        public ushort SyncID { get; private set; }
        public ushort PlayerIndex { get; private set; }

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

            _lerpProgress = 1f;
            _accelProgress = 0f;
            _endLifetime = Time.time + projBase.Lifetime;

            _position = position;
            _baseSpeed = projBase.MaxSpeed > projBase.MinSpeed ? Random.NextSingle().Lerp(projBase.MinSpeed, projBase.MaxSpeed) : projBase.MinSpeed;
            BaseDir = dir;
            IsLocal = isLocal;

            ProjectileAPI.FireProjectileSpawnedCallback(this);
            Hitbox.Init(projBase, hitData, ignoreEnt, out var bounceHit);
            if (bounceHit != null)
                BaseDir = Vector3.Reflect(BaseDir, bounceHit.Value.normal);

            Homing.Init(projBase, position, BaseDir);
        }

        public virtual void SetVisualPosition(Vector3 positionVisual, float lerpDist)
        {
            if (_baseVelocity.sqrMagnitude == 0) return;

            _lerpProgress = 0f;
            _lerpTime = lerpDist / _baseVelocity.magnitude;
            _positionVisual = positionVisual;
            _positionVisualDiff = _positionVisual - _position;
            _dirVisual = _position + _baseVelocity * _lerpTime - _positionVisual;
            s_tempRot.SetLookRotation(_dirVisual);
            gameObject.transform.SetPositionAndRotation(_positionVisual, s_tempRot);
        }

        public void SetPosition(Vector3 pos, Vector3 dir)
        {
            _position = pos;
            BaseDir = dir;
        }

        private void LerpVisualOffset()
        {
            if (_lerpProgress == 1f)
            {
                _positionVisual = _position;
                _dirVisual = Dir;
                s_tempRot.SetLookRotation(_dirVisual);
                return;
            }

            _lerpProgress = Math.Min(1f, _lerpProgress + Time.deltaTime / _lerpTime);
            float invLerpSqr = 1f - (1f - _lerpProgress) * (1f - _lerpProgress);
            Vector3 lastPos = _positionVisual;
            _positionVisual = _position + Vector3.Lerp(_positionVisualDiff, Vector3.zero, invLerpSqr);
            _dirVisual = (_positionVisual - lastPos).normalized;
            s_tempRot.SetLookRotation(_dirVisual);
        }

        protected virtual void Update()
        {
            if (Settings == null || (IsLocal && Settings.CWC.Weapon == null)) // JFS - if weapon is destroyed, this could possibly happen?
            {
                Die();
                return;
            }

            Vector3 dir = Dir;
            Homing.Update(_position, ref dir);
            if (dir != Dir)
                BaseDir = dir;

            Vector3 deltaMove = UpdateVelocity();
            Vector3 collisionVel = deltaMove.sqrMagnitude > EWCProjectileHitbox.MinCollisionSqrDist ? deltaMove : Dir * EWCProjectileHitbox.MinCollisionDist;
            
            bool syncPos = false;
            for(int i = 0; i < MaxCollisionCheck && Hitbox.Update(_position, collisionVel, out var bounce); i++)
            {
                float angleFactor = Math.Abs(Vector3.Dot(Dir, bounce.normal));
                _baseSpeed *= (1f - Settings.RicochetSpeedAngleFactor * angleFactor).Lerp(Settings.RicochetSpeedMod, 1f);
                float remainingDist = collisionVel.magnitude - (bounce.point - _position).magnitude;
                BaseDir = Vector3.Reflect(Dir, bounce.normal);
                deltaMove = Vector3.Reflect(collisionVel, bounce.normal).normalized * remainingDist;
                collisionVel = remainingDist > EWCProjectileHitbox.MinRicochetDist ? deltaMove : Dir * EWCProjectileHitbox.MinRicochetDist;
                _position = bounce.point;
                syncPos = true;
            }

            if (!enabled) return; // Died by hitbox

            if (Time.time > _endLifetime)
            {
                Die();
                return;
            }

            _position += deltaMove;
            if (syncPos)
                EWCProjectileManager.DoProjectileBounce(PlayerIndex, SyncID, _position, Dir);

            LerpVisualOffset();
        }

        private Vector3 UpdateVelocity()
        {
            float delta = Time.deltaTime;
            Vector3 deltaMove;
            // Need to calculate how much distance was covered including the acceleration
            if (Settings!.AccelScale != 1f && _accelProgress != 1f)
            {
                float deltaMod;
                // Get the progress we should have by the end of the delta
                float trgtProgress = Math.Min(_accelProgress + delta / Settings.AccelTime, 1f);

                // Integrate the accel time, calculating the effective delta needed (with base speed) to move as much as it should
                float newExpo = Settings.AccelExponent + 1;
                float progressMod = (float)(Settings.AccelTime * (Settings.AccelScale - 1.0) * (Math.Pow(trgtProgress, newExpo) - Math.Pow(_accelProgress, newExpo)) / newExpo);
                if (trgtProgress < 1f) // Didn't cap speed, can directly add modifier
                    deltaMod = delta + progressMod;
                else // Capped speed, interpolate between modifier during acceleration and modifer after acceleration
                {
                    float timeToAccel = (1f - _accelProgress) * Settings.AccelTime;
                    deltaMod = progressMod + timeToAccel + Settings.AccelScale * (delta - timeToAccel);
                }
                _accelProgress = trgtProgress;
                deltaMove = _baseDir * _baseSpeed * deltaMod;
            }
            else
                deltaMove = _baseVelocity * delta;
            deltaMove.y -= 0.5f * Settings.Gravity * delta * delta + _gravityVel * delta;

            _gravityVel += Settings.Gravity * delta;
            _baseVelocity = _baseDir * Speed;
            _velocity = _baseVelocity;
            _velocity.y -= _gravityVel;
            Dir = _velocity.normalized;
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
    }
}

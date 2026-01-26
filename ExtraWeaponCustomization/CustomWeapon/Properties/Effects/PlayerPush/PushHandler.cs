using EWC.Attributes;
using Il2CppInterop.Runtime.Injection;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.PlayerPush
{
    public sealed class PushHandler : MonoBehaviour
    {
        public static PushHandler Current { get; private set; } = null!;

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<PushHandler>();
        }

        [InvokeOnCleanup]
        private static void Cleanup()
        {
            if (Current != null)
            {
                Current._instances.Clear();
                Current._totalForce = Vector3.zero;
                Current.enabled = false;
            }
        }

        private PlayerAgent _player = null!;
        private CharacterController _controller = null!;
        private readonly Dictionary<ushort, PushInstance> _instances = new();
        private readonly List<ushort> _finished = new();
        private Vector3 _totalForce;

        public PushHandler(IntPtr ptr) : base(ptr) { }

        public void Awake()
        {
            Current = this;
            enabled = false;
            _player = GetComponent<PlayerAgent>();
            _controller = _player.PlayerCharacterController.m_characterController;
            _totalForce = Vector3.zero;
        }

        public static void AddInstance(Vector3 force, Push settings)
        {
            if (Current == null)
            {
                if (!PlayerManager.HasLocalPlayerAgent()) return;

                PlayerManager.GetLocalPlayerAgent().gameObject.AddComponent<PushHandler>();
            }

            Current!.AddInstance_Internal(force, settings);
        }

        private void AddInstance_Internal(Vector3 force, Push settings)
        {
            if (!_instances.TryGetValue(settings.SyncPropertyID, out var instance))
                _instances.Add(settings.SyncPropertyID, instance = new PushInstance(settings));
            _totalForce += instance.AddForce(force);
            enabled = true;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if ((_controller.collisionFlags & CollisionFlags.Sides) == 0) return;

            var normal = hit.normal;
            normal.y = 0;
            if (normal.sqrMagnitude < 0.01f) return;

            normal.Normalize();
            _totalForce = Vector3.zero;
            foreach (var instance in _instances.Values)
                _totalForce += instance.OnWallCollision(normal);
        }

        private void FixedUpdate()
        {
            _totalForce = Vector3.zero;
            foreach ((var id, var instance) in _instances)
            {
                if (instance.UpdateCheckDone())
                    _finished.Add(id);
                else
                    _totalForce += instance.Force;
            }

            foreach (var id in _finished)
                _instances.Remove(id);
            _finished.Clear();

            if (_instances.Count == 0)
                enabled = false;
        }

        class PushInstance
        {
            private Vector3 _force;
            public Vector3 Force => _force;
            private float _frictionStartTime;
            private readonly Push _settings;
            private readonly PlayerLocomotion _locomotion;
            private readonly PlayerCharacterController _controller;

            public PushInstance(Push settings)
            {
                _locomotion = Current._player.Locomotion;
                _controller = Current._player.PlayerCharacterController;
                _settings = settings;

                _frictionStartTime = Clock.Time + settings.FrictionDelay;
                _force = Vector3.zero;
            }

            public Vector3 AddForce(Vector3 force)
            {
                _frictionStartTime = Math.Max(_frictionStartTime, Clock.Time + _settings.RepeatFrictionDelay);
                ApplyVerticalForce(force.y);
                force.y = 0;

                Vector3 horizontal;
                if (_settings.HorizontalCap.IncludeVelocity)
                {
                    horizontal = Current._totalForce + _locomotion.HorizontalVelocity + _locomotion.VerticalVelocity;
                    horizontal.y = 0;
                }
                else
                {
                    horizontal = _force;
                }
                
                Vector3 oldForce = _force;
                _force = _settings.HorizontalCap.AddAndCap(horizontal, force);
                return _force - oldForce;
            }

            public Vector3 OnWallCollision(Vector3 normal)
            {
                float intoWall = Vector3.Dot(_force, normal);
                if (intoWall < 0)
                    _force -= normal * intoWall;
                return _force;
            }

            public bool UpdateCheckDone()
            {
                _controller.Move(_force * Clock.FixedDelta);
                if (_frictionStartTime > Clock.Time) return false;

                float friction;
                float constFriction;
                switch (_locomotion.m_currentStateEnum)
                {
                    case PlayerLocomotion.PLOC_State.Jump:
                    case PlayerLocomotion.PLOC_State.Fall:
                        friction = _settings.AirFrictionScale;
                        constFriction = _settings.AirConstantFriction;
                        break;
                    default:
                        friction = _settings.FrictionScale;
                        constFriction = _settings.ConstantFriction;
                        break;
                }

                friction *= Clock.FixedDelta;
                constFriction *= Clock.FixedDelta;
                _force *= 1 - friction;
                _force -= _force.normalized * constFriction;
                return _force.sqrMagnitude <= constFriction * constFriction + 0.01f;
            }

            private void ApplyVerticalForce(float force)
            {
                if (force == 0) return;

                switch (_locomotion.m_currentStateEnum)
                {
                    case PlayerLocomotion.PLOC_State.Jump:
                    case PlayerLocomotion.PLOC_State.Fall:
                        break;
                    default:
                        if (force < 0)
                            return;
                        _locomotion.ChangeState(PlayerLocomotion.PLOC_State.Jump, true);
                        break;
                }
                var currVertical = _locomotion.VerticalVelocity;
                currVertical.y = _settings.VerticalCap.AddAndCap(currVertical.y, force);
                _locomotion.VerticalVelocity = currVertical;
            }
        }
    }
}

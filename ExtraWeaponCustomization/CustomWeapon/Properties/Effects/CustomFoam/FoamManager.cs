using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.ObjectWrappers;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam
{
    public sealed class FoamManager
    {
        private readonly static FoamManager Current = new();
        private readonly Dictionary<BaseDamageableWrapper<Dam_EnemyDamageBase>, FoamTimeHandler> _foamTimes = new();
        private readonly Dictionary<ObjectWrapper<GlueGunProjectile>, Foam> _customFoams = new();
        private readonly PriorityQueue<ObjectWrapper<GlueGunProjectile>, float> _bubbleExpireTimes = new();
        private readonly LinkedList<FoamTimeHandler> _unfoamHandlers = new();
        private Coroutine? _updateRoutine;
        private Coroutine? _expireRoutine;

        private static BaseDamageableWrapper<Dam_EnemyDamageBase> TempWrapper => BaseDamageableWrapper<Dam_EnemyDamageBase>.SharedInstance;
        private static ObjectWrapper<GlueGunProjectile> ProjTempWrapper => ObjectWrapper<GlueGunProjectile>.SharedInstance;

        public static void AddFoamBubble(GlueGunProjectile proj, Foam property)
        {
            Current.CleanupBubbles();

            var wrapper = new ObjectWrapper<GlueGunProjectile>(proj);
            Current._customFoams[wrapper] = property;
            if (property.BubbleLifetime > 0)
                Current.AddExpiringBubble(wrapper, property.BubbleLifetime);
        }
        public static Foam? GetProjProperty(GlueGunProjectile? proj) => proj != null ? Current._customFoams.GetValueOrDefault(ProjTempWrapper.Set(proj)) : null;

        public static void AddFoam(Dam_EnemyDamageBase damBase, float amount, Foam? property = null)
        {
            if (!Current._foamTimes.TryGetValue(TempWrapper.Set(damBase), out var handler))
            {
                Current.CleanupFoam();
                handler = Current._foamTimes[new BaseDamageableWrapper<Dam_EnemyDamageBase>(TempWrapper)] = new FoamTimeHandler(damBase);
            }

            if (handler.AddFoam(amount, property))
                Current.AddUnfoamHandler(handler);
        }

        internal static void ReceiveSyncFoam(Dam_EnemyDamageBase damBase, float amountRel, float timeRel, bool unfoam)
        {
            if (!Current._foamTimes.TryGetValue(TempWrapper.Set(damBase), out var handler))
            {
                Current.CleanupFoam();
                handler = Current._foamTimes[new BaseDamageableWrapper<Dam_EnemyDamageBase>(TempWrapper)] = new FoamTimeHandler(damBase);
            }

            if (handler.ReceiveSyncFoam(amountRel, timeRel, unfoam))
                Current.AddUnfoamHandler(handler);
        }

        private void CleanupFoam()
        {
            // Remove dead enemies
            _foamTimes.Keys
                .Where(wrapper => !wrapper.Alive)
                .ToList()
                .ForEach(wrapper =>
                {
                    var handler = _foamTimes[wrapper];
                    _foamTimes.Remove(wrapper);
                    _unfoamHandlers.Remove(handler);
                });

            if (_unfoamHandlers.Count == 0f && _updateRoutine != null)
            {
                CoroutineManager.StopCoroutine(_updateRoutine);
                _updateRoutine = null;
            }
        }

        private void CleanupBubbles()
        {
            // Remove dead bubbles
            _customFoams.Keys
                .Where(wrapper => wrapper.Object == null)
                .ToList()
                .ForEach(wrapper =>
                {
                    _customFoams.Remove(wrapper);
                });

            if (_customFoams.Count == 0f && _expireRoutine != null)
            {
                CoroutineManager.StopCoroutine(_expireRoutine);
                _expireRoutine = null;
                _bubbleExpireTimes.Clear();
            }
        }

        private void AddUnfoamHandler(FoamTimeHandler handler)
        {
            _unfoamHandlers.AddLast(handler);
            _updateRoutine ??= CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        private void AddExpiringBubble(ObjectWrapper<GlueGunProjectile> wrapper, float lifetime)
        {
            if (!SNet.IsMaster) return;

            _bubbleExpireTimes.Enqueue(wrapper, Clock.Time + lifetime);
            _expireRoutine ??= CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        private IEnumerator Update()
        {
            while (_unfoamHandlers.Count > 0)
            {
                var next = _unfoamHandlers.First;
                for (var node = next; node != null; node = next)
                {
                    var handler = node.Value;
                    next = node.Next;
                    if (handler.DamBase == null || handler.DamBase.Health <= 0 || !handler.Update())
                        _unfoamHandlers.Remove(node);
                }

                yield return null;
            }
            _updateRoutine = null;
        }

        private IEnumerator UpdateExpiring()
        {
            while (_bubbleExpireTimes.Count > 0)
            {
                float time = Clock.Time;
                while (_bubbleExpireTimes.TryPeek(out var wrapper, out float endTime) && endTime < time)
                {
                    if (wrapper.Object != null)
                        ProjectileManager.WantToDestroyGlue()
                }

                yield return null;
            }
            _updateRoutine = null;
        }

        class FoamTimeHandler
        {
            private float _totalAmount = 0f;
            private float _totalTime = 0f;
            private bool _unfoam = false;
            private float _lastUpdateTime = 0f;
            private float UnfoamAmount => Math.Min((_totalTime / _foamTime + 0.1f) * _tolerance, _tolerance);

            public readonly Dam_EnemyDamageBase DamBase;
            private readonly float _tolerance;
            private readonly float _foamTime;

            public FoamTimeHandler(Dam_EnemyDamageBase damBase)
            {
                DamBase = damBase;
                var block = damBase.Owner.EnemyBalancingData;
                _tolerance = block.GlueTolerance;
                _foamTime = block.GlueFadeOutTime * 0.9f;
            }

            public bool Update()
            {
                if (!_unfoam) return false;

                float delta = Time.time - _lastUpdateTime;
                _totalTime = Math.Max(0f, _totalTime - delta);
                if (_totalTime == 0f)
                {
                    DamBase.m_attachedGlueVolume = 0f;
                    _totalAmount = 0f;
                    _unfoam = false;
                    SendSyncFoam();
                }
                else
                {
                    _totalAmount = UnfoamAmount;
                    DamBase.m_attachedGlueVolume = _totalAmount;
                }

                _lastUpdateTime = Time.time;
                return _unfoam;
            }

            public bool AddFoam(float amount, Foam? property = null)
            {
                if (DamBase.IsImortal)
                    return false;

                float time = property != null ? property.GetMaxFoamTime(_foamTime) : _foamTime;

                if (_unfoam && Update())
                {
                    if (_totalTime < time)
                        _totalTime = Math.Min(time, _totalTime + amount / _tolerance * time);
                    _totalAmount = UnfoamAmount;
                    SendSyncFoam();
                    return false;
                }

                if (_totalAmount + amount >= _tolerance)
                {
                    _totalTime += (_tolerance - _totalAmount) / _tolerance * time;
                    _totalAmount = _tolerance;
                    _unfoam = true;
                    if (SNet.IsMaster && !DamBase.IsStuckInGlue)
                        DamBase.Owner.Locomotion.StuckInGlue.ActivateState();
                    _lastUpdateTime = Time.time;
                }
                else
                {
                    _totalTime += amount / _tolerance * time;
                    _totalAmount += amount;
                }

                DamBase.m_attachedGlueVolume = _totalAmount;
                SendSyncFoam();
                return _unfoam;
            }

            internal bool ReceiveSyncFoam(float amountRel, float timeRel, bool unfoam)
            {
                bool addToHandlers = unfoam && !_unfoam;
                _totalAmount = amountRel * _tolerance;
                _totalTime = timeRel * _foamTime;
                _unfoam = unfoam;
                if (_unfoam)
                    DamBase.m_attachedGlueVolume = UnfoamAmount;
                else
                    DamBase.m_attachedGlueVolume = amountRel * _tolerance;
                return addToHandlers;
            }

            private void SendSyncFoam()
            {
                FoamActionManager.FoamSync(DamBase.Owner, _totalAmount / _tolerance, _totalTime / _foamTime, _unfoam);
            }
        }
    }
}

using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.ObjectWrappers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.DOT
{
    public sealed class DOTController
    {
        // Used to give a fast reference back to the wrapper used as key in the activeDots dictionary.
        // Need to access that specific wrapper so we can get the last DOT instance added to it.
        private readonly Dictionary<IntPtr, DOTDamageableWrapper> _ptrToWrapper = new();
        private readonly Dictionary<DOTDamageableWrapper, PriorityQueue<DOTInstance, DOTInstance>> _activeDots = new();
        private static readonly DOTComparer s_comparer = new();
        private Coroutine? _updateRoutine = null;
        private float _nextTickTime = float.MaxValue;

        public void AddDOT(ref DOTInstance newDot, IDamageable damageable)
        {
            IntPtr ptr = damageable.Pointer;

            // If the limb doesn't exist in activeDots, initialize a new Wrapper and add it
            if (!_ptrToWrapper.ContainsKey(ptr))
            {
                DOTDamageableWrapper wrapper = new(damageable, ptr);
                _ptrToWrapper[ptr] = wrapper;
                _activeDots[wrapper] = new(s_comparer);
                _activeDots[wrapper].Enqueue(newDot, newDot);
                wrapper.LastInstance = newDot;
            }
            else
            {
                // If the limb does exist, try and batch it with the last added DOT on the limb.
                // Mainly to improve shotgun shot performance, not reliable with more than 1 DOT but doesn't need to be.
                DOTDamageableWrapper key = _ptrToWrapper[ptr];

                if (key.LastInstance?.CanAddInstance(newDot.DotBase) == true)
                {
                    key.LastInstance.AddInstance(newDot);
                    newDot = key.LastInstance;
                }
                else
                {
                    _activeDots[key].Enqueue(newDot, newDot);
                    key.LastInstance = newDot;
                }
            }

            _updateRoutine ??= CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        private IEnumerator Update()
        {
            while (true)
            {
                _nextTickTime = float.MaxValue;
                Cleanup();

                int count = _activeDots.Count;
                foreach (var kv in _activeDots.ToList())
                {
                    PriorityQueue<DOTInstance, DOTInstance> queue = kv.Value;
                    IDamageable damageable = kv.Key.Object!;
                    // Keep getting next DOT and dealing damage until no more can deal damage
                    while (queue.Count > 0 && queue.Peek().CanTick)
                    {
                        DOTInstance dot = queue.Dequeue();
                        dot.DoDamage(damageable);

                        if (!dot.Expired)
                            queue.Enqueue(dot, dot);
                    }

                    if (queue.Count > 0)
                        _nextTickTime = Math.Min(_nextTickTime, queue.Peek().NextTickTime);
                }

                // Recalculate next tick time if new DOTs were added
                if (_activeDots.Count > count)
                    foreach (var queue in _activeDots.Values)
                        if (queue.Count > 0)
                            _nextTickTime = Math.Min(_nextTickTime, queue.Peek().NextTickTime);

                if (_nextTickTime == float.MaxValue) break;

                yield return new WaitForSeconds(_nextTickTime - Clock.Time);
            }

            _updateRoutine = null;
        }

        private void Cleanup()
        {
            // Remove dead enemies
            _activeDots.Keys
                .Where(wrapper => !wrapper.Alive)
                .ToList()
                .ForEach(wrapper =>
                {
                    _activeDots.Remove(wrapper);
                    _ptrToWrapper.Remove(wrapper.Pointer);
                });
        }

        public void Clear()
        {
            _activeDots.Clear();
            _ptrToWrapper.Clear();
            Utils.CoroutineUtil.Stop(ref _updateRoutine);
        }

        sealed class DOTDamageableWrapper : ObjectWrapper<IDamageable>
        {
            // Used for batching shotgun hits on same shot
            public DOTInstance? LastInstance { get; set; }
            // In some cases, a PlayerAgent becomes null but not their damageable and can crash later when getting DamageTargetPos.
            public bool Alive => _baseDamageable != null && _baseDamageable.GetHealthRel() > 0 && _hasAgent == (_agent != null);
            private readonly IDamageable _baseDamageable;
            private readonly bool _hasAgent;
            private readonly Agents.Agent? _agent;
            public DOTDamageableWrapper(IDamageable damageable, IntPtr ptr) : base(damageable, ptr)
            {
                _baseDamageable = damageable.GetBaseDamagable();
                _agent = damageable.GetBaseAgent();
                _hasAgent = _agent != null;
            }
        }

        sealed class DOTComparer : IComparer<DOTInstance>
        {
            // Want DOTs with sooner tick times to be at the head of the queue
            public int Compare(DOTInstance? x, DOTInstance? y)
            {
                if (x?.NextTickTime == y?.NextTickTime) return 0;
                if (x == null) return 1;
                if (y == null) return -1;
                return x.NextTickTime < y.NextTickTime ? -1 : 1;
            }
        }
    }
}

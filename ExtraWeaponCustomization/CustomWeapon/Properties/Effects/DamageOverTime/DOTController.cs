using Agents;
using ExtraWeaponCustomization.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class DOTController
    {
        private readonly Dictionary<int, DamageableWrapper> _idToWrapper = new();
        private readonly Dictionary<DamageableWrapper, PriorityQueue<DOTInstance, DOTInstance>> _enemyDots = new();
        private readonly DOTComparer comparer = new();
        private Coroutine? _updateRoutine = null;
        private float _nextTickTime = float.MaxValue;

        public DOTInstance? AddDOT(float totalDamage, IDamageable damageable, DamageOverTime dotBase)
        {
            if (dotBase.Owner == null) return null;

            int instanceID = damageable.TryCast<MonoBehaviour>()?.GetInstanceID() ?? 0;
            if (instanceID == 0) return null;

            // If the limb doesn't exist in enemyDots, initialize a new Wrapper and add it
            DOTInstance dot = new(totalDamage, dotBase.Owner, dotBase);
            if (!_idToWrapper.ContainsKey(instanceID))
            {
                DamageableWrapper wrapper = new(instanceID, damageable);
                _idToWrapper[instanceID] = wrapper;
                _enemyDots[wrapper] = new(comparer);
                _enemyDots[wrapper].Enqueue(dot, dot);
                wrapper.LastInstance = dot;
            }
            else
            {
                // If the limb does exist, try and batch it with the last added DOT on the limb.
                // Mainly to improve shotgun shot performance, not reliable with more than 1 DOT but doesn't need to be.
                DamageableWrapper key = _idToWrapper[instanceID];

                if (key.LastInstance?.CanAddInstance(dotBase) == true)
                {
                    dot = key.LastInstance;
                    key.LastInstance.AddInstance(totalDamage);
                }
                else
                {
                    _enemyDots[key].Enqueue(dot, dot);
                    key.LastInstance = dot;
                }
            }

            _updateRoutine ??= CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(Update()));
            return dot;
        }

        private IEnumerator Update()
        {
            while (true)
            {
                _nextTickTime = float.MaxValue;
                Cleanup();

                foreach (var kv in _enemyDots)
                {
                    PriorityQueue<DOTInstance, DOTInstance> queue = kv.Value;
                    IDamageable damageable = kv.Key.Damageable;
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

                if (_nextTickTime == float.MaxValue) break;

                yield return new WaitForSeconds(_nextTickTime - Clock.Time);
            }

            _updateRoutine = null;
        }

        private void Cleanup()
        {
            // Remove dead enemies
            _enemyDots.Keys
                .Where(wrapper => 
                        wrapper.Damageable == null
                     || wrapper.Damageable.GetBaseAgent() == null
                     || wrapper.Damageable.GetBaseAgent().Alive != true
                     || wrapper.Damageable.GetBaseAgent().m_isBeingDestroyed == true)
                .ToList()
                .ForEach(wrapper => {
                    _enemyDots.Remove(wrapper);
                    _idToWrapper.Remove(wrapper.ID);
                });
        }

        sealed class DamageableWrapper
        {
            public int ID { get; }
            public IDamageable Damageable { get; }
            public DOTInstance? LastInstance { get; set; }

            public DamageableWrapper(int iD, IDamageable damageable)
            {
                ID = iD;
                Damageable = damageable;
            }

            public override int GetHashCode()
            {
                return ID;
            }

            public override bool Equals(object? obj)
            {
                return obj is DamageableWrapper wrapper && wrapper.ID == ID;
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

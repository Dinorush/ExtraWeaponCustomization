using EWC.Attributes;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Hit.DOT.DOTGlowFX;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.DOT
{
    public static class DOTGlowPooling
    {
        private static readonly Queue<DOTGlowHandler> _pool = new();
        private static readonly Dictionary<DamageOverTime, Dictionary<BaseDamageableWrapper, DOTGlowHandler>> _activeHandlers = new();
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        [InvokeOnAssetLoad]
        private static void Initialize()
        {
            for (int i = 0; i < 30; i++)
            {
                _pool.Enqueue(CreatePoolObject());
            }
        }

        [InvokeOnCleanup(onCheckpoint: true)]
        private static void Reset()
        {
            _activeHandlers.Clear();
        }

        private static DOTGlowHandler CreatePoolObject()
        {
            var newObject = new GameObject();
            Object.DontDestroyOnLoad(newObject);

            var effectHandler = newObject.AddComponent<DOTGlowHandler>();
            newObject.AddComponent<EffectLight>();
            newObject.SetActive(false);
            return effectHandler;
        }

        public static void TryDoEffect(DamageOverTime data, IDamageable damBase, Transform target, float mod)
        {
            TempWrapper.Set(damBase);
            if (_activeHandlers.TryGetValue(data, out var damDict))
            {
                if (damDict.TryGetValue(TempWrapper, out var damHandler))
                {
                    damHandler.AddInstance(mod);
                    return;
                }
            }
            else
                _activeHandlers.Add(data, new());

            if (_pool.TryDequeue(out var handler))
            {
                var wrapper = new BaseDamageableWrapper(TempWrapper);
                _activeHandlers[data].Add(wrapper, handler);

                handler.EffectDoneOnce = () =>
                {
                    handler.gameObject.SetActive(false);
                    _pool.Enqueue(handler);
                    if (_activeHandlers.TryGetValue(data, out var damDict))
                        damDict.Remove(wrapper);
                };
                handler.gameObject.SetActive(true);
                handler.DoEffect(data, damBase, target, mod);
            }
        }

        public static void TryEndEffect(DamageOverTime data)
        {
            if (_activeHandlers.TryGetValue(data, out var damDict))
            {
                foreach (var handler in damDict.Values)
                    handler.ForceEnd();
            }
        }
    }
}
using Agents;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EWC.CustomWeapon.HitTracker
{
    internal class HitTracker
    {
        private readonly Dictionary<ObjectWrapper<Agent>, Dictionary<ObjectWrapper<CustomWeaponComponent>, (WeaponHitDamageableContext context, float time)>> _lastHits = new();
        private readonly Dictionary<ObjectWrapper<Agent>, bool> _shownHits = new();

        public void RegisterHit(ObjectWrapper<CustomWeaponComponent> cwc, WeaponHitDamageableContext hitContext, ObjectWrapper<Agent> enemy)
        {
            if (_lastHits.TryGetValue(enemy, out var hitInfo))
            {
                if (hitInfo.ContainsKey(cwc))
                    hitInfo[cwc] = (hitContext, Clock.Time);
                else
                    hitInfo[new ObjectWrapper<CustomWeaponComponent>(cwc)] = (hitContext, Clock.Time);
            }
            else
            {
                enemy = new(enemy);
                _lastHits[enemy] = new()
                {
                    [new ObjectWrapper<CustomWeaponComponent>(cwc)] = (hitContext, Clock.Time)
                };
            }
            _shownHits[enemy] = false;
        }

        public bool TryGetContexts(ObjectWrapper<Agent> enemy, [MaybeNullWhen(false)] out Dictionary<ObjectWrapper<CustomWeaponComponent>, (WeaponHitDamageableContext context, float time)> hitsDict, out IntPtr lastCWCPtr)
        {
            _lastHits.Keys
                .Where(wrapper => wrapper.Object == null)
                .ToList()
                .ForEach(wrapper =>
                {
                    _lastHits.Remove(wrapper);
                    _shownHits.Remove(wrapper);
                });

            if (!_shownHits.ContainsKey(enemy) || _shownHits[enemy])
            {
                hitsDict = null;
                lastCWCPtr = IntPtr.Zero;
                return false;
            }

            _shownHits[enemy] = true;
            hitsDict = _lastHits[enemy];
            if (hitsDict == null || hitsDict.Count == 0)
            {
                lastCWCPtr = IntPtr.Zero;
                return false;
            }

            float maxTime = 0f;
            lastCWCPtr = IntPtr.Zero;
            foreach ((var cwc, (var context, float time)) in hitsDict)
            {
                if (time > maxTime)
                {
                    lastCWCPtr = cwc.Pointer;
                    maxTime = time;
                }
            }
            return true;
        }
    }
}

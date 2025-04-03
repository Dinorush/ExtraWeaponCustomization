using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpreadMod :
        TriggerMod,
        IGunProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        private readonly TriggerStack _triggerStack;
        private Coroutine? _updateRoutine;
        private readonly Queue<float> _updateTimes;

        public SpreadMod()
        {
            _updateTimes = new();
            _triggerStack = new(this);
        }

        public override bool ShouldRegister(Type contextType) => CWC.IsLocal && base.ShouldRegister(contextType);

        public override void TriggerReset()
        {
            _triggerStack.Clear();
            _updateTimes.Clear();
            CoroutineUtil.Stop(ref _updateRoutine, CWC);
            CWC.SpreadController!.ClearMod(this);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            _triggerStack.Add(contexts);
            if (_triggerStack.TryGetMod(out var mod))
            {
                CWC.SpreadController!.SetMod(this, mod);
                EnqueueUpdate();
            }
        }

        public void Invoke(WeaponSetupContext context)
        {
            if (!_triggerStack.TryGetMod(out _))
            {
                _updateTimes.Clear();
                return;
            }

            _updateRoutine ??= CoroutineManager.StartCoroutine(DelayedUpdate().WrapToIl2Cpp());
        }

        public void Invoke(WeaponClearContext context)
        {
            CoroutineUtil.Stop(ref _updateRoutine);
            CWC.SpreadController!.ClearMod(this);
        }

        private void EnqueueUpdate()
        {
            if (CombineModifiers)
                _updateTimes.TryDequeue(out _);
            _updateTimes.Enqueue(Clock.Time + Duration);
            _updateRoutine ??= CoroutineManager.StartCoroutine(DelayedUpdate().WrapToIl2Cpp());
        }

        private IEnumerator DelayedUpdate()
        {
            while (_triggerStack.TryGetMod(out float mod))
            {
                CWC.SpreadController!.SetMod(this, mod);
                float nextTime;
                while (_updateTimes.TryPeek(out nextTime) && nextTime <= Clock.Time)
                    _updateTimes.Dequeue();

                if (_updateTimes.Count > 0)
                    yield return new WaitForSeconds(nextTime - Clock.Time);
                else
                    yield return null;
            }
            CWC.SpreadController!.ClearMod(this);
            _updateRoutine = null;
        }
    }
}

using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public abstract class TriggerModTimed :
        TriggerMod,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        private readonly TriggerStack _triggerStack;
        private Coroutine? _updateRoutine;
        private readonly Queue<float> _updateTimes;

        public TriggerModTimed()
        {
            _updateTimes = new();
            _triggerStack = new(this);
        }

        protected abstract void OnUpdate(float mod);
        protected abstract void OnDisable();

        public override void TriggerReset()
        {
            _triggerStack.Clear();
            _updateTimes.Clear();
            CoroutineUtil.Stop(ref _updateRoutine);
            OnDisable();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            _triggerStack.Add(contexts);
            if (_triggerStack.TryGetMod(out var mod))
            {
                OnUpdate(mod);
                EnqueueUpdate();
            }
            else if(CoroutineUtil.Stop(ref _updateRoutine))
                OnDisable();
        }

        public void Invoke(WeaponSetupContext context)
        {
            if (!_triggerStack.TryGetMod(out var mod))
            {
                _updateTimes.Clear();
                return;
            }

            OnUpdate(mod);
            _updateRoutine ??= CoroutineManager.StartCoroutine(DelayedUpdate().WrapToIl2Cpp());
        }

        public void Invoke(WeaponClearContext context)
        {
            CoroutineUtil.Stop(ref _updateRoutine);
            OnDisable();
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
                OnUpdate(mod);
                float nextTime;
                while (_updateTimes.TryPeek(out nextTime) && nextTime <= Clock.Time)
                    _updateTimes.Dequeue();

                if (_updateTimes.Count > 0)
                    yield return new WaitForSeconds(nextTime - Clock.Time);
                else
                    yield return null;
            }
            OnDisable();
            _updateTimes.Clear();
            _updateRoutine = null;
        }
    }
}

using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using MovementSpeedAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpeedMod :
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        private const string APIGroup = "EWC";

        private readonly TriggerStack _triggerStack;
        private Coroutine? _updateRoutine;
        private readonly Queue<float> _updateTimes;
        private ISpeedModifier _speedModifier;

        public SpeedMod()
        {
            _speedModifier = MoveSpeedAPI.AddModifier(1f, LayerToAPILayer(), APIGroup);
            _speedModifier.Disable();
            _updateTimes = new();
            _triggerStack = new(this);
        }

        public override bool ShouldRegister(Type contextType) => CWC.IsLocal && base.ShouldRegister(contextType);

        public override void TriggerReset()
        {
            _triggerStack.Clear();
            _updateTimes.Clear();
            CoroutineUtil.Stop(ref _updateRoutine, CWC);
            _speedModifier.Disable();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            _triggerStack.Add(contexts);
            if (_triggerStack.TryGetMod(out var mod))
            {
                _speedModifier.Enable(mod);
                EnqueueUpdate();
            }
        }

        public void Invoke(WeaponSetupContext context)
        {
            if (!_triggerStack.TryGetMod(out var mod))
            {
                _updateTimes.Clear();
                return;
            }

            _speedModifier.Enable(mod);
            _updateRoutine ??= CoroutineManager.StartCoroutine(DelayedUpdate().WrapToIl2Cpp());
        }

        public void Invoke(WeaponClearContext context)
        {
            CoroutineUtil.Stop(ref _updateRoutine);
            _speedModifier.Disable();
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
                _speedModifier.Mod = mod;
                float nextTime;
                while (_updateTimes.TryPeek(out nextTime) && nextTime <= Clock.Time)
                    _updateTimes.Dequeue();

                if (_updateTimes.Count > 0)
                    yield return new WaitForSeconds(nextTime - Clock.Time);
                else
                    yield return null;
            }
            _speedModifier.Disable();
            _updateTimes.Clear();
            _updateRoutine = null;
        }

        private StackLayer LayerToAPILayer()
        {
            return StackLayer switch
            {
                StackType.Override => MovementSpeedAPI.StackLayer.Override,
                StackType.Max => Mod > 1 ? MovementSpeedAPI.StackLayer.Max : MovementSpeedAPI.StackLayer.Min,
                StackType.Mult => MovementSpeedAPI.StackLayer.Multiply,
                StackType.Add => MovementSpeedAPI.StackLayer.Add,
                _ => MovementSpeedAPI.StackLayer.Multiply
            };
        }
    }
}

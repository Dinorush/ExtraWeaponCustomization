using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using GameData;
using Gear;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using UnityEngine;

namespace EWC.CustomWeapon
{
    public sealed class CustomMeleeComponent : CustomWeaponComponent
    {
        public readonly MeleeComp Melee;

        public float CurrentAttackSpeed { get; private set; }
        public float LightAttackSpeed => _baseLightSpeed * CurrentAttackSpeed;
        public float ChargedAttackSpeed => _baseChargeSpeed * CurrentAttackSpeed;
        public float PushAttackSpeed => _basePushSpeed * CurrentAttackSpeed;

        private readonly MWS_AttackLight _lightLeft;
        private readonly MWS_AttackLight _lightRight;
        private readonly Animator _animator;
        private readonly Il2CppReferenceArray<MWS_Base> _states;
        private readonly MeleeAnimationSetDataBlock _animData;

        private readonly float _baseLightSpeed;
        private readonly float _baseChargeSpeed;
        private readonly float _basePushSpeed;

        public CustomMeleeComponent(IntPtr value) : base(value)
        {
            Melee = (MeleeComp)Weapon;
            CurrentAttackSpeed = 1f;

            _animData = Melee.Value.MeleeAnimationData;
            _states = Melee.Value.m_states;
            _lightLeft = _states[(int)eMeleeWeaponState.AttackMissLeft].Cast<MWS_AttackLight>();
            _lightRight = _states[(int)eMeleeWeaponState.AttackMissRight].Cast<MWS_AttackLight>();
            _animator = Melee.Value.WeaponAnimator;

            var mods = MSCWrapper.GetAttackSpeedMods(Melee.Value);
            _baseLightSpeed = mods.lightAttackSpeed;
            _baseChargeSpeed = mods.chargedAttackSpeed;
            _basePushSpeed = mods.pushAttackSpeed;
        }

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomMeleeComponent>();
        }

        public override void Clear()
        {
            CurrentAttackSpeed = 1f;
            if (!_destroyed)
                SetMeleeAttackTimings();
        }

        public void UpdateAttackSpeed()
        {
            float newAttackSpeed = Invoke(new WeaponFireRateContext(1f)).Value;

            if (newAttackSpeed != CurrentAttackSpeed)
            {
                CurrentAttackSpeed = newAttackSpeed;
                SetMeleeAttackTimings();
            }
        }

        public void ModifyAttackSpeed(eMeleeWeaponState state)
        {
            switch (state)
            {
                case eMeleeWeaponState.AttackMissLeft:
                    _lightLeft.m_wantedNormalSpeed = LightAttackSpeed;
                    _lightLeft.m_wantedChargeSpeed = LightAttackSpeed * 0.3f;
                    break;
                case eMeleeWeaponState.AttackMissRight:
                    _lightRight.m_wantedNormalSpeed = LightAttackSpeed;
                    _lightRight.m_wantedChargeSpeed = LightAttackSpeed * 0.3f;
                    break;
                case eMeleeWeaponState.AttackHitLeft:
                case eMeleeWeaponState.AttackHitRight:
                    _animator.speed = LightAttackSpeed;
                    break;
                case eMeleeWeaponState.AttackChargeReleaseLeft:
                case eMeleeWeaponState.AttackChargeReleaseRight:
                    _animator.speed = ChargedAttackSpeed;
                    break;
                case eMeleeWeaponState.Push: // Push sets speed based on stamina
                    _animator.speed *= PushAttackSpeed;
                    break;
            }
        }

        private void SetMeleeAttackTimings()
        {
            float mod = 1f / LightAttackSpeed;
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackMissLeft].AttackData, _animData.FPAttackMissLeft, mod);
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackMissRight].AttackData, _animData.FPAttackMissRight, mod);
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackHitLeft].AttackData, _animData.FPAttackHitLeft, mod);
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackHitRight].AttackData, _animData.FPAttackHitRight, mod);

            mod = 1f / ChargedAttackSpeed;
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackChargeReleaseLeft].AttackData, _animData.FPAttackChargeUpReleaseLeft, mod);
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackChargeReleaseRight].AttackData, _animData.FPAttackChargeUpReleaseRight, mod);
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackChargeHitLeft].AttackData, _animData.FPAttackChargeUpHitLeft, mod);
            CopyMeleeData(_states[(int)eMeleeWeaponState.AttackChargeHitRight].AttackData, _animData.FPAttackChargeUpHitRight, mod);

            mod = 1f / PushAttackSpeed;
            CopyMeleeData(_states[(int)eMeleeWeaponState.Push].AttackData, _animData.FPAttackPush, mod);
        }

        private static void CopyMeleeData(MeleeAttackData data, MeleeAnimationSetDataBlock.MeleeAttackData animAttackData, float mod = 1f)
        {
            data.m_attackLength = animAttackData.AttackLengthTime * mod;
            data.m_attackHitTime = animAttackData.AttackHitTime * mod;
            data.m_damageStartTime = animAttackData.DamageStartTime * mod;
            data.m_damageEndTime = animAttackData.DamageEndTime * mod;
            data.m_attackCamFwdHitTime = animAttackData.AttackCamFwdHitTime * mod;
            data.m_comboEarlyTime = animAttackData.ComboEarlyTime * mod;
        }
    }
}

using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.Audio;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.JSON;
using GameData;
using Gear;
using Player;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class AudioSwap : 
        Trait,
        IGunProperty,
        ITriggerCallback,
        IWeaponProperty<WeaponAudioSwapContextSync>,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        public uint AudioID { get; set; } = 0;
        public float Duration { get; set; } = 0f;
        private TriggerCoordinator? _coordinator;
        public TriggerCoordinator? Trigger
        {
            get => _coordinator;
            set
            {
                _coordinator = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        private WeaponAudioDataBlock? _cachedBlock;
        private WeaponAudioDataBlock? _audioBlock;

        private Coroutine? _activeRoutine;
        private float _endTime = 0f;

        public void Invoke(WeaponTriggerContext context) => Trigger?.Invoke(context);

        public void Invoke(WeaponSetupContext context)
        {
            _audioBlock ??= WeaponAudioDataBlock.GetBlock(AudioID);
            if (Clock.Time < _endTime) // In case it was previously cleared by Temp Properties
                ApplyAudio();
        }

        public void Invoke(WeaponClearContext context) => ClearAudio();

        public void TriggerReset()
        {
            _endTime = 0;
            if (_activeRoutine != null)
            {
                CoroutineManager.StopCoroutine(_activeRoutine);
                ClearAudio();
            }
            _activeRoutine = null;
        }

        public void TriggerApply(List<TriggerContext> contexts)
        {
            _audioBlock ??= WeaponAudioDataBlock.GetBlock(AudioID);
            if (_audioBlock == null) return;

            _endTime = Clock.Time + Duration;
            _activeRoutine ??= CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(DeactivateAfterDelay()));

            AudioSwapManager.SendInstance(CWC.Weapon.Owner.Owner, PlayerAmmoStorage.GetSlotFromAmmoType(CWC.Weapon.AmmoType));
        }

        public void Invoke(WeaponAudioSwapContextSync context)
        {
            _audioBlock ??= WeaponAudioDataBlock.GetBlock(AudioID);
            if (_audioBlock == null) return;

            _endTime = Clock.Time + Duration;
            _activeRoutine ??= CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(DeactivateAfterDelay()));
        }

        private IEnumerator DeactivateAfterDelay()
        {
            ApplyAudio();
            while (Clock.Time < _endTime)
                yield return new WaitForSeconds(_endTime - Clock.Time);
            ClearAudio();
            _activeRoutine = null;
        }

        private void ApplyAudio()
        {
            if (_audioBlock != null)
            {
                SetLoopingAudio(CWC.Gun!, false);
                _cachedBlock = CWC.Gun!.AudioData;
                CWC.Gun.AudioData = _audioBlock;
                CWC.Gun.SetupAudioEvents();
                SetLoopingAudio(CWC.Gun!, true);
            }
        }

        private void ClearAudio()
        {
            if (_cachedBlock != null)
            {
                SetLoopingAudio(CWC.Gun!, false);
                CWC.Gun!.AudioData = _cachedBlock;
                CWC.Gun.SetupAudioEvents();
                SetLoopingAudio(CWC.Gun!, true);
                _cachedBlock = null;
            }
        }

        private void SetLoopingAudio(BulletWeapon gun, bool start)
        {
            if (!gun.m_archeType.m_firing) return;

            if (gun.ArchetypeData.FireMode == eWeaponFireMode.Auto && !gun.AudioData.TriggerAutoAudioForEachShot)
            {
                if (start)
                    gun.TriggerAutoFireStartAudio();
                else
                    gun.TriggerAutoFireEndAudio();
            }

            if (gun.ArchetypeData.FireMode == eWeaponFireMode.Burst && !gun.AudioData.TriggerBurstAudioForEachShot)
            {
                if (start)
                    gun.TriggerBurstFireAudio();
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(AudioID), AudioID);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteString(nameof(Trigger), "Invalid");
            writer.WriteEndObject();
        }

        public override IWeaponProperty Clone()
        {
            return new AudioSwap()
            {
                AudioID = AudioID,
                Duration = Duration,
                Trigger = Trigger?.Clone()
            };
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "audioid":
                case "audio":
                case "id":
                    AudioID = reader.GetUInt32();
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "triggertype":
                case "trigger":
                    Trigger = EWCJson.Deserialize<TriggerCoordinator>(ref reader);
                    break;
            }
        }
    }
}

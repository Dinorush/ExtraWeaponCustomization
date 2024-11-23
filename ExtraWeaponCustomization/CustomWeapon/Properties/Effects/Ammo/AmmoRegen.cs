using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class AmmoRegen :
        Effect,
        IGunProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponWieldContext>,
        IWeaponProperty<WeaponUnWieldContext>,
        IWeaponProperty<WeaponPreReloadContext>
    {
        public float ClipRegen { get; private set; } = 0f;
        public float ReserveRegen { get; private set; } = 0f;
        public bool OverflowToReserve { get; private set; } = true;
        public bool PullFromReserve { get; private set; } = false;
        public bool UseRawAmmo { get; private set; } = false;
        public bool AllowReload { get; private set; } = true;
        public bool ActiveInHolster { get; private set; } = true;
        public float DelayAfterTrigger { get; private set; } = 0f;

        private float _clipBuffer = 0f;
        private float _reserveBuffer = 0f;
        private PlayerAmmoStorage? _ammoStorage;
        private InventorySlotAmmo? _slotAmmo;

        private float _lastTickTime = 0f;
        private float _nextTickTime = 0f;
        private Coroutine? _updateRoutine;

        public void Invoke(WeaponSetupContext _)
        {
            if (ActiveInHolster && _updateRoutine == null)
                _updateRoutine = CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        public void Invoke(WeaponClearContext _)
        {
            if (_updateRoutine != null)
            {
                CoroutineManager.StopCoroutine(_updateRoutine);
                _updateRoutine = null;
            }
        }

        public void Invoke(WeaponWieldContext _)
        {
            if (!ActiveInHolster && _updateRoutine == null)
                _updateRoutine = CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        public void Invoke(WeaponUnWieldContext _)
        {
            if (!ActiveInHolster && _updateRoutine != null)
            {
                CoroutineManager.StopCoroutine(_updateRoutine);
                _updateRoutine = null;
            }
        }

        public void Invoke(WeaponPreReloadContext context)
        {
            context.Allow &= AllowReload;
        }

        private bool ResetCache()
        {
            if (_ammoStorage == null || _slotAmmo == null)
            {
                PlayerBackpack? backpack = PlayerBackpackManager.LocalBackpack;
                if (backpack == null) return false;

                _ammoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
                if (_ammoStorage == null) return false;

                _slotAmmo = _ammoStorage.GetInventorySlotAmmo(CWC.Weapon.AmmoType);
                if (_slotAmmo == null) return false;
            }
            return true;
        }

        public IEnumerator Update()
        {
            _lastTickTime = Clock.Time;
            
            // Sometimes a CWC appears that dies but doesn't run OnDestroy
            while (CWC != null)
            {
                if (!ResetCache())
                {
                    yield return null;
                    continue;
                }

                float time = Clock.Time;
                if (_nextTickTime > time)
                {
                    yield return new WaitForSeconds(_nextTickTime - time);
                    continue;
                }

                float delta = Math.Min(time - _nextTickTime, time - _lastTickTime);
                _clipBuffer += ClipRegen * delta;
                _reserveBuffer += ReserveRegen * delta;
                _lastTickTime = time;

                float costOfBullet = _slotAmmo!.CostOfBullet;
                float min = UseRawAmmo ? costOfBullet : 1f;
                if (Math.Abs(_clipBuffer) < min && Math.Abs(_reserveBuffer) < min)
                {
                    yield return null;
                    continue;
                }

                if (UseRawAmmo)
                {
                    _clipBuffer /= costOfBullet;
                    _reserveBuffer /= costOfBullet;
                }

                // Calculate the actual changes we can make to clip/ammo
                int currClip = CWC.Weapon.GetCurrentClip();
                int clipChange = (int)(PullFromReserve ? Math.Min(_clipBuffer, _slotAmmo.BulletsInPack) : _clipBuffer);
                int newClip = Math.Clamp(currClip + clipChange, 0, CWC.Weapon.GetMaxClip());

                // If we overflow/underflow the magazine, send the rest to reserves (if not pulling from reserves)
                int bonusReserve = OverflowToReserve ? clipChange - (newClip - currClip) : 0;
                clipChange = newClip - currClip;

                int reserveChange = (int)(PullFromReserve ? _reserveBuffer - clipChange : _reserveBuffer + bonusReserve);

                _clipBuffer -= (int)_clipBuffer;
                _reserveBuffer -= (int)_reserveBuffer;

                CWC.Weapon.SetCurrentClip(newClip);

                if (UseRawAmmo)
                {
                    _clipBuffer *= costOfBullet;
                    _reserveBuffer *= costOfBullet;
                }

                float reserveCost = reserveChange * costOfBullet;
                if (_slotAmmo.IsFull)
                {
                    if (reserveCost < 0)
                        _slotAmmo.AmmoInPack = Math.Max(0, _slotAmmo.AmmoInPack + reserveCost);
                }
                else
                    _slotAmmo.AmmoInPack = Math.Clamp(_slotAmmo.AmmoInPack + reserveCost, 0, _slotAmmo.AmmoMaxCap);

                _slotAmmo.OnBulletsUpdateCallback?.Invoke(_slotAmmo.BulletsInPack);
                _ammoStorage!.NeedsSync = true;
                _ammoStorage.UpdateSlotAmmoUI(_slotAmmo, newClip);
                yield return null;
            }
        }

        public override void TriggerApply(List<TriggerContext> _)
        {
            _nextTickTime = Clock.Time + DelayAfterTrigger;
        }

        public override void TriggerReset()
        {
            _nextTickTime = 0f;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ClipRegen), ClipRegen);
            writer.WriteNumber(nameof(ReserveRegen), ReserveRegen);
            writer.WriteBoolean(nameof(OverflowToReserve), OverflowToReserve);
            writer.WriteBoolean(nameof(PullFromReserve), PullFromReserve);
            writer.WriteBoolean(nameof(UseRawAmmo), UseRawAmmo);
            writer.WriteBoolean(nameof(AllowReload), AllowReload);
            writer.WriteBoolean(nameof(ActiveInHolster), ActiveInHolster);
            writer.WriteNumber(nameof(DelayAfterTrigger), DelayAfterTrigger);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "clipregen":
                case "clipchange":
                case "clip":
                    ClipRegen = reader.GetSingle();
                    break;
                case "reserveregen":
                case "reservechange":
                case "reserve":
                    ReserveRegen = reader.GetSingle();
                    break;
                case "overflowtoreserve":
                case "overflow":
                    OverflowToReserve = reader.GetBoolean();
                    break;
                case "pullfromreserve":
                    PullFromReserve = reader.GetBoolean();
                    break;
                case "userawammo":
                case "useammo":
                    UseRawAmmo = reader.GetBoolean();
                    break;
                case "allowreload":
                    AllowReload = reader.GetBoolean();
                    break;
                case "activeinholster":
                    ActiveInHolster = reader.GetBoolean();
                    break;
                case "delayaftertrigger":
                    DelayAfterTrigger = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}

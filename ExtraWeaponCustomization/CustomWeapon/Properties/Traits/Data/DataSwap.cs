using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.JSON;
using GameData;
using Gear;
using Player;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class DataSwap : 
        Trait,
        IGunProperty,
        ITriggerCallbackSync,
        IWeaponProperty<WeaponOwnerSetContext>,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        public ushort SyncID { get; set; }

        public uint ArchetypeID { get; private set; } = 0;
        public uint AudioID { get; private set; } = 0;
        public float Duration { get; private set; } = 0f;
        public bool EndBurst { get; private set; } = false;
        public bool EndCharge { get; private set; } = false;
        public bool KeepMagCost { get; private set; } = false;

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

        private ArchetypeDataBlock? _cachedArchBlock;
        private ArchetypeDataBlock? _archBlock;
        private BulletWeaponArchetype? _cachedArchetype;
        private BulletWeaponArchetype? _archetype;
        private int _magBuffer = 0;
        private float _costBuffer = 0f;

        private WeaponAudioDataBlock? _cachedAudioBlock;
        private WeaponAudioDataBlock? _audioBlock;

        private Coroutine? _activeRoutine;
        private float _endTime = 0f;

        public void Invoke(WeaponTriggerContext context) => Trigger?.Invoke(context);

        public void Invoke(WeaponOwnerSetContext context)
        {
            _archetype?.SetOwner(CWC.Weapon.Owner);
            _cachedArchetype?.SetOwner(CWC.Weapon.Owner);

            ResetERDComponent();
            CWC.RefreshSoundDelay();
        }

        public void Invoke(WeaponSetupContext context)
        {
            if (GetData() && Clock.Time < _endTime) // In case it was previously cleared by Temp Properties
                ApplyData();
        }

        public void Invoke(WeaponClearContext context) => ClearData();

        public void TriggerReset()
        {
            TriggerResetSync();
            TriggerManager.SendReset(this);
        }

        public void TriggerResetSync()
        {
            _endTime = 0;
            if (_activeRoutine != null)
            {
                CoroutineManager.StopCoroutine(_activeRoutine);
                ClearData();
            }
            _activeRoutine = null;
        }

        public void TriggerApply(List<TriggerContext> contexts)
        {
            if (!GetData()) return;

            TriggerApplySync();
            TriggerManager.SendInstance(this);
        }

        public void TriggerApplySync(float mod = 1f)
        {
            if (!GetData()) return;

            _endTime = Clock.Time + Duration;
            _activeRoutine ??= CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(DeactivateAfterDelay()));
        }

        private IEnumerator DeactivateAfterDelay()
        {
            ApplyData();
            while (Clock.Time < _endTime)
                yield return new WaitForSeconds(_endTime - Clock.Time);
            ClearData();
            _activeRoutine = null;
        }

        private bool GetData()
        {
            _archBlock ??= ArchetypeDataBlock.GetBlock(ArchetypeID);
            _audioBlock ??= WeaponAudioDataBlock.GetBlock(AudioID);
            if (_archBlock != null && _archetype == null)
            {
                switch (_archBlock.FireMode)
                {
                    case eWeaponFireMode.Semi:
                        _archetype = new BWA_Semi(_archBlock);
                        break;
                    case eWeaponFireMode.Burst:
                        _archetype = new BWA_Burst(_archBlock);
                        break;
                    case eWeaponFireMode.Auto:
                        _archetype = new BWA_Auto(_archBlock);
                        break;
                    case eWeaponFireMode.SemiBurst:
                        _archetype = new BWA_SemiBurst(_archBlock);
                        break;
                }
                _archetype!.Setup(CWC.Gun!);
                if (_archBlock.FireMode == eWeaponFireMode.Burst)
                {
                    var burst = _archetype.TryCast<BWA_Burst>()!;
                    burst.m_burstMax = _archBlock.BurstShotCount;
                }

                if (CWC.HasTrait(typeof(AutoTrigger)))
                    _archetype.m_triggerNeedsPress = false;

                if (CWC.Gun!.Owner?.IsLocallyOwned == true)
                {
                    _archetype.SetOwner(CWC.Gun.Owner); // Why does this set audio delay
                    ResetERDComponent();
                    CWC.RefreshSoundDelay();
                }
            }
            return _archBlock != null || _audioBlock != null;
        }

        private void ApplyData()
        {
            if (_archBlock != null)
                ChangeArchetype();

            if (_audioBlock != null)
            {
                SetLoopingAudio(CWC.Gun!, false);
                _cachedAudioBlock = CWC.Gun!.AudioData;
                CWC.Gun.AudioData = _audioBlock;
                CWC.Gun.SetupAudioEvents();
                SetLoopingAudio(CWC.Gun!, true);
            }
        }

        private void ClearData()
        {
            if (_cachedArchBlock != null)
                ChangeArchetype();

            if (_cachedAudioBlock != null)
            {
                SetLoopingAudio(CWC.Gun!, false);
                CWC.Gun!.AudioData = _cachedAudioBlock;
                CWC.Gun.SetupAudioEvents();
                SetLoopingAudio(CWC.Gun!, true);
                _cachedAudioBlock = null;
            }
        }

        private void ChangeArchetype()
        {
            ArchetypeDataBlock newBlock;
            BulletWeaponArchetype newArch;
            BulletWeaponArchetype oldArch;
            if (_cachedArchBlock != null)
            {
                newBlock = _cachedArchBlock;
                newArch = _cachedArchetype!;
                oldArch = _archetype!;
                _cachedArchBlock = null;
                _cachedArchetype = null;
            }
            else
            {
                newBlock = _archBlock!;
                newArch = _archetype!;
                _cachedArchBlock = CWC.Gun!.ArchetypeData;
                _cachedArchetype = CWC.Gun.m_archeType;
                oldArch = _cachedArchetype!;
            }

            PlayerAmmoStorage ammoStorage = PlayerBackpackManager.GetBackpack(CWC.Gun!.Owner.Owner).AmmoStorage;
            InventorySlotAmmo slotAmmo = ammoStorage.GetInventorySlotAmmo(CWC.Gun!.AmmoType);

            int clip = CWC.Gun!.GetCurrentClip();
            float clipCost = CWC.Gun!.GetCurrentClip() * slotAmmo.CostOfBullet;
            int clipSize = CWC.Gun!.ClipSize;

            CWC.Gun.ArchetypeData = newBlock;
            CopyArchetypeVars(newArch, oldArch);
            CWC.Gun.m_archeType = newArch;
            CWC.RefreshArchetypeCache();
            ResetERDComponent();

            slotAmmo.Setup(newBlock.CostOfBullet * EXPAPIWrapper.GetAmmoMod(), CWC.Gun.ClipSize);
            int newClipSize = CWC.Gun.ClipSize;

            if (KeepMagCost)
            {
                float newBuffer = clipCost;
                _costBuffer += newBuffer;
                int newMag = Mathf.Min(newClipSize, (int) (_costBuffer / slotAmmo.CostOfBullet));
                _costBuffer -= newMag * slotAmmo.CostOfBullet;
                newBuffer -= newMag * slotAmmo.CostOfBullet;

                slotAmmo.AmmoInPack += newBuffer;
                CWC.Gun.SetCurrentClip(newMag);
            }
            else
            {
                // Calculate the target magazine
                float newMagFloat = (float) clip / clipSize * newClipSize + _magBuffer;
                int newMag = Mathf.Min(newClipSize, (int)newMagFloat);

                // Calculate the maximum amount of ammo we can give
                float trgtCost = Mathf.Min(slotAmmo.AmmoInPack + clipCost, newMag * slotAmmo.CostOfBullet);
                newMagFloat = trgtCost / slotAmmo.CostOfBullet;
                newMag = (int)newMagFloat;

                _magBuffer = Mathf.Max(0, clip - newMag * clipSize / newClipSize);
                slotAmmo.AmmoInPack += clipCost - newMag * slotAmmo.CostOfBullet;
                CWC.Gun.SetCurrentClip(newMag); 
            }

            newArch.m_clip = CWC.Gun.GetCurrentClip();
            if (CWC.Weapon.Owner.IsLocallyOwned && GuiManager.CrosshairLayer.m_circleCrosshair.Visible)
                GuiManager.CrosshairLayer.ShowSpreadCircle(CWC.Gun.RecoilData.hipFireCrosshairSizeDefault);
            ammoStorage.UpdateSlotAmmoUI(slotAmmo, CWC.Gun.GetCurrentClip());
            ammoStorage.NeedsSync = true;
        }

        private void CopyArchetypeVars(BulletWeaponArchetype newArch, BulletWeaponArchetype oldArch)
        {
            if (oldArch.m_inChargeup)
            {
                if (EndCharge)
                {
                    oldArch.OnStopChargeup();
                    oldArch.m_nextShotTimer = Clock.Time + oldArch.ShotDelay();
                }
                else if (newArch.HasChargeup)
                {
                    newArch.m_inChargeup = oldArch.m_inChargeup;
                    newArch.m_chargeupTimer = oldArch.m_chargeupTimer - oldArch.ChargeupDelay() + newArch.ChargeupDelay();
                }
                else
                    oldArch.OnStopChargeup();
            }

            if (oldArch.m_archetypeData.FireMode == eWeaponFireMode.Burst && !oldArch.BurstIsDone())
            {
                var sendBurst = oldArch.TryCast<BWA_Burst>()!;
                if (EndBurst)
                {
                    sendBurst.m_burstCurrentCount = 0;
                    sendBurst.PostFireCheck();
                }
                else if (newArch.m_archetypeData.FireMode == eWeaponFireMode.Burst)
                {
                    var recvBurst = newArch.TryCast<BWA_Burst>()!;
                    recvBurst.m_burstCurrentCount = Mathf.Min(sendBurst.m_burstCurrentCount, recvBurst.m_burstMax);
                    sendBurst.m_burstCurrentCount = 0;
                }
            }

            if (oldArch.m_firing && oldArch.m_archetypeData.FireMode != newArch.m_archetypeData.FireMode)
                oldArch.OnStopFiring();

            newArch.m_nextBurstTimer = oldArch.m_nextBurstTimer;
            newArch.m_nextShotTimer = oldArch.m_nextShotTimer;
            newArch.m_fireHeld = oldArch.m_fireHeld;
            newArch.m_firePressed = oldArch.m_firePressed;
            newArch.m_firing = oldArch.m_firing;
            newArch.m_readyToFire = oldArch.m_readyToFire;
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

        private void ResetERDComponent() => ERDAPIWrapper.ChangeCustomRecoil(CWC.Weapon.ArchetypeID, CWC.Gun!);

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ArchetypeID), ArchetypeID);
            writer.WriteNumber(nameof(AudioID), AudioID);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(EndBurst), EndBurst);
            writer.WriteBoolean(nameof(EndCharge), EndCharge);
            writer.WriteBoolean(nameof(KeepMagCost), KeepMagCost);
            writer.WriteString(nameof(Trigger), "Invalid");
            writer.WriteEndObject();
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (DataSwap) base.Clone();
            copy.Trigger = Trigger?.Clone();
            return copy;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "weaponaudioid":
                case "weaponaudio":
                case "audioid":
                case "audio":
                    AudioID = reader.GetUInt32();
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "archetypeid":
                case "archetype":
                case "archid":
                case "arch":
                    ArchetypeID = reader.GetUInt32();
                    break;
                case "endburst":
                    EndBurst = reader.GetBoolean();
                    break;
                case "endcharge":
                    EndCharge = reader.GetBoolean();
                    break;
                case "keepmagcost":
                case "keepmag":
                case "keepclipcost":
                case "keepclip":
                    KeepMagCost = reader.GetBoolean();
                    break;
                case "triggertype":
                case "trigger":
                    Trigger = EWCJson.Deserialize<TriggerCoordinator>(ref reader);
                    break;
            }
        }
    }
}

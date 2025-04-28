using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using EWC.Utils;
using GameData;
using Gear;
using Player;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class DataSwap : 
        Trait,
        IGunProperty,
        ITriggerCallbackBasicSync,
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
        public bool EndFiring { get; private set; } = false;
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

        private readonly DelayedCallback _applyCallback;

        public DataSwap()
        {
            _applyCallback = new(
                () => Duration,
                ApplyData,
                ClearData
            );
        }

        public override bool ShouldRegister(Type contextType)
        {
            if (Trigger == null && contextType == typeof(WeaponTriggerContext)) return false;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponTriggerContext context) => Trigger!.Invoke(context);

        public void Invoke(WeaponOwnerSetContext context)
        {
            _archetype?.SetOwner(CWC.Weapon.Owner);
            _cachedArchetype?.SetOwner(CWC.Weapon.Owner);

            ResetERDComponent();
            CWC.RefreshSoundDelay();
        }

        public void Invoke(WeaponSetupContext context)
        {
            if (GetData() && _applyCallback.Active) // In case it was previously cleared by Temp Properties
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
            _applyCallback.Stop();
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

            CWC.StartDelayedCallback(_applyCallback);
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
                    var burst = _archetype.Cast<BWA_Burst>();
                    burst.m_burstMax = _archBlock.BurstShotCount;
                }

                if (CWC.HasTrait<AutoTrigger>())
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
                SetLoopingAudio(false);
                _cachedAudioBlock = CWC.Gun!.AudioData;
                CWC.Gun.AudioData = _audioBlock;
                CWC.Gun.SetupAudioEvents();
                SetLoopingAudio(true);
            }
        }

        private void ClearData()
        {
            if (_cachedArchBlock != null)
                ChangeArchetype();

            if (_cachedAudioBlock != null)
            {
                SetLoopingAudio(false);
                CWC.Gun!.AudioData = _cachedAudioBlock;
                CWC.Gun.SetupAudioEvents();
                SetLoopingAudio(true);
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
                _cachedArchBlock = CWC.ArchetypeData;
                _cachedArchetype = CWC.GunArchetype;
                oldArch = _cachedArchetype!;
            }

            PlayerAmmoStorage ammoStorage = PlayerBackpackManager.GetBackpack(CWC.Gun!.Owner.Owner).AmmoStorage;
            InventorySlotAmmo slotAmmo = ammoStorage.GetInventorySlotAmmo(CWC.Gun!.AmmoType);

            int clip = CWC.Gun!.GetCurrentClip();
            float clipCost = CWC.Gun!.GetCurrentClip() * slotAmmo.CostOfBullet;
            int clipSize = CWC.Gun!.ClipSize;

            CWC.ArchetypeData = newBlock;
            CopyArchetypeVars(newArch, oldArch);
            CWC.GunArchetype = newArch;
            CWC.RefreshArchetypeCache();
            ResetERDComponent();

            slotAmmo.Setup(newBlock.CostOfBullet / EXPAPIWrapper.GetAmmoMod(), CWC.Gun.ClipSize);
            int newClipSize = CWC.Gun.ClipSize;

            if (KeepMagCost)
            {
                float newBuffer = clipCost;
                _costBuffer += newBuffer + 1e-10f; // Some small constant to avoid rounding errors
                int newMag = Math.Min(newClipSize, (int) (_costBuffer / slotAmmo.CostOfBullet));
                _costBuffer -= newMag * slotAmmo.CostOfBullet;
                newBuffer -= newMag * slotAmmo.CostOfBullet;

                slotAmmo.AmmoInPack += newBuffer;
                CWC.Gun.SetCurrentClip(newMag);
            }
            else
            {
                // Calculate the target magazine
                float newMagFloat = (float)clip / clipSize * newClipSize + _magBuffer;
                int newMag = Math.Min(newClipSize, (int)newMagFloat);

                // Calculate the maximum amount of ammo we can give
                float maxCost = slotAmmo.AmmoInPack + clipCost;
                if (maxCost < newMag * slotAmmo.CostOfBullet)
                    newMag = (int)(maxCost / slotAmmo.CostOfBullet);

                _magBuffer = Math.Max(0, clip - newMag * clipSize / newClipSize);
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
                var sendBurst = oldArch.Cast<BWA_Burst>();
                if (EndBurst)
                {
                    sendBurst.m_burstCurrentCount = 0;
                    sendBurst.PostFireCheck();
                }
                else if (newArch.m_archetypeData.FireMode == eWeaponFireMode.Burst)
                {
                    var recvBurst = newArch.Cast<BWA_Burst>();
                    recvBurst.m_burstCurrentCount = Math.Min(sendBurst.m_burstCurrentCount, recvBurst.m_burstMax);
                    sendBurst.m_burstCurrentCount = 0;
                }
            }

            if (oldArch.m_firing && (EndFiring || oldArch.m_archetypeData.FireMode != newArch.m_archetypeData.FireMode))
                oldArch.OnStopFiring();

            newArch.m_nextBurstTimer = oldArch.m_nextBurstTimer;
            newArch.m_nextShotTimer = oldArch.m_nextShotTimer;
            newArch.m_fireHeld = oldArch.m_fireHeld;
            newArch.m_firePressed = oldArch.m_firePressed;
            newArch.m_firing = oldArch.m_firing;
            newArch.m_readyToFire = oldArch.m_readyToFire;
        }

        private void SetLoopingAudio(bool start)
        {
            if (!CWC.GunArchetype!.m_firing) return;

            if (CWC.GunFireMode == eWeaponFireMode.Auto && !CWC.Gun!.AudioData.TriggerAutoAudioForEachShot)
            {
                if (start)
                    CWC.Gun!.TriggerAutoFireStartAudio();
                else
                    CWC.Gun!.TriggerAutoFireEndAudio();
            }

            if (CWC.GunFireMode == eWeaponFireMode.Burst && !CWC.Gun!.AudioData.TriggerBurstAudioForEachShot)
            {
                if (start)
                    CWC.Gun.TriggerBurstFireAudio();
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
            writer.WriteBoolean(nameof(EndFiring), EndFiring);
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
            base.DeserializeProperty(property, ref reader);
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
                case "endfiring":
                    EndFiring = reader.GetBoolean();
                    break;
                case "keepmagcost":
                case "keepmag":
                case "keepclipcost":
                case "keepclip":
                    KeepMagCost = reader.GetBoolean();
                    break;
                case "triggertype":
                case "trigger":
                    Trigger = TriggerCoordinator.Deserialize(ref reader);
                    break;
            }
        }
    }
}

using EWC.CustomWeapon.ComponentWrapper.OwnerComps;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
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
        ITriggerCallbackBasicSync,
        IWeaponProperty<WeaponCreatedContext>,
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

        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public override bool ShouldRegister(Type contextType)
        {
            if (Trigger == null && contextType == typeof(WeaponTriggerContext)) return false;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponTriggerContext context) => Trigger!.Invoke(context);

        public void Invoke(WeaponCreatedContext context)
        {
            _archetype?.SetOwner(CWC.Owner.Player);
            _cachedArchetype?.SetOwner(CWC.Owner.Player);

            ResetERDComponent();
            CGC.RefreshSoundDelay();
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
            if (_archBlock != null && CWC.Owner.IsType(OwnerType.Local) && _archetype == null)
            {
                var gun = (LocalGunComp)CWC.Weapon;
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
                    default:
                        throw new NotImplementedException($"DataSwap does not support FireMode {_archBlock.FireMode} for the player (should be one of the standard 4)");
                }
                _archetype!.Setup(gun.Value);
                if (_archBlock.FireMode == eWeaponFireMode.Burst)
                {
                    var burst = _archetype.Cast<BWA_Burst>();
                    burst.m_burstMax = _archBlock.BurstShotCount;
                }

                if (CWC.HasTrait<AutoTrigger>())
                    _archetype.m_triggerNeedsPress = false;

                _archetype.SetOwner(CWC.Owner.Player); // Why does this set audio delay
                ResetERDComponent();
                CGC.RefreshSoundDelay();
            }
            return _archBlock != null || _audioBlock != null;
        }

        private void ApplyData()
        {
            if (_audioBlock != null)
                SetLoopingAudio(false);

            if (_archBlock != null)
                ChangeArchetype();

            if (_audioBlock != null)
            {
                _cachedAudioBlock = CGC.Gun.AudioData;
                CGC.Gun.AudioData = _audioBlock;
                SetLoopingAudio(true);
            }
        }

        private void ClearData()
        {
            if (_cachedAudioBlock != null)
                SetLoopingAudio(false);

            if (_cachedArchBlock != null)
                ChangeArchetype();

            if (_cachedAudioBlock != null)
            {
                CGC.Gun.AudioData = _cachedAudioBlock;
                SetLoopingAudio(true);
                _cachedAudioBlock = null;
            }
        }

        private void ChangeArchetype()
        {
            ArchetypeDataBlock newBlock;
            BulletWeaponArchetype? newArch;
            BulletWeaponArchetype? oldArch;
            LocalGunComp? localGun = CGC.Gun as LocalGunComp;
            if (_cachedArchBlock != null)
            {
                newBlock = _cachedArchBlock;
                newArch = _cachedArchetype;
                oldArch = _archetype;
                _cachedArchBlock = null;
                _cachedArchetype = null;
            }
            else
            {
                newBlock = _archBlock!;
                newArch = _archetype;
                _cachedArchBlock = CGC.Gun.ArchetypeData;
                _cachedArchetype = localGun?.GunArchetype;
                oldArch = _cachedArchetype;
            }

            int clip = CGC.Gun.GetCurrentClip();
            int clipSize = CGC.Gun.GetMaxClip();

            bool hasOwner = CWC.Owner.Player != null;
            PlayerAmmoStorage? ammoStorage;
            InventorySlotAmmo? slotAmmo;
            float clipCost;
            if (hasOwner)
            {
                ammoStorage = PlayerBackpackManager.GetBackpack(CGC.Owner.Player!.Owner).AmmoStorage;
                slotAmmo = ammoStorage.GetInventorySlotAmmo(CGC.Weapon.AmmoType);
                clipCost = clip * slotAmmo.CostOfBullet;
            }
            else
            {
                ammoStorage = null;
                slotAmmo = null;
                clipCost = ((SentryGunComp)CWC.Weapon).CostOfBullet;
            }

            var oldBlock = CGC.Gun.ArchetypeData;
            CGC.Gun.ArchetypeData = newBlock;
            if (newArch != null)
            {
                CopyArchetypeVars(newArch, oldArch!);
                localGun!.GunArchetype = newArch;
            }
            CGC.RefreshArchetypeCache();
            ResetERDComponent();

            int newClipSize = CGC.Gun.GetMaxClip();
            int slotAmmoClipSize = newClipSize;
            float cost = newBlock.CostOfBullet;
            if (CWC.Weapon.IsType(WeaponType.SentryHolder))
                cost = ((SentryHolderComp)CWC.Weapon).CostOfBullet;
            else if (CWC.Owner.IsType(OwnerType.Sentry))
            {
                slotAmmoClipSize = 0;
                var sentryComp = (SentryGunComp)CWC.Weapon;
                cost = sentryComp.CostOfBullet;
            }

            if (hasOwner)
            {
                slotAmmo!.Setup(cost / EXPAPIWrapper.GetAmmoMod(CWC.Owner.IsType(OwnerType.Local)), slotAmmoClipSize);
                cost = slotAmmo.CostOfBullet;
            }

            if (KeepMagCost)
            {
                float newBuffer = clipCost;
                _costBuffer += newBuffer + 1e-10f; // Some small constant to avoid rounding errors
                int newMag = Math.Min(newClipSize, (int) (_costBuffer / cost));
                _costBuffer -= newMag * cost;
                newBuffer -= newMag * cost;

                if (hasOwner)
                    slotAmmo!.AmmoInPack += newBuffer;
                CGC.Gun.SetCurrentClip(newMag);
            }
            else
            {
                // Calculate the target magazine
                float newMagFloat = (float)clip / clipSize * newClipSize + _magBuffer;
                int newMag = Math.Min(newClipSize, (int)newMagFloat);

                // Calculate the maximum amount of ammo we can give
                float maxCost = clipCost + (hasOwner ? slotAmmo!.AmmoInPack : 0);
                if (maxCost < newMag * cost)
                    newMag = (int)(maxCost / cost);

                _magBuffer = Math.Max(0, clip - newMag * clipSize / newClipSize);
                if (hasOwner)
                    slotAmmo!.AmmoInPack += clipCost - newMag * cost;
                CGC.Gun.SetCurrentClip(newMag); 
            }

            if (newArch != null)
                newArch.m_clip = CGC.Gun.GetCurrentClip();
            if (localGun != null && GuiManager.CrosshairLayer.m_circleCrosshair.Visible)
                GuiManager.CrosshairLayer.ShowSpreadCircle(localGun.Value.RecoilData.hipFireCrosshairSizeDefault);

            if (hasOwner)
            {
                ammoStorage!.UpdateSlotAmmoUI(slotAmmo, CGC.Gun.GetCurrentClip());
                ammoStorage.NeedsSync = true;
            }
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
            if (CWC.Weapon is LocalGunComp localGun)
            {
                if (!localGun.GunArchetype.m_firing) return;

                if (localGun.FireMode == eWeaponFireMode.Auto && !localGun.AudioData.TriggerAutoAudioForEachShot)
                {
                    if (start)
                        localGun.Value.TriggerAutoFireStartAudio();
                    else
                        localGun.Value.TriggerAutoFireEndAudio();
                }

                if (localGun.FireMode == eWeaponFireMode.Burst && !localGun.AudioData.TriggerBurstAudioForEachShot)
                {
                    if (start)
                        localGun.Value.TriggerBurstFireAudio();
                }
            }
            else if (CWC.Weapon is SentryGunComp sentry)
            {
                if (!sentry.Value.m_isFiring || sentry.FireMode != eWeaponFireMode.Auto) return;

                if (start)
                    sentry.Firing.TriggerAutoFireStartAudio();
                else
                    sentry.Firing.TriggerAutoFireEndAudio();
            }
        }

        private void ResetERDComponent()
        {
            if (CWC.Owner.IsType(OwnerType.Local))
                ERDAPIWrapper.ChangeCustomRecoil(CGC.Gun.ArchetypeData.persistentID, ((IBulletWeaponComp)CWC.Weapon).BulletWeapon);
        }

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
            writer.WriteNull(nameof(Trigger));
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

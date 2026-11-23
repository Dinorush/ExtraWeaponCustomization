using EWC.CustomWeapon.ComponentWrapper.DataSwapper;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Shared.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using GameData;
using Gear;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class DataSwap : 
        Effect,
        ITriggerCallbackBasicSync,
        IWeaponProperty<WeaponCreatedContext>,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        public ushort SyncID { get; set; }

        public uint ArchetypeID { get; private set; } = 0;
        public uint AudioID { get; private set; } = 0;
        public uint Priority { get; private set; } = 0;
        public float Duration { get; private set; } = 0f;
        public bool EndBurst { get; private set; } = false;
        public bool EndCharge { get; private set; } = false;
        public bool EndFiring { get; private set; } = false;
        public bool KeepMagCost { get; private set; } = false;

        private ArchetypeDataBlock? _archBlock;
        private WeaponAudioDataBlock? _audioBlock;
        private BulletWeaponArchetype? _archetype;
        private LinkedListNode<SwapData>? _swapDataNode;
        private readonly DelayedCallback _removeCallback;

        public DataSwap()
        {
            _removeCallback = new(
                () => Duration,
                ClearData
            );
        }

        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public override bool ValidProperty()
        {
            if (!base.ValidProperty()) return false;

            return GetData();
        }

        public override bool ShouldRegister(Type contextType)
        {
            if (Trigger == null && contextType == typeof(WeaponTriggerContext)) return false;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponCreatedContext context)
        {
            _archetype?.SetOwner(CWC.Owner.Player);
            if (CWC.HasTrait<AutoTrigger>() && _archetype != null)
                _archetype.m_triggerNeedsPress = false;
            if (CWC.Owner.IsType(OwnerType.Local))
                ((LocalDataSwapper)CGC.DataSwapper).ResetERDComponent();
            CGC.RefreshSoundDelay();
        }

        public void Invoke(WeaponSetupContext context)
        {
            if (_removeCallback.Active) // In case it was previously cleared by Temp Properties
                ApplyData();
        }

        public void Invoke(WeaponClearContext context) => ClearData();

        public override void TriggerReset()
        {
            TriggerResetSync();
            TriggerManager.SendReset(this);
        }

        public void TriggerResetSync()
        {
            _removeCallback.Stop();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            TriggerApplySync();
            TriggerManager.SendInstance(this);
        }

        public void TriggerApplySync(float mod = 1f)
        {
            ApplyData();
            CWC.StartDelayedCallback(_removeCallback);
        }

        private bool GetData()
        {
            _archBlock ??= ArchetypeDataBlock.GetBlock(ArchetypeID);
            _audioBlock ??= WeaponAudioDataBlock.GetBlock(AudioID);
            if (CWC.Owner.IsType(OwnerType.Local) && _archBlock != null && _archetype == null)
            {
                var gun = (LocalGunComp)CWC.Weapon;
                _archetype = _archBlock.FireMode switch
                {
                    eWeaponFireMode.Semi => new BWA_Semi(_archBlock),
                    eWeaponFireMode.Burst => new BWA_Burst(_archBlock),
                    eWeaponFireMode.Auto => new BWA_Auto(_archBlock),
                    eWeaponFireMode.SemiBurst => new BWA_SemiBurst(_archBlock),
                    _ => null
                };
                if (_archetype == null)
                {
                    EWCLogger.Error($"DataSwap does not support FireMode {_archBlock.FireMode} for the player (should be one of the standard 4)");
                    return false;
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
                ((LocalDataSwapper)CGC.DataSwapper).ResetERDComponent();
                CGC.RefreshSoundDelay();
            }
            return _archBlock != null || _audioBlock != null;
        }

        private void ApplyData()
        {
            if (_swapDataNode != null)
                CGC.DataSwapper.AddSwap(_swapDataNode);
            else
                _swapDataNode = CGC.DataSwapper.AddSwap(this, _archBlock, _audioBlock, _archetype);
        }

        private void ClearData()
        {
            if (_swapDataNode != null)
                CGC.DataSwapper.RemoveSwap(_swapDataNode);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ArchetypeID), ArchetypeID);
            writer.WriteNumber(nameof(AudioID), AudioID);
            writer.WriteNumber(nameof(Priority), Priority);
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

        public override bool DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            if (base.DeserializeProperty(property, ref reader))
                return true;

            switch (property)
            {
                case "archetypeid":
                case "archetype":
                case "archid":
                case "arch":
                    ArchetypeID = EWCJson.Deserialize<uint>(ref reader);
                    break;
                case "weaponaudioid":
                case "weaponaudio":
                case "audioid":
                case "audio":
                    AudioID = EWCJson.Deserialize<uint>(ref reader);
                    break;
                case "priority":
                    Priority = reader.GetUInt32();
                    break;
                case "duration":
                    Duration = reader.GetSingle();
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
                default:
                    return false;
            }
            return true;
        }
    }
}

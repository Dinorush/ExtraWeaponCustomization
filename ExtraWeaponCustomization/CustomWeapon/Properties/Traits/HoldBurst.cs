using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Player;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class HoldBurst :
        Trait,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponFireCancelContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponSprintContext>,
        IWeaponProperty<WeaponPreSprintContext>,
        IWeaponProperty<WeaponPreSwapContext>,
        IWeaponProperty<WeaponPostStopFiringContext>
    {
        public int ShotsUntilCancel { get; private set; } = 1;
        public bool RequireHold { get; private set; } = true;
        public bool CancelConsumeClip { get; private set; } = false;

        private int _burstMaxCount = 0;
        private int _burstCurrentCount = 0;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public override bool ShouldRegister(Type contextType)
        {
            if (!CancelConsumeClip)
            { 
                if (contextType == typeof(WeaponPostStopFiringContext)) return false;
                if (contextType == typeof(WeaponPostFireContext)) return false;
            }
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponPostStartFireContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;
            // Can't use archetype.m_burstMax in case clip < max burst count
            _burstMaxCount = arch.m_burstCurrentCount;
            _burstCurrentCount = 0;
        }

        public void Invoke(WeaponFireCancelContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;
            if (_burstMaxCount - arch.m_burstCurrentCount < ShotsUntilCancel) return;
            
            if (!arch.m_fireHeld && (RequireHold || !CanShoot()))
            {
                ConsumeFromClip();
                arch.m_burstCurrentCount = 0;
                _burstCurrentCount = 0;
                context.Allow = false;
            }
        }
        
        public void Invoke(WeaponPostFireContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;

            _burstCurrentCount = arch.m_burstCurrentCount;
        }

        public void Invoke(WeaponPostStopFiringContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;

            ConsumeFromClip();
        }

        private void ConsumeFromClip()
        {
            if (_burstCurrentCount == 0) return;

            int currClip = CGC.Gun.GetCurrentClip();
            int bullets = Math.Min(_burstCurrentCount, currClip);
            var slotAmmo = PlayerBackpackManager.LocalBackpack.AmmoStorage.GetInventorySlotAmmo(CGC.Gun.AmmoType);
            slotAmmo.AmmoInPack += bullets * slotAmmo.CostOfBullet;
            CGC.Gun.SetCurrentClip(currClip - bullets);
        }

        public void Invoke(WeaponSprintContext context)
        {
            if (!CGC.Owner.Player!.Locomotion.IsRunning || !((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;

            if (arch.m_firing)
            {
                arch.m_burstCurrentCount = 0;
                arch.PostFireCheck();
            }
        }

        public void Invoke(WeaponPreSprintContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;

            context.AllowBurstCancel = context.AllowBurstCancel || _burstMaxCount - arch.m_burstCurrentCount >= ShotsUntilCancel;
        }

        public void Invoke(WeaponPreSwapContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;

            context.AllowBurstCancel = context.AllowBurstCancel || _burstMaxCount - arch.m_burstCurrentCount >= ShotsUntilCancel;
        }

        private bool CanShoot()
        {
            if (CGC.Gun.Component.FPItemHolder.ItemIsBusy) return false;
            var locomotion = CGC.Owner.Player!.Locomotion;
            return !locomotion.IsRunning && !locomotion.IsInAir;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ShotsUntilCancel), ShotsUntilCancel);
            writer.WriteBoolean(nameof(RequireHold), RequireHold);
            writer.WriteBoolean(nameof(CancelConsumeClip), CancelConsumeClip);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "shotsuntilcancel":
                case "shots":
                    ShotsUntilCancel = reader.GetInt32();
                    break;
                case "requirehold":
                case "hold":
                    RequireHold = reader.GetBoolean();
                    break;
                case "cancelconsumeclip":
                case "cancelconsumemag":
                    CancelConsumeClip = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}

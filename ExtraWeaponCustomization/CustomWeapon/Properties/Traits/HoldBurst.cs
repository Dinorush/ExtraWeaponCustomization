using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class HoldBurst :
        Trait,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponFireCancelContext>,
        IWeaponProperty<WeaponSprintContext>,
        IWeaponProperty<WeaponPreSprintContext>,
        IWeaponProperty<WeaponPreSwapContext>
    {
        public int ShotsUntilCancel { get; private set; } = 1;
        public bool RequireHold { get; private set; } = true;

        private int _burstMaxCount = 0;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public void Invoke(WeaponPostStartFireContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;
            // Can't use archetype.m_burstMax in case clip < max burst count
            _burstMaxCount = arch.m_burstCurrentCount;
        }

        public void Invoke(WeaponFireCancelContext context)
        {
            if (!((LocalGunComp)CGC.Gun).TryGetBurstArchetype(out var arch)) return;
            if (_burstMaxCount - arch.m_burstCurrentCount < ShotsUntilCancel) return;
            
            if (!arch.m_fireHeld && (RequireHold || !CanShoot()))
            {
                arch.m_burstCurrentCount = 0;
                context.Allow = false;
            }
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
                default:
                    break;
            }
        }
    }
}

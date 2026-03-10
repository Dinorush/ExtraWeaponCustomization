using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using Gear;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ForceFire :
        Effect
    {
        public WeaponState RequiredState { get; private set; } = WeaponState.None;

        protected override WeaponType RequiredWeaponType => WeaponType.BulletWeapon;
        protected override OwnerType RequiredOwnerType => OwnerType.Local;

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            BulletWeaponArchetype bwa = ((LocalGunComp)CWC.Weapon).GunArchetype;
            var fpItemHolder = CWC.Owner.Player!.FPItemHolder;
            bool held = fpItemHolder.WieldedItem?.Pointer == CWC.Weapon.Component.Pointer;
            if (!held && RequiredState.HasFlag(WeaponState.Held)) return;

            if ((held && fpItemHolder.ItemIsBusy) || bwa.m_clip <= 0f) return;

            if (RequiredState.HasFlag(WeaponState.Ready))
            {
                if (bwa.m_firing) return;
                var time = Clock.Time;
                if (time < bwa.m_nextShotTimer || time < bwa.m_nextBurstTimer) return;
            }

            if (bwa.m_firing)
            {
                bwa.OnFireShot();
                bwa.PostFireCheck();
            }
            else
            {
                if (bwa.m_inChargeup)
                {
                    bwa.m_inChargeup = false;
                    GuiManager.CrosshairLayer.SetChargeUpVisibleAndProgress(visible: false);
                }

                bwa.m_readyToFire = true;
                bwa.OnStartFiring();
                bwa.OnFireShot();
                bwa.PostFireCheck();
                bwa.m_clip = bwa.m_weapon.GetCurrentClip();
            }
        }

        public override void TriggerReset() { }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteString(nameof(RequiredState), RequiredState.ToString());
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "requiredstate":
                case "required":
                    RequiredState = ToWeaponState(reader.GetString()!);
                    break;
            }
        }

        private static WeaponState ToWeaponState(string value)
        {
            value = value.Replace(" ", null).ToLower();
            WeaponState result = WeaponState.None;
            if (value == "all")
                return WeaponState.All;
            if (value.Contains("ready"))
                result |= WeaponState.Ready;
            if (value.Contains("held"))
                result |= WeaponState.Held;
            return result;
        }
    }

    [Flags]
    public enum WeaponState
    {
        None,
        Ready = 1,
        Held = 1 << 1,
        All = -1
    }
}

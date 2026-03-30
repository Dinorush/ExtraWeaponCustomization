using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.TriggerModifier;
using EWC.CustomWeapon.Properties.Shared.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public class ChargeSpeedMod :
        TriggerMod,
        IWeaponProperty<WeaponChargeSpeedContext>
    {
        public bool ForceUpdate { get; private set; } = false;

        private readonly TriggerStack _triggerStack;

        public ChargeSpeedMod() => _triggerStack = new(this);

        protected override WeaponType RequiredWeaponType => WeaponType.BulletWeapon;
        protected override OwnerType RequiredOwnerType => OwnerType.Local;

        public override bool TryGetStacks(out float stacks, BaseDamageableWrapper? _ = null) => _triggerStack.TryGetStacks(out stacks);

        public override void TriggerReset()
        {
            _triggerStack.Clear();

            CGC.UpdateChargeTime(ForceUpdate);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            var num = Count(contexts);
            _triggerStack.Add(num);

            CGC.UpdateChargeTime(ForceUpdate);
        }

        public void Invoke(WeaponChargeSpeedContext context)
        {
            if (_triggerStack.TryGetMod(out float mod))
                context.AddMod(mod, StackLayer);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            writer.WriteBoolean(nameof(ForceUpdate), ForceUpdate);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "forceupdate":
                    ForceUpdate = reader.GetBoolean();
                    break;
            }
        }
    }
}

using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ArmorMod :
        TriggerMod,
        ITriggerCallbackBasicSync,
        IWeaponProperty<WeaponPlayerArmorContext>
    {
        public ushort SyncID { get; set; }

        public PlayerDamageType[] DamageType { get; private set; } = PlayerDamageTypeConst.Any;
        public bool Immune { get; private set; } = false;

        private readonly TriggerStack _triggerStack;

        protected override OwnerType RequiredOwnerType => OwnerType.Player;

        public ArmorMod() => _triggerStack = new(this);

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            var num = Count(contexts);
            TriggerApplySync(num);
            TriggerManager.SendInstance(this, num);
        }

        public void TriggerApplySync(float num)
        {
            _triggerStack.Add(num);
        }

        public override void TriggerReset()
        {
            TriggerResetSync();
            TriggerManager.SendReset(this);
        }

        public void TriggerResetSync()
        {
            _triggerStack.Clear();
        }

        public override bool TryGetStacks(out float stacks, BaseDamageableWrapper? _ = null) => _triggerStack.TryGetStacks(out stacks);

        public void Invoke(WeaponPlayerArmorContext context)
        {
            if (!context.DamageType.HasFlagIn(DamageType)) return;

            if (Immune)
            {
                context.Immune = true;
                return;
            }
            
            if (!_triggerStack.TryGetMod(out float mod)) return;

            context.AddMod(mod, StackLayer);
        }
        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteBoolean(nameof(Immune), Immune);
            writer.WriteString(nameof(DamageType), DamageType[0].ToString());
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "moddamagetype":
                case "damagetype":
                    DamageType = reader.GetString().ToPlayerDamageTypes();
                    break;
                case "immune":
                case "immunity":
                    Immune = reader.GetBoolean();
                    break;
            }
        }
    }
}

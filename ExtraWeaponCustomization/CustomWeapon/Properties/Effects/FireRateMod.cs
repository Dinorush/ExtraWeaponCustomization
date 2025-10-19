using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public class FireRateMod :
        TriggerMod,
        ITriggerCallbackBasicSync,
        IWeaponProperty<WeaponFireRateContext>
    {
        public ushort SyncID { get; set; }

        public bool ForceUpdate { get; private set; } = false;

        private readonly TriggerStack _triggerStack;

        public FireRateMod() => _triggerStack = new(this);

        public override void TriggerReset()
        {
            _triggerStack.Clear();

            TriggerManager.SendReset(this);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            var num = Sum(contexts);
            TriggerApplySync(num);

            if (CWC.Weapon.IsType(Enums.WeaponType.Gun))
                TriggerManager.SendInstance(this, num);
        }

        public void TriggerResetSync()
        {
            _triggerStack.Clear();

            if (ForceUpdate && CWC.Weapon.IsType(Enums.WeaponType.Gun))
            {
                CGC.UpdateStoredFireRate();
                CGC.ModifyFireRate();
            }
        }

        public void TriggerApplySync(float num)
        {
            _triggerStack.Add(num);

            if (ForceUpdate && CWC.Weapon.IsType(Enums.WeaponType.Gun))
            {
                CGC.UpdateStoredFireRate();
                CGC.ModifyFireRate();
            }
        }

        public void Invoke(WeaponFireRateContext context)
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

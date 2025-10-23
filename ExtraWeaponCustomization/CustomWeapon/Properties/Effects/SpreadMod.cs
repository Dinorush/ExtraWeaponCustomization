using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpreadMod :
        TriggerModTimed
    {
        public bool UpdateCrosshair { get; private set; } = true;

        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        protected override void OnUpdate(float mod) => CGC.SpreadController.SetMod(this, mod, UpdateCrosshair);
        protected override void OnDisable() => CGC.SpreadController.ClearMod(this, UpdateCrosshair);

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
            writer.WriteBoolean(nameof(UpdateCrosshair), UpdateCrosshair);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "updatecrosshair":
                    UpdateCrosshair = reader.GetBoolean();
                    break;
            }
        }
    }
}

using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class HitmarkerCooldown : 
        Trait,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponHitmarkerContext>
    {
        public float Cooldown { get; private set; } = 0f;

        private float _lastHitmarkerTime = 0f;

        public void Invoke(WeaponHitmarkerContext context)
        {
            float time = Clock.Time;
            if (time - _lastHitmarkerTime > Cooldown)
                _lastHitmarkerTime = time;
            else
                context.Result = false;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Cooldown), Cooldown);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "cooldown":
                    Cooldown = reader.GetSingle();
                    break;
            }
        }
    }
}

using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class ThickBullet : 
        Trait,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        public float HitSize { get; private set; } = 0f;
        public float HitSizeFriendly { get; private set; } = 0f;

        protected override OwnerType RequiredOwnerType => OwnerType.Managed;

        public void Invoke(WeaponSetupContext context)
        {
            CWC.ShotComponent.ThickBullet = this;
        }

        public void Invoke(WeaponClearContext context)
        {
            CWC.ShotComponent.ThickBullet = null;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HitSize), HitSize);
            writer.WriteNumber(nameof(HitSizeFriendly), HitSizeFriendly);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property.ToLowerInvariant())
            {
                case "hitsize":
                case "size":
                    HitSize = reader.GetSingle();
                    break;
                case "hitsizefriendly":
                case "sizefriendly":
                    HitSizeFriendly = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}

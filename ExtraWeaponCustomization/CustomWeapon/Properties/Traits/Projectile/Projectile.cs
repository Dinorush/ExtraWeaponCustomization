using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class Projectile :
        Trait,
        IWeaponProperty<WeaponPostSetupContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponPostRayContext>
    {
        public float Speed { get; set; } = 0f;
        public float Gravity { get; set; } = 1f;
        public float Size { get; set; } = 0f;
        public float SizeWorld { get; set; } = 0f;


        private float _weaponRayDist = 100f;

        public void Invoke(WeaponPostSetupContext context)
        {
            _weaponRayDist = context.Weapon.MaxRayDist;
            context.Weapon.MaxRayDist = 0f;
        }

        public void Invoke(WeaponClearContext context)
        {
            context.Weapon.MaxRayDist = _weaponRayDist;
        }

        public void Invoke(WeaponPostRayContext context)
        {
            // Fire projectile here
        }

        public override IWeaponProperty Clone()
        {
            Projectile copy = new()
            {
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (property.ToLowerInvariant())
            {

            }
        }
    }
}

using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using UnityEngine;
using System.Text.Json;
using ExtraWeaponCustomization.Utils;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class ThickBullet : 
        Trait,
        IWeaponProperty<WeaponPostRayContext>
    {
        public float HitSize { get; set; } = 0f;

        private static RaycastHit s_rayHit;

        public void Invoke(WeaponPostRayContext context)
        {
            if (HitSize == 0) return;

            float dist = context.Result ? context.Data.rayHit.distance : context.Data.maxRayDist;
            bool newResult = Physics.SphereCast(Weapon.s_ray, HitSize, out s_rayHit, dist, EWCProjectileManager.MaskEntity);
            if (!newResult) return;
            
            if (context.Result)
            {
                // If the shot direct hit the same enemy the thick shot did, use the original ray cast instead
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(s_rayHit);
                IDamageable? oldDamageable = DamageableUtil.GetDamageableFromData(context.Data);
                if (damageable == oldDamageable || damageable?.GetBaseAgent() == oldDamageable?.GetBaseAgent())
                    return;
            }

            context.Result = true;
            context.Data.rayHit = s_rayHit;
        }

        public override IWeaponProperty Clone()
        {
            ThickBullet copy = new()
            {
                HitSize = HitSize
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HitSize), HitSize);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (property.ToLowerInvariant())
            {
                case "hitsize":
                case "size":
                    HitSize = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}

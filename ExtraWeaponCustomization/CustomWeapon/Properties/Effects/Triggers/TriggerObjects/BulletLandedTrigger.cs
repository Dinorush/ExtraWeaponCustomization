using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class BulletLandedTrigger : DamageTypeTrigger<WeaponHitContextBase>
    {
        public bool TerrainOnly { get; set; } = false;
        public BulletLandedTrigger() : base(TriggerName.BulletLanded, DamageType.Bullet) {}

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            // Want to trigger when a bullet lands but NOT on a pre-hit context.
            if (base.Invoke(context, out amount))
            {
                if ((TerrainOnly && HitTerrain(context))
                 || (!TerrainOnly && context is not WeaponPreHitDamageableContext))
                    return true;
            }
            amount = 0;
            return false;
        }

        private bool HitTerrain(WeaponTriggerContext context)
        {
            var baseContext = (WeaponHitContextBase)context;
            if (!baseContext.DamageType.HasFlag(DamageType.Terrain)) return false;
            var hitContext = (WeaponHitContext)context;
            return !hitContext.HitCorpse;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "terrainonly":
                case "terrain":
                    TerrainOnly = reader.GetBoolean();
                    break;
            }
        }
    }
}

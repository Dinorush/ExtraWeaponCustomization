using Enemies;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    internal class BioPing :
        Trait,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponPreHitEnemyContext>
    {
        public DamageType DamageType { get; private set; } = DamageType.Bullet;

        public void Invoke(WeaponPreHitEnemyContext context)
        {
            if (context.DamageType.HasFlag(DamageType))
                ToolSyncManager.WantToTagEnemy(context.Damageable.GetBaseAgent().Cast<EnemyAgent>());
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteString(nameof(DamageType), DamageType.ToString());
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "damagetype":
                case "type":
                    DamageType = IDamageTypeTrigger.ResolveDamageType(reader.GetString());
                    break;
            }
        }
    }
}

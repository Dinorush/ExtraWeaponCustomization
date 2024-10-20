using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    internal class BioPing :
        Trait,
        IWeaponProperty<WeaponPreHitEnemyContext>
    {
        public void Invoke(WeaponPreHitEnemyContext context)
        {
            EnemyAgent enemy = context.Damageable.GetBaseAgent().Cast<EnemyAgent>();
            ToolSyncManager.WantToTagEnemy(enemy);
        }

        public override IWeaponProperty Clone()
        {
            return new BioPing();
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader) {}
    }
}

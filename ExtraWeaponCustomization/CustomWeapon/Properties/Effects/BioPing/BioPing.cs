using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Log;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    internal class BioPing :
        Effect,
        IGunProperty,
        IMeleeProperty
    {
        public float CooldownPerTarget { get; private set; } = 0f;

        private readonly Dictionary<ObjectWrapper<EnemyAgent>, float> _cooldownTimes = new();
        private static ObjectWrapper<EnemyAgent> TempWrapper => ObjectWrapper<EnemyAgent>.SharedInstance;

        public BioPing()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(TriggerName.Hit, TriggerName.Damage, TriggerName.Charge);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            float nextTime = Clock.Time + CooldownPerTarget;
            foreach (var context in contexts)
            {
                IDamageable? damageable = ((WeaponPreHitEnemyContext)context.context).Damageable;
                if (damageable == null) continue;

                TempWrapper.SetObject(damageable.GetBaseAgent().Cast<EnemyAgent>());
                if (!_cooldownTimes.TryGetValue(TempWrapper, out var expireTime))
                {
                    // Clean dead agents from dict
                    _cooldownTimes.Keys
                        .Where(wrapper => wrapper.Object == null || !wrapper.Object.Alive)
                        .ToList()
                        .ForEach(wrapper => _cooldownTimes.Remove(wrapper));

                    _cooldownTimes.Add(new ObjectWrapper<EnemyAgent>(TempWrapper), nextTime);
                    ToolSyncManager.WantToTagEnemy(TempWrapper.Object);
                }
                else if (Clock.Time > expireTime)
                {
                    _cooldownTimes[TempWrapper] = nextTime;
                    ToolSyncManager.WantToTagEnemy(TempWrapper.Object);
                }
            }
        }

        public override void TriggerReset()
        {
            _cooldownTimes.Clear();
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(CooldownPerTarget), CooldownPerTarget);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "damagetype":
                case "type":
                    EWCLogger.Warning("DamageType field is deprecated on BioPing. Please specify in the Trigger field.");
                    Trigger?.Activate[0].DeserializeProperty(property, ref reader);
                    break;
                case "cooldownpertarget":
                    CooldownPerTarget = reader.GetSingle();
                    break;
            }
        }
    }
}

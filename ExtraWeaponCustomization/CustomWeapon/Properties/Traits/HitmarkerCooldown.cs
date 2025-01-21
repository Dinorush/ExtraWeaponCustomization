using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Linq;
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
        public float CooldownPerTarget { get; private set; } = 0f;

        private float _lastHitmarkerTime = 0f;
        private readonly Dictionary<ObjectWrapper<EnemyAgent>, float> _cooldownsPerTarget = new();
        private static ObjectWrapper<EnemyAgent> TempWrapper => ObjectWrapper<EnemyAgent>.SharedInstance;

        public void Invoke(WeaponHitmarkerContext context)
        {
            float time = Clock.Time;
            bool showHitmarker = true;

            if (time - _lastHitmarkerTime >= Cooldown)
            {
                TempWrapper.Set(context.Enemy);
                if (!_cooldownsPerTarget.TryGetValue(TempWrapper, out float hitTime))
                {
                    _cooldownsPerTarget.Keys
                        .Where(wrapper => wrapper.Object == null || !wrapper.Object.Alive)
                        .ToList()
                        .ForEach(wrapper => _cooldownsPerTarget.Remove(wrapper));

                    _cooldownsPerTarget.Add(new ObjectWrapper<EnemyAgent>(TempWrapper), 0);
                }
                else if (time - hitTime < CooldownPerTarget)
                    showHitmarker = false;
            }
            else
                showHitmarker = false;

            if (showHitmarker)
            {
                _lastHitmarkerTime = time;
                if (CooldownPerTarget > 0f)
                    _cooldownsPerTarget[TempWrapper] = time;
            }
            else
                context.Result = false;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Cooldown), Cooldown);
            writer.WriteNumber(nameof(CooldownPerTarget), CooldownPerTarget);
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
                case "cooldownpertarget":
                    CooldownPerTarget = reader.GetSingle();
                    break;
            }
        }
    }
}

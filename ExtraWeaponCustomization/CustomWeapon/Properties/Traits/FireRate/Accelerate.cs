using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public class Accelerate :
        IWeaponProperty<WeaponFireRateSetContext>,
        IWeaponProperty<WeaponPostStopFiringContext>,
        IWeaponProperty<WeaponTriggerContext>,
        IWeaponProperty<WeaponDamageContext>
    {
        public readonly static string Name = typeof(Accelerate).Name;
        public bool AllowStack { get; } = false;

        private float _endShotDelay = 1f;
        public float EndShotDelay
        {
            get { return _endShotDelay; }
            set
            {
                _endShotDelay = Math.Max(CustomWeaponData.MinShotDelay, value);
                _endFireRate = 1f / _endShotDelay;
            }
        }
        private float _endFireRate = 1f;
        public float EndFireRate
        {
            get { return _endFireRate; }
            set { _endFireRate = Math.Max(0.001f, value); }
        }
        private float _accelTime = 1f;
        public float EndDamageMod { get; set; } = 1f;
        public float AccelTime
        {
            get { return _accelTime; }
            set { _accelTime = Math.Max(0.001f, value); }
        }
        private float _decelTime = 0.001f;
        public float DecelTime
        {
            get { return _decelTime; }
            set { _decelTime = Math.Max(0.001f, value); }
        }
        public float DecelDelay { get; set; } = 0f;
        public float AccelExponent { get; set; } = 1f;
        public TriggerType ResetTriggerType { get; set; } = TriggerType.Invalid;

        private float _progress = 0f;
        private float _lastUpdateTime = 0f;
        private bool _firing = false;

        public void Invoke(WeaponFireRateSetContext context)
        {
            if (_lastUpdateTime == 0f) _lastUpdateTime = Clock.Time;

            // Update acceleration progress
            if (_firing)
                _progress = Math.Min(_progress + (Clock.Time - _lastUpdateTime) / AccelTime, 1f);
            else if(Clock.Time - _lastUpdateTime > DecelDelay)
                _progress = Math.Max(_progress - (Clock.Time - _lastUpdateTime) / DecelTime, 0f);

            _lastUpdateTime = Clock.Time;
            _firing = context.Weapon.GetCurrentClip() > 0;

            // Apply accelerated fire rate
            float startFireRate = 1f/Math.Max(CustomWeaponData.MinShotDelay, context.Weapon.m_archeType.ShotDelay());
            context.FireRate = CalculateCurrentFireRate(startFireRate);
        }

        public void Invoke(WeaponPostStopFiringContext context)
        {
            _lastUpdateTime = Clock.Time;
            _firing = false;
        }

        public void Invoke(WeaponTriggerContext context)
        {
            if (context.Type != ResetTriggerType) return;

            // Reset acceleration
            _progress = 0;
            _firing = false;
            _lastUpdateTime = Clock.Time;
        }

        // FireRateSet context is called first, so we know progress is up to date
        public void Invoke(WeaponDamageContext context)
        {
            if (EndDamageMod == 1f) return;
            context.Damage *= CalculateCurrentDamageMod();
        }

        private float CalculateCurrentFireRate(float startFireRate)
        {
            return UnityEngine.Mathf.Lerp(startFireRate, EndFireRate, (float) Math.Pow(_progress, AccelExponent));
        }

        private float CalculateCurrentDamageMod()
        {
            return UnityEngine.Mathf.Lerp(1f, EndDamageMod, (float)Math.Pow(_progress, AccelExponent));
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(EndShotDelay), EndShotDelay);
            writer.WriteNumber(nameof(EndFireRate), EndFireRate);
            writer.WriteNumber(nameof(EndDamageMod), EndDamageMod);
            writer.WriteNumber(nameof(AccelTime), AccelTime);
            writer.WriteNumber(nameof(AccelExponent), AccelExponent);
            writer.WriteNumber(nameof(DecelTime), DecelTime);
            writer.WriteNumber(nameof(DecelDelay), DecelDelay);
            writer.WriteString(nameof(ResetTriggerType), ResetTriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "endshotdelay":
                case "accelshotdelay":
                    EndShotDelay = reader.GetSingle();
                    break;
                case "endfirerate":
                case "accelfirerate":
                    EndFireRate = reader.GetSingle();
                    break;
                case "enddamagemod":
                case "acceldamagemod":
                    EndDamageMod = reader.GetSingle();
                    break;
                case "acceltime":
                    AccelTime = reader.GetSingle();
                    break;
                case "accelexponent":
                case "exponent":
                    AccelExponent = reader.GetSingle();
                    break;
                case "deceltime":
                    DecelTime = reader.GetSingle();
                    break;
                case "deceldelay":
                    DecelDelay = reader.GetSingle();
                    break;
                case "resettriggertype":
                case "resettrigger":
                    ResetTriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    break;
                default:
                    break;
            }
        }
    }
}

using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.JSON;
using ExtraWeaponCustomization.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public class Accelerate :
        Trait,
        ITriggerCallback,
        IGunProperty,
        IWeaponProperty<WeaponPreStartFireContext>,
        IWeaponProperty<WeaponFireRateSetContext>,
        IWeaponProperty<WeaponCancelFireContext>,
        IWeaponProperty<WeaponDamageContext>,
        IWeaponProperty<WeaponPreFireContextSync>
    {
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
        public float EndDamageMod { get; set; } = 1f;
        public StackType DamageStackLayer { get; set; } = StackType.Multiply;
        private float _accelTime = 1f;
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
        private bool _useContinuousGrowth = false;

        private TriggerCoordinator? _coordinator;
        public TriggerCoordinator? Trigger
        {
            get => _coordinator;
            set
            {
                _coordinator = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        private float _progress = 0f;
        private float _lastUpdateTime = 0f;

        private float _resetProgress = 0f;
        private float _resetUpdateTime = 0f;
        private bool _calculateGrowthFactor = true;

        private void UpdateProgress()
        {
            _resetProgress = _progress;
            _resetUpdateTime = _lastUpdateTime;

            CWC.Weapon.GetComponent<CustomWeaponComponent>();

            if (_lastUpdateTime == 0f)
                _lastUpdateTime = Clock.Time;

            float delta = Clock.Time - _lastUpdateTime;
            float accelTime = Math.Min(1f / CWC.CurrentFireRate, delta);

            _progress = Math.Min(_progress + accelTime / AccelTime, 1f);

            delta -= accelTime + Clock.Delta;

            if (delta > 0 && delta > DecelDelay)
                _progress = Math.Max(_progress - (delta - DecelDelay) / DecelTime, 0f);

            _lastUpdateTime = Clock.Time;
        }

        public void Invoke(WeaponPreStartFireContext context)
        {
            UpdateProgress();   
        }

        // Runs on every shot fired. Don't need to do a full UpdateProgress since we know the player is
        // holding down the trigger if there are consecutive calls to this without UpdateProgress.
        public void Invoke(WeaponFireRateSetContext context)
        {
            // Update acceleration progress
            _progress = Math.Min(_progress + (Clock.Time - _lastUpdateTime) / AccelTime, 1f);

            _lastUpdateTime = Clock.Time;

            // Apply accelerated fire rate
            float startFireRate = 1f / Math.Max(CustomWeaponData.MinShotDelay, CWC.Gun!.m_archeType.ShotDelay());
            context.FireRate = CalculateCurrentFireRate(startFireRate);
        }

        public void Invoke(WeaponCancelFireContext context)
        {
            _progress = _resetProgress;
            _lastUpdateTime = _resetUpdateTime;
        }

        public void Invoke(WeaponPreFireContextSync context)
        {
            UpdateProgress();
        }

        public void Invoke(WeaponTriggerContext context) => Trigger?.Invoke(context);

        public void TriggerReset()
        {
            // Reset acceleration
            _progress = 0;
            _lastUpdateTime = Clock.Time;
        }

        public void TriggerApply(List<TriggerContext> contexts)
        {
            // Reset acceleration
            _progress = 0;
            _lastUpdateTime = Clock.Time;
        }

        // FireRateSet context is called first, so we know progress is up to date
        public void Invoke(WeaponDamageContext context)
        {
            if (EndDamageMod == 1f) return;
            context.Damage.AddMod(CalculateCurrentDamageMod(), DamageStackLayer);
        }

        private float CalculateCurrentFireRate(float startFireRate)
        {
            if (_useContinuousGrowth)
            {
                CalculateGrowthFactor(startFireRate);
                float fireRate = (float) Math.Exp(AccelExponent * _progress) * startFireRate;
                return startFireRate < EndFireRate ? Math.Min(fireRate, EndFireRate) : Math.Max(EndFireRate, fireRate);
            }
            return UnityEngine.Mathf.Lerp(startFireRate, EndFireRate, (float) Math.Pow(_progress, AccelExponent));
        }

        private float CalculateCurrentDamageMod()
        {
            return UnityEngine.Mathf.Lerp(1f, EndDamageMod, (float)Math.Pow(_progress, AccelExponent));
        }

        public override IWeaponProperty Clone()
        {
            Accelerate copy = new()
            {
                EndFireRate = EndFireRate,
                EndDamageMod = EndDamageMod,
                DamageStackLayer = DamageStackLayer,
                AccelTime = AccelTime,
                AccelExponent = AccelExponent,
                _useContinuousGrowth = _useContinuousGrowth,
                DecelTime = DecelTime,
                DecelDelay = DecelDelay,
                Trigger = Trigger?.Clone()
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(EndShotDelay), EndShotDelay);
            writer.WriteNumber(nameof(EndFireRate), EndFireRate);
            writer.WriteNumber(nameof(EndDamageMod), EndDamageMod);
            writer.WriteString(nameof(DamageStackLayer), DamageStackLayer.ToString());
            writer.WriteNumber(nameof(AccelTime), AccelTime);
            if (_useContinuousGrowth)
                writer.WriteString(nameof(AccelExponent), "e");
            else
                writer.WriteNumber(nameof(AccelExponent), AccelExponent);
            writer.WriteNumber(nameof(DecelTime), DecelTime);
            writer.WriteNumber(nameof(DecelDelay), DecelDelay);
            writer.WriteString("ResetTrigger", "Invalid");
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
                case "damagestacklayer":
                case "stacklayer":
                case "layer":
                    DamageStackLayer = reader.GetString().ToEnum(StackType.Invalid);
                    break;
                case "acceltime":
                    AccelTime = reader.GetSingle();
                    break;
                case "accelexponent":
                case "exponent":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        string check = reader.GetString()!.ToLowerInvariant();
                        if (check == "e")
                            _useContinuousGrowth = true;
                    }
                    else
                    {
                        AccelExponent = reader.GetSingle();
                        _useContinuousGrowth = false;
                    }
                    break;
                case "deceltime":
                    DecelTime = reader.GetSingle();
                    break;
                case "deceldelay":
                    DecelDelay = reader.GetSingle();
                    break;
                case "resettriggertype":
                case "resettrigger":
                    Trigger = EWCJson.Deserialize<TriggerCoordinator>(ref reader);
                    break;
                default:
                    break;
            }
        }

        private void CalculateGrowthFactor(float startFireRate)
        {
            if (!_calculateGrowthFactor) return;
            _calculateGrowthFactor = false;
            AccelExponent = (float) Math.Log(EndFireRate / startFireRate);
        }
    }
}

using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class Accelerate :
        Effect,
        IGunProperty,
        ITriggerCallbackBasicSync,
        IWeaponProperty<WeaponPreStartFireContext>,
        IWeaponProperty<WeaponPreFireContextSync>,
        IWeaponProperty<WeaponFireCanceledContext>,
        IWeaponProperty<WeaponFireRateContext>,
        IWeaponProperty<WeaponShotInitContext>,
        IWeaponProperty<WeaponShotGroupInitContext>
    {
        public ushort SyncID { get; set; }

        public float EndFireRate { get; private set; } = 0f;
        private float _endFireRateMod = 1f;
        public float EndFireRateMod
        {
            get { return _endFireRateMod; }
            private set { _endFireRateMod = Math.Max(0.001f, value); }
        }
        public StackType FireRateStackLayer { get; private set; } = StackType.Multiply;
        private float FireRateMod => EndFireRate > 0f ? EndFireRate / CWC.BaseFireRate : EndFireRateMod;

        public float EndShotMod { get; private set; } = 1f;
        public DamageType[] ModDamageType { get; private set; } = DamageTypeConst.Any;
        public StatType ModStatType { get; private set; } = StatType.Damage;
        public StackType ModStackLayer { get; private set; } = StackType.Multiply;
        public bool StoreModOnGroup { get; private set; } = true;

        private float _accelTime = 1f;
        public float AccelTime
        {
            get { return _accelTime; }
            private set { _accelTime = Math.Max(0.001f, value); }
        }
        private float _decelTime = 0.001f;
        public float DecelTime
        {
            get { return _decelTime; }
            private set { _decelTime = Math.Max(0.001f, value); }
        }
        public float DecelDelay { get; private set; } = 0f;
        public float AccelExponent { get; private set; } = 1f;
        public bool UseContinuousGrowth { get; private set; } = false;

        private float _progress = 0f;
        private float _lastUpdateTime = 0f;

        private float _resetProgress = 0f;
        private float _resetUpdateTime = 0f;

        public override bool ShouldRegister(Type contextType)
        {
            bool modifiesShot = EndShotMod != 1f || ModStackLayer == StackType.Override;
            if (contextType == typeof(WeaponShotInitContext)) return modifiesShot && !StoreModOnGroup;
            if (contextType == typeof(WeaponShotGroupInitContext)) return modifiesShot && StoreModOnGroup;
            return base.ShouldRegister(contextType);
        }

        private void UpdateProgress()
        {
            _resetProgress = _progress;
            _resetUpdateTime = _lastUpdateTime;

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

        public void Invoke(WeaponPreFireContextSync context)
        {
            UpdateProgress();
        }

        // Runs on every shot fired. Don't need to do a full UpdateProgress since we know the player is
        // holding down the trigger if there are consecutive calls to this without UpdateProgress.
        public void Invoke(WeaponFireRateContext context)
        {
            // Update acceleration progress
            _progress = Math.Min(_progress + (Clock.Time - _lastUpdateTime) / AccelTime, 1f);

            _lastUpdateTime = Clock.Time;

            // Apply accelerated fire rate
            if (FireRateMod != 1f && _progress > 0)
                context.AddMod(GetMod(FireRateMod), FireRateStackLayer);
        }

        public void Invoke(WeaponShotInitContext context)
        {
            if (_progress > 0)
                context.Mod.Add(this, ModStatType, GetMod(EndShotMod), 0f, StackType.Override, ModStackLayer, null, ModDamageType);
        }

        public void Invoke(WeaponShotGroupInitContext context)
        {
            if (_progress > 0)
                context.GroupMod.Add(this, ModStatType, GetMod(EndShotMod), 0f, StackType.Override, ModStackLayer, null, ModDamageType);
        }

        public void Invoke(WeaponFireCanceledContext context)
        {
            _progress = _resetProgress;
            _lastUpdateTime = _resetUpdateTime;
        }

        public override void TriggerReset()
        {
            TriggerResetSync();
            TriggerManager.SendReset(this);
        }

        public void TriggerResetSync()
        {
            // Reset acceleration
            _progress = 0;
            _lastUpdateTime = Clock.Time;
        }

        public override void TriggerApply(List<TriggerContext> contexts) => TriggerReset();
        public void TriggerApplySync(float mod) => TriggerResetSync();

        private float GetMod(float mod)
        {
            if (UseContinuousGrowth)
                return (float)Math.Pow(mod, _progress);
            return Math.Pow(_progress, AccelExponent).Lerp(1f, mod);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber("EndShotDelay", 0f);
            writer.WriteNumber(nameof(EndFireRate), EndFireRate);
            writer.WriteNumber(nameof(EndFireRateMod), EndFireRateMod);
            writer.WriteString(nameof(FireRateStackLayer), FireRateStackLayer.ToString());
            writer.WriteNumber(nameof(EndShotMod), EndShotMod);
            writer.WriteString(nameof(ModStatType), ModStatType.ToString());
            writer.WriteString(nameof(ModDamageType), ModDamageType[0].ToString());
            writer.WriteString(nameof(ModStackLayer), ModStackLayer.ToString());
            writer.WriteBoolean(nameof(StoreModOnGroup), StoreModOnGroup);
            writer.WriteNumber(nameof(AccelTime), AccelTime);
            if (UseContinuousGrowth)
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
                    EndFireRate = 1f / Math.Max(CustomWeaponData.MinShotDelay, reader.GetSingle());
                    break;
                case "endfirerate":
                case "accelfirerate":
                    EndFireRate = reader.GetSingle();
                    break;
                case "endfireratemod":
                case "accelfireratemod":
                case "fireratemod":
                    EndFireRateMod = reader.GetSingle();
                    break;
                case "fireratestacklayer":
                case "fireratelayer":
                    FireRateStackLayer = reader.GetString().ToEnum(StackType.Invalid);
                    break;
                case "enddamagemod":
                case "acceldamagemod":
                case "damagemod":
                case "endshotmod":
                case "accelshotmod":
                case "shotmod":
                    EndShotMod = reader.GetSingle();
                    break;
                case "moddamagetype":
                case "damagetype":
                    ModDamageType = reader.GetString().ToDamageTypes();
                    break;
                case "damagestacklayer":
                case "modstacklayer":
                case "damagelayer":
                case "modlayer":
                    ModStackLayer = reader.GetString().ToEnum(StackType.Invalid);
                    break;
                case "modstattype":
                case "stattype":
                case "modstat":
                case "stat":
                    ModStatType = reader.GetString().ToEnum(StatType.Damage);
                    break;
                case "storemodongroup":
                case "storeongroup":
                    StoreModOnGroup = reader.GetBoolean();
                    break;
                case "acceltime":
                    AccelTime = reader.GetSingle();
                    break;
                case "accelexponent":
                case "exponent":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        if (reader.GetString()!.ToLowerInvariant() == "e")
                            UseContinuousGrowth = true;
                    }
                    else
                    {
                        AccelExponent = reader.GetSingle();
                        UseContinuousGrowth = false;
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
                    Trigger = TriggerCoordinator.Deserialize(ref reader);
                    break;
                default:
                    break;
            }
        }
    }
}

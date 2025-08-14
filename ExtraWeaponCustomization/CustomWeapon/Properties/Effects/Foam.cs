using Enemies;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils.Extensions;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class Foam : 
        Effect,
        IGunProperty,
        IMeleeProperty,
        ISyncProperty
    {
        public ushort SyncPropertyID { get; set; }

        public float Amount { get; private set; } = 0f;
        public float PrecisionAmountMulti { get; private set; } = 0f;
        public bool IgnoreArmor { get; private set; } = false;
        public bool IgnoreBackstab { get; private set; } = false;
        public bool IgnoreShotMods { get; private set; } = false;
        public float BubbleAmount { get; private set; } = 0f;
        public float BubbleStrength { get; private set; } = 1f;
        public float BubbleExpandSpeed { get; private set; } = 0.3f;
        public bool BubbleOnDoors { get; private set; } = true;
        public bool BubbleIgnoreModifiers { get; private set; } = true;
        public float BubbleLifetime { get; private set; } = 0f;
        public float FoamTime { get; private set; } = 0f;
        public FoamOverrideType FoamTimeType { get; private set; } = FoamOverrideType.Min;
        public bool IgnoreFalloff { get; private set; } = false;
        public bool IgnoreBooster { get; private set; } = true;

        private const float WallHitBuffer = 0.03f;
        private int _layerDynamic;
        private int LayerDynamic { get => _layerDynamic != 0 ? _layerDynamic : _layerDynamic = LayerManager.LAYER_DYNAMIC; }

        public Foam()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.BulletLanded));
            SetValidTriggers(DamageType.Player | DamageType.Lock, ITrigger.PositionalTriggers.Extend(TriggerName.BulletLanded, TriggerName.ChargeLanded));
        }

        public override void TriggerReset() {}
        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            PlayerAgent owner = CWC.Weapon.Owner;
            float strengthMod = IgnoreBooster ? 1f : AgentModifierManager.ApplyModifier(owner, AgentModifier.GlueStrength, 1f);
            foreach (TriggerContext tContext in triggerList)
            {
                var baseContext = (WeaponHitContextBase)tContext.context;
                // Can't spawn glue on or do glue damage to players/locks. If we hit a door (none of the damage types), ignore it if we don't bubble doors
                if (baseContext.DamageType.HasAnyFlag(DamageType.Player | DamageType.Lock))
                    continue;

                float sizeMod = tContext.triggerAmt * (IgnoreFalloff ? 1f : baseContext.Falloff);
                float damageMod = 1f;

                Vector3 position = baseContext.Position;
                GameObject? go = null;
                if (baseContext.DamageType.HasFlag(DamageType.Enemy))
                {
                    var damContext = (WeaponHitDamageableContextBase) baseContext;
                    float precisionMulti = PrecisionAmountMulti;

                    if (!IgnoreShotMods)
                    {
                        WeaponStatContext damageContext = new(damageMod, precisionMulti, 1f, DamageType.Foam.WithSubTypes(damContext.Damageable), damContext.Damageable, baseContext.ShotInfo.Orig, CWC.DebuffIDs);
                        CWC.Invoke(damageContext);
                        damageMod = damageContext.Damage;
                        precisionMulti = damageContext.Precision;
                    }

                    Dam_EnemyDamageLimb limb = damContext.Damageable.Cast<Dam_EnemyDamageLimb>();
                    go = limb.gameObject;

                    EnemyAgent agent = damContext.Damageable.GetBaseAgent().Cast<EnemyAgent>();
                    position = damContext.LocalPosition + agent.Position;

                    if (damContext.DamageType.HasFlag(DamageType.Weakspot))
                        damageMod = Math.Max(damageMod, damageMod * precisionMulti * limb.m_weakspotDamageMulti);

                    if (!IgnoreBackstab)
                        damageMod *= damContext.Backstab;

                    if (!IgnoreArmor)
                        damageMod *= limb.m_armorDamageMulti;

                    FoamActionManager.FoamDirect(limb.m_base.Owner, Amount * strengthMod * sizeMod * damageMod, this);
                }
                else
                    position += baseContext.Normal * WallHitBuffer;

                if (BubbleAmount <= 0) return;

                if (!BubbleIgnoreModifiers)
                    sizeMod *= damageMod;

                if (go != null)
                {
                    FoamActionManager.FoamEnemy(go, owner, position, sizeMod, this);
                }
                // Didn't hit enemy/player/lock, must be an environment hit
                else if (!baseContext.DamageType.HasFlag(DamageType.Enemy))
                {
                    go = baseContext.GameObject;
                    // Door check
                    if (go != null && go.layer == LayerDynamic)
                    {
                        // Don't spawn any foam if we hit a door and flag is disabled.
                        // Could let it foam static, but then we get ugly floating blobs on door opening.
                        if (!BubbleOnDoors) return;

                        iLG_WeakDoor_Destruction? comp = go.GetComponentInParent<iLG_WeakDoor_Destruction>();
                        if (comp != null && !comp.SkinnedDoorEnabled)
                        {
                            comp.EnableSkinnedDoor();
                            go = comp.FindCollider(position).gameObject;
                        }
                        FoamActionManager.FoamDoor(go, owner, position, sizeMod, this);
                    }
                    else
                    {
                        FoamActionManager.FoamStatic(owner, position, sizeMod, this);
                    }
                }
            }
        }

        public float GetMaxFoamTime(float origTime)
        {
            if (FoamTime <= 0) return origTime;

            return FoamTimeType switch
            {
                FoamOverrideType.Min => Math.Min(origTime, FoamTime),
                FoamOverrideType.Mult => origTime * FoamTime,
                FoamOverrideType.Override => FoamTime,
                _ => origTime
            };
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Amount), Amount);
            writer.WriteNumber(nameof(PrecisionAmountMulti), PrecisionAmountMulti);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreShotMods), IgnoreShotMods);
            writer.WriteNumber(nameof(BubbleAmount), BubbleAmount);
            writer.WriteNumber(nameof(BubbleStrength), BubbleStrength);
            writer.WriteNumber(nameof(BubbleExpandSpeed), BubbleExpandSpeed);
            writer.WriteBoolean(nameof(BubbleOnDoors), BubbleOnDoors);
            writer.WriteBoolean(nameof(BubbleIgnoreModifiers), BubbleIgnoreModifiers);
            writer.WriteNumber(nameof(BubbleLifetime), BubbleLifetime);
            writer.WriteNumber(nameof(FoamTime), FoamTime);
            writer.WriteString(nameof(FoamTimeType), FoamTimeType.ToString());
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(IgnoreBooster), IgnoreBooster);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "amount":
                    Amount = reader.GetSingle();
                    break;
                case "precisionamountmulti":
                case "precisionmulti":
                case "precisionmult":
                case "precision":
                    PrecisionAmountMulti = reader.GetSingle();
                    break;
                case "ignorearmor":
                    IgnoreArmor = reader.GetBoolean();
                    break;
                case "ignorebackstab":
                    IgnoreBackstab = reader.GetBoolean();
                    break;
                case "ignoredamagemods":
                case "ignoredamagemod":
                case "ignoreshotmods":
                case "ignoreshotmod":
                    IgnoreShotMods = reader.GetBoolean();
                    break;
                case "bubbleamount":
                case "bubble":
                    BubbleAmount = reader.GetSingle();
                    break;
                case "bubblestrength":
                case "strength":
                    BubbleStrength = reader.GetSingle();
                    break;
                case "bubbleexpandspeed":
                case "expandspeed":
                    BubbleExpandSpeed = reader.GetSingle();
                    break;
                case "bubbleondoors":
                case "ondoors":
                    BubbleOnDoors = reader.GetBoolean();
                    break;
                case "bubbleignoremodifiers":
                case "bubbleignoremods":
                    BubbleIgnoreModifiers = reader.GetBoolean();
                    break;
                case "bubblelifetime":
                case "lifetime":
                    BubbleLifetime = reader.GetSingle();
                    break;
                case "foamtime":
                case "time":
                    FoamTime = reader.GetSingle();
                    break;
                case "foamtimetype":
                case "timetype":
                    FoamTimeType = reader.GetString().ToEnum(FoamOverrideType.Min);
                    break;
                case "ignorefalloff":
                    IgnoreFalloff = reader.GetBoolean();
                    break;
                case "ignorebooster":
                    IgnoreBooster = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }

    public enum FoamOverrideType
    {
        Min,
        Mult, Multiply = Mult,
        Override
    }
}

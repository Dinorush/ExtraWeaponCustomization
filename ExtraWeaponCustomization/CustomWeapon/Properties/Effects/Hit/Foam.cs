using Enemies;
using EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
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
        public float BubbleAmount { get; private set; } = 0f;
        public float BubbleStrength { get; private set; } = 1f;
        public float BubbleExpandSpeed { get; private set; } = 0.3f;
        public bool IgnoreFalloff { get; private set; } = false;
        public bool IgnoreBooster { get; private set; } = true;

        private const float WallHitBuffer = -0.03f;

        public Foam()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.BulletLanded));
            SetValidTriggers(DamageType.Any, TriggerName.BulletLanded, TriggerName.PreHit, TriggerName.Hit, TriggerName.Damage, TriggerName.Charge);
        }

        public override void TriggerReset() {}
        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            PlayerAgent owner = CWC.Weapon.Owner;
            float strengthMod = IgnoreBooster ? 1f : AgentModifierManager.ApplyModifier(owner, AgentModifier.GlueStrength, 1f);
            foreach (TriggerContext tContext in triggerList)
            {
                var baseContext = (WeaponHitContextBase)tContext.context;
                // Can't spawn glue on or do glue damage to players/locks
                if (baseContext.DamageType.HasAnyFlag(DamageType.Player | DamageType.Lock)) continue;

                float sizeMod = tContext.triggerAmt * (IgnoreFalloff ? 1f : baseContext.Falloff);
                float amount = Amount * strengthMod * sizeMod;
                Vector3 position = baseContext.Position;
                GameObject? go = null;
                if (baseContext is WeaponHitDamageableContextBase damContext)
                {
                    Dam_EnemyDamageLimb limb = damContext.Damageable.Cast<Dam_EnemyDamageLimb>();
                    go = limb.gameObject;

                    EnemyAgent agent = damContext.Damageable.GetBaseAgent().Cast<EnemyAgent>();
                    position = damContext.LocalPosition + agent.Position;

                    if (damContext.DamageType.HasFlag(DamageType.Weakspot))
                        amount = Math.Max(amount, amount * PrecisionAmountMulti * limb.m_weakspotDamageMulti);

                    if (!IgnoreArmor)
                        amount *= limb.m_armorDamageMulti;

                    limb.GlueDamage(amount + 0.001f);
                }
                else
                    position += baseContext.Direction * WallHitBuffer;

                if (BubbleAmount <= 0) return;

                if (go != null)
                {
                    FoamManager.FoamEnemy(go, owner, position, sizeMod, this);
                }
                // Didn't hit enemy/player/lock, must be an environment hit
                else if (!baseContext.DamageType.HasFlag(DamageType.Enemy))
                {
                    var hitContext = (WeaponHitContext)baseContext;
                    go = hitContext.Collider.gameObject;
                    if (go != null && go.layer == LayerUtil.MaskDynamic)
                    {
                        iLG_WeakDoor_Destruction? comp = go.GetComponentInParent<iLG_WeakDoor_Destruction>();
                        if (comp != null && !comp.SkinnedDoorEnabled)
                            comp.EnableSkinnedDoor();
                        FoamManager.FoamDoor(go, owner, position, sizeMod, this);
                    }
                    else
                        FoamManager.FoamStatic(owner, position, sizeMod, this);
                }
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Amount), Amount);
            writer.WriteNumber(nameof(PrecisionAmountMulti), PrecisionAmountMulti);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteNumber(nameof(BubbleAmount), BubbleAmount);
            writer.WriteNumber(nameof(BubbleStrength), BubbleStrength);
            writer.WriteNumber(nameof(BubbleExpandSpeed), BubbleExpandSpeed);
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
}

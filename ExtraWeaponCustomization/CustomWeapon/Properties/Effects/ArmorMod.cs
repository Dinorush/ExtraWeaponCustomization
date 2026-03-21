using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ArmorMod :
        TriggerModDebuff
    {
        public PlayerDamageType[] DamageType { get; private set; } = PlayerDamageTypeConst.Any;
        public bool Immune { get; private set; } = false;
        public bool ApplyToTarget { get; private set; } = false;

        protected override OwnerType RequiredOwnerType => OwnerType.Managed;

        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        public ArmorMod() { }

        public override bool ValidProperty()
        {
            if (!ApplyToTarget && !CWC.Owner.IsType(OwnerType.Local))
                return false;
            return base.ValidProperty();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (!ApplyToTarget)
            {
                var player = CWC.Owner.Player;
                if (player != null)
                {
                    var triggerAmt = Count(contexts);
                    AddTriggerInstance(
                        new(player.Damage.Cast<IDamageable>()),
                        triggerAmt
                        );
                    TriggerManager.SendInstance(this, player, triggerAmt);
                }
                return;
            }

            if (contexts.Count > 1)
            {
                Dictionary<BaseDamageableWrapper, float> triggerDict = new();
                foreach (var context in contexts)
                {
                    if (!TryGetPlayerDamageable(context, out var damageable)) continue;

                    TempWrapper.Set(damageable);
                    if (!triggerDict.TryGetValue(TempWrapper, out var amt))
                        triggerDict.Add(new BaseDamageableWrapper(TempWrapper), context.triggerAmt);
                    else
                        triggerDict[TempWrapper] = Combine(amt, context.triggerAmt);
                }

                foreach ((BaseDamageableWrapper wrapper, float triggerAmt) in triggerDict)
                {
                    AddTriggerInstance(wrapper, triggerAmt);
                    TriggerManager.SendInstance(this, wrapper.Object!.GetBaseAgent(), triggerAmt);
                }
            }
            else
            {
                if (!TryGetPlayerDamageable(contexts[0], out var damageable)) return;

                var triggerAmt = contexts[0].triggerAmt;
                AddTriggerInstance(
                    new BaseDamageableWrapper(damageable),
                    triggerAmt
                    );
                TriggerManager.SendInstance(this, damageable.GetBaseAgent(), triggerAmt);
            }
        }

        private bool TryGetPlayerDamageable(TriggerContext tContext, [MaybeNullWhen(false)] out IDamageable damageable)
        {
            if (tContext.context is WeaponHitDamageableContextBase damContext && damContext.DamageType.HasFlag(Enums.DamageType.Player))
            {
                damageable = damContext.Damageable;
                return true;
            }
            else if (CWC.Owner.Player != null)
            {
                damageable = CWC.Owner.Player.Damage.Cast<IDamageable>();
                return true;
            }
            damageable = null;
            return false;
        }

        protected override DebuffModifierBase AddModifier(IDamageable damageable)
        {
            if (Immune)
                return DebuffManager.AddArmorImmuneBuff(damageable, DamageType);
            else
                return DebuffManager.AddArmorModBuff(damageable, 1f, StackLayer, DamageType);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteBoolean(nameof(Immune), Immune);
            writer.WriteString(nameof(DamageType), DamageType[0].ToString());
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            writer.WriteBoolean(nameof(ApplyToTarget), ApplyToTarget);
            writer.WriteNumber(nameof(GlobalID), GlobalID);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "moddamagetype":
                case "damagetype":
                    DamageType = reader.GetString().ToPlayerDamageTypes();
                    break;
                case "immune":
                case "immunity":
                    Immune = reader.GetBoolean();
                    break;
                case "applytotarget":
                    ApplyToTarget = reader.GetBoolean();
                    break;
            }
        }
    }
}

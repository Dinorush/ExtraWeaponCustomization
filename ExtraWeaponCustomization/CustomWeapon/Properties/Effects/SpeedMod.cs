using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Speed;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using ModifierAPI;
using Player;
using SNetwork;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpeedMod :
        TriggerModTimed,
        ITriggerCallbackBasicSync
    {
        private const string APIGroup = "EWC";

        public ushort SyncID { get; set; }

        public bool ApplyToTarget { get; private set; } = false;

        protected override OwnerType RequiredOwnerType => OwnerType.Managed;

        private static ObjectWrapper<SNet_Player> TempWrapper => ObjectWrapper<SNet_Player>.SharedInstance;

        private IStatModifier _speedModifier = null!;

        public override bool ValidProperty()
        {
            if (!ApplyToTarget && !CWC.Owner.IsType(OwnerType.Local))
                return false;
            return base.ValidProperty();
        }

        protected override void OnUpdate(float mod) => _speedModifier.Enable(mod);
        protected override void OnDisable() => _speedModifier.Disable();

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (!ApplyToTarget)
            {
                base.TriggerApply(contexts);
                return;
            }

            if (contexts.Count == 1)
            {
                var context = contexts[0];
                if (TryGetSyncedPlayer(context, out var target))
                    SpeedManager.ApplySpeedMod(target.Owner, this, context.triggerAmt);
                else if (CWC.Owner.IsType(OwnerType.Local))
                    TriggerApplySync(context.triggerAmt);
                return;
            }

            float localAmt = 0;
            Dictionary<ObjectWrapper<SNet_Player>, float> otherTargets = new();
            foreach (var tContext in contexts)
            {
                if (!TryGetSyncedPlayer(tContext, out var target))
                    localAmt = Combine(localAmt, tContext.triggerAmt);
                else
                {
                    if (otherTargets.TryGetValue(TempWrapper.Set(target.Owner), out var amt))
                        otherTargets[TempWrapper] = Combine(amt, tContext.triggerAmt);
                    else
                        otherTargets[new(TempWrapper)] = tContext.triggerAmt;
                }
            }

            if (localAmt > 0 && CWC.Owner.IsType(OwnerType.Local))
                TriggerApplySync(localAmt);
            foreach ((var wrapper, float amt) in otherTargets)
                SpeedManager.ApplySpeedMod(wrapper.Object!, this, amt);
        }

        private bool TryGetSyncedPlayer(TriggerContext tContext, [MaybeNullWhen(false)] out PlayerAgent player)
        {
            if (tContext.context is WeaponHitDamageableContextBase damContext && damContext.DamageType.HasFlag(DamageType.Player))
            {
                player = damContext.Damageable.GetBaseAgent().Cast<PlayerAgent>();
                return !player.IsLocallyOwned;
            }
            player = null;
            return false;
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (SpeedMod) base.Clone();
            copy._speedModifier = MoveSpeedAPI.AddModifier(1f, LayerToAPILayer(), APIGroup);
            copy._speedModifier.Disable();
            return copy;
        }

        private StackLayer LayerToAPILayer()
        {
            return StackLayer switch
            {
                StackType.Override => ModifierAPI.StackLayer.Override,
                StackType.Max => ModifierAPI.StackLayer.Max,
                StackType.Min => ModifierAPI.StackLayer.Min,
                StackType.Mult => ModifierAPI.StackLayer.Multiply,
                StackType.Add => ModifierAPI.StackLayer.Add,
                _ => ModifierAPI.StackLayer.Multiply
            };
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(InnerStackType), InnerStackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            writer.WriteBoolean(nameof(ApplyToTarget), ApplyToTarget);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "applytotarget":
                    ApplyToTarget = reader.GetBoolean();
                    break;
            }
        }
    }
}

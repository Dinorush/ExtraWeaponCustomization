using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.PlayerPush;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.JSON;
using EWC.Utils.Extensions;
using Player;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class Push :
        Effect,
        ISyncProperty
    {
        public ushort SyncPropertyID { get; set; }

        public float Force { get; private set; } = 0f;
        public List<float> Offset { get; private set; } = new(2) { 0, 0 };
        public float FrictionDelay { get; private set; } = 0.1f;
        public float RepeatFrictionDelay { get; private set; } = 0.1f;
        public float FrictionStrength { get; private set; } = 8f;
        public float ConstantFriction { get; private set; } = 1f;
        public float AirFrictionStrength { get; private set; } = 1f;
        public float AirConstantFriction { get; private set; } = 1f;
        public PushCap HorizontalCap { get; private set; } = new();
        public PushCap VerticalCap { get; private set; } = new();
        public float VerticalScale { get; private set; } = 1f;
        public bool NormalizeForce { get; private set; } = true;
        public bool ApplyToTarget { get; private set; } = false;

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (ApplyToTarget)
            {
                foreach (var tContext in contexts)
                {
                    PlayerAgent? target;
                    Vector3 dir;
                    if (tContext.context is WeaponHitDamageableContextBase damContext && damContext.DamageType.HasFlag(DamageType.Player))
                    {
                        target = damContext.Damageable.GetBaseAgent().Cast<PlayerAgent>();
                        dir = damContext.Direction;
                    }
                    else
                    {
                        target = CWC.Owner.Player;
                        dir = CWC.Owner.FireDir;
                    }

                    if (target != null)
                        DoPush(target, dir, tContext.triggerAmt);
                }
            }
            else
            {
                if (CWC.Owner.Player != null)
                    DoPush(CWC.Owner.Player, CWC.Owner.FireDir, contexts.Sum(tContext => tContext.triggerAmt));
            }
        }

        private void DoPush(PlayerAgent target, Vector3 dir, float triggerAmt)
        {
            if (target.Owner.IsBot) return;

            var force = dir;
            if (VerticalScale != 1)
            {
                force.y *= VerticalScale;
                if (NormalizeForce)
                    force = force.normalized;
            }
            force = force.RotateBy(Offset[0], Offset[1]);
            force *= Force * triggerAmt;

            if (force == Vector3.zero) return;

            PushManager.DoPush(target, force, this);
        }

        public override void TriggerReset() { }

        public override WeaponPropertyBase Clone()
        {
            var copy = (Push) base.Clone();
            copy.Offset = Offset;
            copy.HorizontalCap = HorizontalCap;
            copy.VerticalCap = VerticalCap;
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Force), Force);
            EWCJson.Serialize(writer, nameof(Offset), Offset);
            writer.WriteNumber(nameof(FrictionDelay), FrictionDelay);
            writer.WriteNumber(nameof(RepeatFrictionDelay), RepeatFrictionDelay);
            writer.WriteNumber(nameof(FrictionStrength), FrictionStrength);
            writer.WriteNumber(nameof(ConstantFriction), ConstantFriction);
            writer.WriteNumber(nameof(AirFrictionStrength), AirFrictionStrength);
            writer.WriteNumber(nameof(AirConstantFriction), AirConstantFriction);
            writer.WriteNumber(nameof(VerticalScale), VerticalScale);
            writer.WriteBoolean(nameof(NormalizeForce), NormalizeForce);
            writer.WritePropertyName(nameof(HorizontalCap));
            HorizontalCap.Serialize(writer);
            writer.WritePropertyName(nameof(VerticalCap));
            VerticalCap.Serialize(writer);
            writer.WriteBoolean(nameof(ApplyToTarget), ApplyToTarget);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "force":
                    Force = reader.GetSingle();
                    break;
                case "offset":
                    List<float>? offset = ReadOffset(ref reader);
                    if (offset == null) return;
                    Offset = offset;
                    break;
                case "frictiondelay":
                    FrictionDelay = reader.GetSingle();
                    break;
                case "repeatfrictiondelay":
                    RepeatFrictionDelay = reader.GetSingle();
                    break;
                case "frictionstrength":
                    FrictionStrength = reader.GetSingle();
                    break;
                case "constantfriction":
                    ConstantFriction = reader.GetSingle();
                    break;
                case "airfrictionstrength":
                    AirFrictionStrength = reader.GetSingle();
                    break;
                case "airconstantfriction":
                    AirConstantFriction = reader.GetSingle();
                    break;
                case "horizontalcap":
                    HorizontalCap.Deserialize(ref reader);
                    break;
                case "verticalcap":
                    VerticalCap.Deserialize(ref reader);
                    break;
                case "verticalscale":
                case "vertical":
                    VerticalScale = reader.GetSingle();
                    break;
                case "normalizeforce":
                    NormalizeForce = reader.GetBoolean();
                    break;
                case "applytotarget":
                    ApplyToTarget = reader.GetBoolean();
                    break;
            }
        }

        private static List<float>? ReadOffset(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected list object");

            List<float> offsets = new();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for x offset");
            offsets.Add(reader.GetSingle());

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for y offset");
            offsets.Add(reader.GetSingle());

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Expected EndArray token for [x,y] offset pair");

            return offsets;
        }
    }
}

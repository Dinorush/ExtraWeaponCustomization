using System;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.PlayerPush
{
    public class PushCap
    {
        public float Cap { get; private set; } = 0f;
        public float SoftCap { get; private set; } = 0f;
        public float SoftCapMod { get; private set; } = 0.5f;
        public bool IncludeVelocity { get; private set; } = true;

        private const float CapSteerStrength = 0.25f;

        public Vector3 AddAndCap(Vector3 current, Vector3 force, Vector3 velocity)
        {
            var forceDir = force.normalized;
            float forceMag = force.magnitude;
            var parallelMag = Vector3.Dot(current, forceDir);
            var velParallelMag = Vector3.Dot(velocity, forceDir);
            float targetParallelMag = forceMag + parallelMag + velParallelMag;

            if (SoftCap > 0 && targetParallelMag > SoftCap)
            {
                float softForce = Math.Min(targetParallelMag - SoftCap, forceMag);
                forceMag -= (1 - SoftCapMod) * softForce;
                targetParallelMag = forceMag + parallelMag + velParallelMag;
            }

            if (Cap > 0 && targetParallelMag > Cap)
            {
                float newMag;
                float leftover;
                if (parallelMag + velParallelMag >= Cap)
                {
                    newMag = parallelMag;
                    leftover = forceMag;
                }
                else
                {
                    newMag = Math.Min(parallelMag + forceMag, Cap - velParallelMag);
                    leftover = parallelMag + forceMag - newMag;
                }

                var perpendicular = current - forceDir * parallelMag;
                float perpMag = perpendicular.magnitude;
                perpMag = Math.Max(0, perpMag - leftover * CapSteerStrength);
                perpendicular = perpMag > 0.01f ? perpendicular.normalized * perpMag : Vector3.zero;

                return forceDir * newMag + perpendicular;
            }

            return current + forceDir * forceMag;
        }

        public float AddAndCap(float current, float force)
        {
            if (SoftCap > 0 && force + current > SoftCap)
            {
                float softForce = Math.Min(force + current - SoftCap, force);
                force -= (1 - SoftCapMod) * softForce;
            }

            if (Cap > 0 && force + current > Cap)
            {
                return Math.Max(current, Cap);
            }

            return current + force;
        }

        public void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(SoftCap), SoftCap);
            writer.WriteNumber(nameof(SoftCapMod), SoftCapMod);
            writer.WriteBoolean(nameof(IncludeVelocity), IncludeVelocity);
            writer.WriteEndObject();
        }

        private void DeserializeProperty(string propertyName, ref Utf8JsonReader reader)
        {
            switch (propertyName)
            {
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "softcap":
                    SoftCap = reader.GetSingle();
                    break;
                case "softcapmod":
                    SoftCapMod = reader.GetSingle();
                    break;
                case "includevelocity":
                    IncludeVelocity = reader.GetBoolean();
                    break;
            }
        }

        public void Deserialize(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                Cap = reader.GetSingle();
                return;
            }

            if (reader.TokenType != JsonTokenType.StartObject) throw new Exception("Expected number or StartObject token");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName token");

                string property = reader.GetString()!;
                reader.Read();
                DeserializeProperty(property.ToLowerInvariant().Replace(" ", ""), ref reader);
            }

            throw new JsonException("Expected EndObject token");
        }
    }
}

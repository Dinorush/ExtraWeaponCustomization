using EWC.JSON;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile
{
    public sealed class ProjectileDirChange
    {
        public readonly List<float> Offsets = new(2);
        public float Exponent { get; private set; } = 1f;
        public float Delay { get; private set; } = 0f;
        public float ChangeTime { get; private set; } = 0f;

        public class State
        {
            public readonly ProjectileDirChange Settings;
            private readonly float _startTime;
            private readonly float _x;
            private readonly float _y;
            private readonly Vector3 _right;
            private readonly Vector3 _up;
            private bool _isDone;
            private float _lastX;
            private float _lastY;

            public State(Vector3 dir, ushort shotIndex, ProjectileDirChange settings)
            {
                _startTime = Time.time;
                Settings = settings;
                _x = settings.Offsets[(shotIndex * 2) % settings.Offsets.Count];
                _y = settings.Offsets[(shotIndex * 2 + 1) % settings.Offsets.Count];
                _lastX = 0;
                _lastY = 0;
                Vector3 cross = Math.Abs(dir.y) < 0.99f ? Vector3.up : Vector3.forward;
                _right = Vector3.Cross(cross, dir).normalized;
                _up = Vector3.Cross(_right, dir).normalized;
            }

            public void Update(ref Vector3 dir)
            {
                if (_isDone) return;

                float timeFromStart = Time.time - _startTime - Settings.Delay;
                if (timeFromStart < 0f) return;

                float scale = Settings.ChangeTime > 0 ? (float)Math.Pow(Math.Min(timeFromStart / Settings.ChangeTime, 1f), Settings.Exponent) : 1f;
                float x = _x * scale;
                float y = _y * scale;
                float rotX = x - _lastX;
                float rotY = y - _lastY;
                if (rotX != 0)
                    dir = Quaternion.AngleAxis(-rotX, _up) * dir;
                if (rotY != 0)
                    dir = Quaternion.AngleAxis(-rotY, _right) * dir;

                _lastX = x;
                _lastY = y;

                if (scale == 1f)
                    _isDone = true;
            }

            public void Copy(State state)
            {
                _lastX = state._lastX;
                _lastY = state._lastY;
                _isDone = state._isDone;
            }
        }

        public State CreateState(Vector3 dir, ushort shotIndex) => new(dir, shotIndex, this);

        public void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Offsets");
            EWCJson.Serialize(writer, nameof(Offsets), Offsets);
            writer.WriteNumber(nameof(Exponent), Exponent);
            //writer.WriteString(nameof(MoveType), MoveType.ToString());
            writer.WriteNumber(nameof(Delay), Delay);
            writer.WriteNumber(nameof(ChangeTime), ChangeTime);
            writer.WriteEndObject();
        }

        public static void SerializeList(List<ProjectileDirChange> moveChanges, Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var statChange in moveChanges)
                statChange.Serialize(writer);
            writer.WriteEndArray();
        }

        private void DeserializeProperty(string propertyName, ref Utf8JsonReader reader)
        {
            switch (propertyName)
            {
                case "offsets":
                case "offset":
                    List<float>? offsets = ReadOffsets(ref reader);
                    if (offsets == null) return;
                    if (offsets.Count % 2 != 0)
                        offsets.RemoveAt(offsets.Count - 1);
                    Offsets.AddRange(offsets);
                    break;
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                case "delay":
                    Delay = reader.GetSingle();
                    break;
                case "changetime":
                case "time":
                case "duration":
                    ChangeTime = reader.GetSingle();
                    break;
            }
        }

        public void Deserialize(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new Exception("Expected StartObject token");

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

        private static List<float>? ReadOffsets(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected list object");

            List<float> offsets = new();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return offsets;

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for x offset");
                    offsets.Add(reader.GetSingle());

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for y offset");
                    offsets.Add(reader.GetSingle());

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Expected EndArray token for [x,y] offset pair");
                }
                else
                {
                    if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for offset value");

                    offsets.Add(reader.GetSingle());
                }
            }

            throw new JsonException("Expected EndArray token");
        }

        public static List<ProjectileDirChange> DeserializeList(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new Exception("Expected StartArray token");

            List<ProjectileDirChange> moveChanges = new();
            while(reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return moveChanges;

                ProjectileDirChange moveChange = new();
                moveChange.Deserialize(ref reader);
                moveChanges.Add(moveChange);
            }

            throw new JsonException("Expected EndArray token");
        }
    }
}

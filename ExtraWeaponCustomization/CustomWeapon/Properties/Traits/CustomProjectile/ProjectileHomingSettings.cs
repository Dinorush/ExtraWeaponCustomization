using EWC.Utils;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile
{
    public sealed class ProjectileHomingSettings
    {
        public float InitialHomingStrength { get; private set; } = 0f;
        public float InitialHomingDuration { get; private set; } = 0f;

        public float HomingStrength { get; private set; } = 0f;
        public float HomingDelay { get; private set; } = 0f;
        public float HomingMaxDist { get; private set; } = 50f;
        public float HomingMinDist { get; private set; } = 10f;
        public float HomingDistExponent { get; private set; } = 3f;

        public TargetingMode TargetMode { get; private set; } = TargetingMode.Normal;

        public float SearchAngle { get; private set; } = 0f;
        public float SearchRange { get; private set; } = 50f;
        public float SearchTickDelay { get; private set; } = 0.1f;
        public SearchMode SearchInitialMode { get; private set; } = SearchMode.Normal;
        public bool SearchIgnoreWalls { get; private set;} = false;
        public bool SearchIgnoreInvisibility { get; private set; } = false;
        public bool SearchTagOnly { get; private set; } = false;
        public bool StopSearchOnTargetDead { get; private set; } = false;
        public bool StopSearchOnPierce { get; private set; } = false;

        public bool HomingEnabled => HomingStrength > 0f || InitialHomingStrength > 0f;

        public void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(InitialHomingStrength), InitialHomingStrength);
            writer.WriteNumber(nameof(InitialHomingDuration), InitialHomingDuration);
            writer.WriteNumber(nameof(HomingStrength), HomingStrength);
            writer.WriteNumber(nameof(HomingDelay), HomingDelay);
            writer.WriteNumber(nameof(HomingMaxDist), HomingMaxDist);
            writer.WriteNumber(nameof(HomingMinDist), HomingMinDist);
            writer.WriteNumber(nameof(HomingDistExponent), HomingDistExponent);
            writer.WriteString(nameof(TargetMode), TargetMode.ToString());
            writer.WriteNumber(nameof(SearchAngle), SearchAngle);
            writer.WriteNumber(nameof(SearchRange), SearchRange);
            writer.WriteNumber(nameof(SearchTickDelay), SearchTickDelay);
            writer.WriteString(nameof(SearchInitialMode), SearchInitialMode.ToString());
            writer.WriteBoolean(nameof(SearchIgnoreWalls), SearchIgnoreWalls);
            writer.WriteBoolean(nameof(SearchIgnoreInvisibility), SearchIgnoreInvisibility);
            writer.WriteBoolean(nameof(SearchTagOnly), SearchTagOnly);
            writer.WriteBoolean(nameof(StopSearchOnTargetDead), StopSearchOnTargetDead);
            writer.WriteEndObject();
        }

        private void DeserializeProperty(string propertyName, ref Utf8JsonReader reader)
        {
            switch (propertyName)
            {
                case "initialhomingstrength":
                case "initialstrength":
                    InitialHomingStrength = reader.GetSingle();
                    break;
                case "initialhomingduration":
                case "initialduration":
                    InitialHomingDuration = reader.GetSingle();
                    break;

                case "homingstrength":
                case "strength":
                    HomingStrength = reader.GetSingle();
                    break;
                case "homingdelay":
                case "delay":
                    HomingDelay = reader.GetSingle();
                    break;
                case "homingmaxdist":
                case "maxdist":
                    HomingMaxDist = reader.GetSingle();
                    break;
                case "homingmindist":
                case "mindist":
                    HomingMinDist = reader.GetSingle();
                    break;
                case "homingdistexponent":
                case "distexponent":
                    HomingDistExponent = reader.GetSingle();
                    break;

                case "targetingmode":
                case "targetmode":
                    TargetMode = reader.GetString()!.ToEnum(TargetingMode.Normal);
                    break;

                case "searchangle":
                case "angle":
                    SearchAngle = reader.GetSingle();
                    break;
                case "searchrange":
                case "range":
                    SearchRange = reader.GetSingle();
                    break;
                case "searchtickdelay":
                case "tickdelay":
                    SearchTickDelay = reader.GetSingle();
                    break;
                case "searchinitialmode":
                case "searchmode":
                    SearchInitialMode = reader.GetString()!.ToEnum(SearchMode.Normal);
                    break;
                case "searchignorewalls":
                case "ignorewalls":
                    SearchIgnoreWalls = reader.GetBoolean();
                    break;
                case "searchignoreinvisibility":
                case "searchignoreinvis":
                case "ignoreinvisibility":
                case "ignoreinvis":
                    SearchIgnoreInvisibility = reader.GetBoolean();
                    break;
                case "searchtagonly":
                case "tagonly":
                    SearchTagOnly = reader.GetBoolean();
                    break;
                case "stopsearchontargetdead":
                case "stopontargetdead":
                    StopSearchOnTargetDead = reader.GetBoolean();
                    break;
                case "stopsearchonpierce":
                case "stoponpierce":
                    StopSearchOnPierce = reader.GetBoolean();
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
    }

    public enum SearchMode
    {
        Normal,
        AimDir,
        AutoAim
    }
}

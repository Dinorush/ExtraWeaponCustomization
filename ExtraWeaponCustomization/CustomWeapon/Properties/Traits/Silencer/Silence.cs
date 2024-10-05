using Agents;
using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using Player;
using SNetwork;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    internal class Silence :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponPreFireContextSync>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
    {
        public float FakeAlertRadius { get; set; } = 0f;
        public float AlertRadius { get; set; } = 0f;
        public float AlertAmount { get; set; } = 0f;
        public float WakeUpRadius { get; set; } = 0f;
        public bool CrossDoors { get; set; } = false;

        private readonly static List<EnemyAgent> s_wakeupList = new();
        private readonly static List<EnemyAgent> s_alertList = new();
        private readonly static List<EnemyAgent> s_fakeAlertList = new();

        private static NM_NoiseData s_noiseData = new();
        private static Agent.NoiseType s_cachedNoise = Agent.NoiseType.None;
        private static float s_cachedTimestamp = 0f;
        private static Ray s_ray;
        private const SearchSetting SearchSettings = SearchSetting.CheckDoors;

        public void Invoke(WeaponPreFireContext context)
        {
            s_cachedNoise = CWC.Weapon.Owner.Noise;
            s_cachedTimestamp = CWC.Weapon.Owner.m_noiseTimestamp;
        }

        public void Invoke(WeaponPreFireContextSync context)
        {
            s_cachedNoise = CWC.Weapon.Owner.Noise;
            s_cachedTimestamp = CWC.Weapon.Owner.m_noiseTimestamp;
        }

        public void Invoke(WeaponPostFireContext context)
        {
            OverrideSound();
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            OverrideSound();
        }

        private void OverrideSound()
        {
            if (!SNet.IsMaster) return;

            PlayerAgent owner = CWC.Weapon.Owner;
            owner.m_noise = s_cachedNoise;
            owner.m_noiseTimestamp = s_cachedTimestamp;

            s_noiseData.noiseMaker = owner.TryCast<INM_NoiseMaker>()!;

            bool runAlert = AlertRadius > WakeUpRadius;
            bool runFakeAlert = FakeAlertRadius > AlertRadius && FakeAlertRadius > WakeUpRadius;

            s_ray.origin = owner.IsLocallyOwned ? Weapon.s_ray.origin : owner.EyePosition;
            s_ray.direction = owner.IsLocallyOwned ? owner.FPSCamera.CameraRayDir : CWC.Weapon.MuzzleAlign.forward;

            if (runFakeAlert)
            {
                s_fakeAlertList.Clear();
                s_fakeAlertList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, FakeAlertRadius, 180f, owner.CourseNode, SearchSettings));
            }

            if (runAlert)
            {
                s_alertList.Clear();
                s_alertList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, AlertRadius, 180f, owner.CourseNode, SearchSettings));
            }

            s_wakeupList.Clear();
            s_wakeupList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, WakeUpRadius, 180f, owner.CourseNode, SearchSettings));

            s_noiseData.type = NM_NoiseType.InstaDetect;
            foreach (var enemy in s_wakeupList)
            {
                if (!CrossDoors && enemy.CourseNode.NodeID != owner.CourseNode.NodeID)
                    continue;
                if (runAlert)
                    RemoveFromList(enemy, s_alertList);
                if (runFakeAlert)
                    RemoveFromList(enemy, s_fakeAlertList);
                enemy.TryCast<INM_Listener>()!.ListenerDetectNoise(ref s_noiseData, 0f);
            }

            foreach (var enemy in s_alertList)
            {
                if (!CrossDoors && enemy.CourseNode.NodeID != owner.CourseNode.NodeID)
                    continue;
                if (runFakeAlert)
                    RemoveFromList(enemy, s_fakeAlertList);
                AgentTarget target = enemy.AI.m_behaviourData.GetTarget(owner);
                target.m_noiseDetectWindowTimeTotal += AlertAmount;
            }

            s_noiseData.type = NM_NoiseType.PulseOnly;
            foreach (var enemy in s_fakeAlertList)
            {
                if (!CrossDoors && enemy.CourseNode.NodeID != owner.CourseNode.NodeID)
                    continue;
                enemy.TryCast<INM_Listener>()!.ListenerDetectNoise(ref s_noiseData, 0f);
            }
        }

        private void RemoveFromList(EnemyAgent remove, List<EnemyAgent> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (remove.GetInstanceID() == list[i].GetInstanceID())
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        public override IWeaponProperty Clone()
        {
            Silence copy = new()
            {
                FakeAlertRadius = FakeAlertRadius,
                AlertRadius = AlertRadius,
                AlertAmount = AlertAmount,
                WakeUpRadius = WakeUpRadius
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(FakeAlertRadius), FakeAlertRadius);
            writer.WriteNumber(nameof(AlertRadius), AlertRadius);
            writer.WriteNumber(nameof(AlertAmount), AlertAmount);
            writer.WriteNumber(nameof(WakeUpRadius), WakeUpRadius);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "fakealertradius":
                case "fakealert":
                    FakeAlertRadius = reader.GetSingle();
                    break;
                case "alertradius":
                case "alert":
                    AlertRadius = reader.GetSingle();
                    break;
                case "alertamount":
                    AlertAmount = reader.GetSingle();
                    break;
                case "wakeupradius":
                case "wakeup":
                    WakeUpRadius = reader.GetSingle();
                    break;
                case "crossdoors":
                    CrossDoors = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}

using Agents;
using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using EWC.Utils.Extensions;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class Silence :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponPreFireContextSync>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>,
        IWeaponProperty<WeaponStealthUpdateContext>
    {
        public float FakeAlertRadius { get; private set; } = 0f;
        public float AlertRadius { get; private set; } = 0f;
        public float AlertAmount { get; private set; } = 0f;
        public float WakeUpRadius { get; private set; } = 0f;
        public CrossDoorMode CrossDoorMode { get; private set; } = CrossDoorMode.Normal;

        private readonly Dictionary<ObjectWrapper<Agent>, float> _alertProgress = new();

        private readonly static List<EnemyAgent> s_wakeupList = new();
        private readonly static List<EnemyAgent> s_alertList = new();
        private readonly static List<EnemyAgent> s_fakeAlertList = new();

        private static Agent.NoiseType s_cachedNoise;
        private static float s_cachedTimestamp;
        private static Ray s_ray = new(Vector3.zero, Vector3.forward);
        private const SearchSetting SearchSettings = SearchSetting.CheckDoors;

        private const float InstantWakeupProgress = 1000f;
        // Based on R6Mono
        private const float InstantWakeupOutput = 1000f;
        private const float WakeupOutput = 1.1f;
        private const float AlertOutput = 0.95f;

        private static ObjectWrapper<Agent> TempWrapper => ObjectWrapper<Agent>.SharedInstance;

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponPreFireContext)
             || contextType == typeof(WeaponPostFireContext)
             || contextType == typeof(WeaponStealthUpdateContext))
                return SNet.IsMaster;

            return base.ShouldRegister(contextType);
        }

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

        public void Invoke(WeaponStealthUpdateContext context)
        {
            TempWrapper.Set(context.Enemy);
            if (!_alertProgress.TryGetValue(TempWrapper, out float progress)) return;

            if (progress >= InstantWakeupProgress)
            {
                context.Output = InstantWakeupOutput;
                _alertProgress.Remove(TempWrapper);
                return;
            }

            if (!context.Detecting)
            {
                _alertProgress.Remove(TempWrapper);
                return;
            }

            float output = progress >= 1f ? WakeupOutput : AlertOutput;
            if (output > context.Output)
                context.Output = output;
        }

        private void OverrideSound()
        {
            if (!SNet.IsMaster) return;

            PlayerAgent owner = CWC.Weapon.Owner;
            owner.m_noise = s_cachedNoise;
            owner.m_noiseTimestamp = s_cachedTimestamp;

            bool runAlert = AlertRadius > WakeUpRadius;
            bool runFakeAlert = FakeAlertRadius > AlertRadius && FakeAlertRadius > WakeUpRadius;

            s_ray.origin = owner.IsLocallyOwned ? Weapon.s_ray.origin : owner.EyePosition;

            // Cache enemies
            if (runFakeAlert)
            {
                s_fakeAlertList.Clear();
                s_fakeAlertList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, FakeAlertRadius, 180f, owner.CourseNode, SearchSettings));
            }

            if (runAlert)
            {
                foreach (ObjectWrapper<Agent> wrapper in _alertProgress.Keys.Where(key => key.Object == null || !key.Object.Alive).ToList())
                    _alertProgress.Remove(wrapper);
                s_alertList.Clear();
                s_alertList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, AlertRadius, 180f, owner.CourseNode, SearchSettings));
            }

            s_wakeupList.Clear();
            s_wakeupList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, WakeUpRadius, 180f, owner.CourseNode, SearchSettings));

            Vector3 pos = owner.Position;

            // Instant wakeup
            for (int i = s_wakeupList.Count - 1; i >= 0; --i)
            {
                EnemyAgent enemy = s_wakeupList[i];
                if (runAlert)
                    RemoveFromList(enemy, s_alertList);
                if (runFakeAlert)
                    RemoveFromList(enemy, s_fakeAlertList);
                if (!EnemyCanHear(enemy, WakeUpRadius))
                    continue;

                TempWrapper.Set(enemy);
                if (!_alertProgress.ContainsKey(TempWrapper))
                    _alertProgress.Add(new ObjectWrapper<Agent>(enemy), 0);

                _alertProgress[TempWrapper] += InstantWakeupProgress;
            }

            // Standard alert (like walking)
            for (int i = s_alertList.Count - 1; i >= 0; --i)
            {
                EnemyAgent enemy = s_alertList[i];
                if (runFakeAlert)
                    RemoveFromList(enemy, s_fakeAlertList);
                if (!EnemyCanHear(enemy, AlertRadius))
                    continue;

                TempWrapper.Set(enemy);
                if (!_alertProgress.ContainsKey(TempWrapper))
                    _alertProgress.Add(new ObjectWrapper<Agent>(enemy), 0);

                EnemyDetection detection = enemy.AI.m_detection;
                // If the enemy is asleep, make them alert (could do this by setting progress > 0 but sounds icky)
                if (detection.m_noiseDetectionStatus == EnemyDetection.DetectionStatus.Deactivated)
                    detection.m_statusTimerEnd = 0;
                // If the enemy is alert (not transitioning), then add progress
                else if (detection.m_noiseDetectionOn)
                    _alertProgress[TempWrapper] += AlertAmount;
            }

            // Fake pulse
            foreach (var enemy in s_fakeAlertList)
            {
                if (!EnemyCanHear(enemy, FakeAlertRadius))
                    continue;

                enemy.AI.m_locomotion.Hibernate.Heartbeat(0.5f, CWC.Weapon.Owner.Position);
            }
        }

        private bool EnemyCanHear(EnemyAgent enemy, float range)
        {
            if (!enemy.ListenerReady) return false;

            PlayerAgent owner = CWC.Weapon.Owner;
            float nodeDistance = enemy.CourseNode.m_playerCoverage.GetNodeDistanceToPlayer_Unblocked(owner);
            if (!enemy.AI.m_detection.GetWeaponDetectionDistance(nodeDistance, out float weaponDist)) return false;

            if (CrossDoorMode == CrossDoorMode.None && enemy.CourseNode.NodeID != owner.CourseNode.NodeID) return false;

            float baseDist = enemy.EnemyDetectionData.weaponDetectionDistanceMax;
            if (CrossDoorMode == CrossDoorMode.Normal)
                weaponDist = Math.Max(0, range - (baseDist - weaponDist));

            if ((owner.Position - enemy.Position).sqrMagnitude >= weaponDist * weaponDist) return false;

            return true;
        }

        private void RemoveFromList(EnemyAgent remove, List<EnemyAgent> list)
        {
            // Imagine doing list.Remove(enemy) when il2cpp exists
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (remove.Pointer == list[i].Pointer)
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(FakeAlertRadius), FakeAlertRadius);
            writer.WriteNumber(nameof(AlertRadius), AlertRadius);
            writer.WriteNumber(nameof(AlertAmount), AlertAmount);
            writer.WriteNumber(nameof(WakeUpRadius), WakeUpRadius);
            writer.WriteString(nameof(CrossDoorMode), CrossDoorMode.ToString());
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
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
                case "crossdoormode":
                case "crossdoors":
                case "crossdoor":
                    if (reader.TokenType == JsonTokenType.False || reader.TokenType == JsonTokenType.True)
                    {
                        Utils.Log.EWCLogger.Warning("CrossDoors using true/false is deprecated. Use \"Normal\", \"None\", or \"NoPenalty\" instead.");
                        if (reader.GetBoolean())
                            CrossDoorMode = CrossDoorMode.Normal;
                        else
                            CrossDoorMode = CrossDoorMode.None;
                    }
                    CrossDoorMode = reader.GetString().ToEnum(CrossDoorMode.Normal);
                    break;
                default:
                    break;
            }
        }
    }

    public enum CrossDoorMode
    {
        Normal,
        None,
        NoPenalty
    }
}

using Agents;
using AIGraph;
using AmorLib.Utils;
using Enemies;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.Utils;
using EWC.Utils.Extensions;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class Noise :
        Effect,
        IWeaponProperty<WeaponStealthUpdateContext>,
        ITriggerCallbackDirSync
    {
        public ushort SyncID { get; set; }

        public float FakeAlertRadius { get; private set; } = 0f;
        public float AlertRadius { get; private set; } = 0f;
        public float AlertAmount { get; private set; } = 0f;
        public float WakeUpRadius { get; private set; } = 0f;
        public uint SoundID { get; private set; } = 0u;
        public CrossDoorMode CrossDoorMode { get; private set; } = CrossDoorMode.NoPenalty;
        public bool UseNoiseSystem { get; private set; } = false;
        public bool LocalSoundOnly { get; private set; } = false;
        public bool FollowUser { get; private set; } = false;

        private readonly Dictionary<ObjectWrapper<Agent>, float> _alertProgress = new();

        private readonly static List<EnemyAgent> s_wakeupList = new();
        private readonly static List<EnemyAgent> s_alertList = new();
        private readonly static List<EnemyAgent> s_fakeAlertList = new();

        private static Ray s_ray = new(Vector3.zero, Vector3.forward);
        private const SearchSetting SearchSettings = SearchSetting.CheckDoors;

        private const float InstantWakeupProgress = 1000f;
        // Based on R6Mono
        private const float InstantWakeupOutput = 1000f;
        private const float WakeupOutput = 1.1f;
        private const float AlertOutput = 0.95f;

        private static ObjectWrapper<Agent> TempWrapper => ObjectWrapper<Agent>.SharedInstance;

        public override bool ValidProperty()
        {
            if (LocalSoundOnly && !CWC.Owner.IsType(Enums.OwnerType.Local))
                return false;
            return base.ValidProperty();
        }

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponStealthUpdateContext)) return !LocalSoundOnly && !UseNoiseSystem && SNet.IsMaster;

            return base.ShouldRegister(contextType);
        }

        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            foreach (var trigger in triggerList)
            {
                Vector3 position = !FollowUser && trigger.context is WeaponHitContextBase hitContext ? hitContext.Position : CWC.Owner.FirePos;
                if (LocalSoundOnly)
                {
                    PostSound(position);
                    continue;
                }

                TriggerApplySync(position, Vector3.zero, trigger.triggerAmt);
                TriggerManager.SendInstance(this, position, Vector3.zero, trigger.triggerAmt);

                if (UseNoiseSystem)
                    MakeNoise(position);
            }
        }

        public void TriggerApplySync(Vector3 position, Vector3 dir, float mod)
        {
            PostSound(position);

            if (UseNoiseSystem || !SNet.IsMaster) return;

            TriggerSound(position, mod);
        }

        private void PostSound(Vector3 position)
        {
            if (FollowUser)
                CWC.Weapon.Sound.Post(SoundID, position);
            else
                CellSound.Post(SoundID, position);
        }

        public void TriggerResetSync()
        {
            _alertProgress.Clear();
        }

        public override void TriggerReset()
        {
            _alertProgress.Clear();
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

        private void TriggerSound(Vector3 position, float amount)
        {
            if (!SNet.IsMaster) return;

            var node = CourseNodeUtil.GetCourseNode(position, CWC.Owner.DimensionIndex);
            bool runAlert = AlertRadius > WakeUpRadius;
            bool runFakeAlert = FakeAlertRadius > AlertRadius && FakeAlertRadius > WakeUpRadius;

            s_ray.origin = position;

            // Cache enemies
            if (runFakeAlert)
            {
                s_fakeAlertList.Clear();
                s_fakeAlertList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, FakeAlertRadius, 180f, node, SearchSettings));
            }

            if (runAlert)
            {
                foreach (ObjectWrapper<Agent> wrapper in _alertProgress.Keys.Where(key => key.Object == null || !key.Object.Alive).ToList())
                    _alertProgress.Remove(wrapper);
                s_alertList.Clear();
                s_alertList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, AlertRadius, 180f, node, SearchSettings));
            }

            s_wakeupList.Clear();
            s_wakeupList.AddRange(SearchUtil.GetEnemiesInRange(s_ray, WakeUpRadius, 180f, node, SearchSettings));

            // Instant wakeup
            for (int i = s_wakeupList.Count - 1; i >= 0; --i)
            {
                EnemyAgent enemy = s_wakeupList[i];
                if (runAlert)
                    RemoveFromList(enemy, s_alertList);
                if (runFakeAlert)
                    RemoveFromList(enemy, s_fakeAlertList);
                if (!EnemyCanHear(position, node, enemy))
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
                if (!EnemyCanHear(position, node, enemy))
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
                    _alertProgress[TempWrapper] += AlertAmount * amount;
            }

            // Fake pulse
            foreach (var enemy in s_fakeAlertList)
            {
                if (!EnemyCanHear(position, node, enemy))
                    continue;

                enemy.AI.m_locomotion.Hibernate.Heartbeat(0.5f, CWC.Owner.FirePos);
            }
        }

        private bool EnemyCanHear(Vector3 position, AIG_CourseNode node, EnemyAgent enemy)
        {
            if (!enemy.ListenerReady) return false;

            if (CrossDoorMode == CrossDoorMode.None && enemy.CourseNode.NodeID != node.NodeID) return false;

            float weaponDist = enemy.EnemyDetectionData.weaponDetectionDistanceMax;
            if ((position - enemy.Position).sqrMagnitude >= weaponDist * weaponDist) return false;

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

        private void MakeNoise(Vector3 position)
        {
            var noiseData = new pNM_NoiseData()
            {
                position = position,
                radiusMin = WakeUpRadius,
                radiusMax = Math.Max(WakeUpRadius + 0.01f, FakeAlertRadius),
                yScale = 1f,
                includeToNeightbourAreas = CrossDoorMode != CrossDoorMode.None,
                raycastFirstNode = false,
                type = NM_NoiseType.Detectable
            };
            noiseData.noiseMaker.Set(CWC.Owner.Player);
            noiseData.node.Set(CourseNodeUtil.GetCourseNode(position, CWC.Owner.DimensionIndex));

            if (SNet.IsMaster)
                NoiseManager.ReceiveNoise(noiseData);
            else
                NoiseManager.s_noisePacket.Send(noiseData, SNet_ChannelType.GameNonCritical, SNet.Master);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(FakeAlertRadius), FakeAlertRadius);
            writer.WriteNumber(nameof(AlertRadius), AlertRadius);
            writer.WriteNumber(nameof(AlertAmount), AlertAmount);
            writer.WriteNumber(nameof(WakeUpRadius), WakeUpRadius);
            writer.WriteNumber(nameof(SoundID), SoundID);
            writer.WriteString(nameof(CrossDoorMode), CrossDoorMode.ToString());
            writer.WriteBoolean(nameof(UseNoiseSystem), UseNoiseSystem);
            writer.WriteBoolean(nameof(LocalSoundOnly), LocalSoundOnly);
            writer.WriteBoolean(nameof(FollowUser), FollowUser);
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
                case "soundid":
                case "sound":
                    if (reader.TokenType == JsonTokenType.String)
                        SoundID = AkSoundEngine.GetIDFromString(reader.GetString()!);
                    else
                        SoundID = reader.GetUInt32();
                    break;
                case "crossdoormode":
                case "crossdoors":
                case "crossdoor":
                    CrossDoorMode = reader.GetString().ToEnum(CrossDoorMode.NoPenalty);
                    break;
                case "usenoisesystem":
                case "noisesystem":
                    UseNoiseSystem = reader.GetBoolean();
                    break;
                case "localsoundonly":
                case "localsound":
                    LocalSoundOnly = reader.GetBoolean();
                    break;
                case "followuser":
                case "follow":
                    FollowUser = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }

    public enum CrossDoorMode
    {
        NoPenalty,
        None
    }
}

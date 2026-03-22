using Agents;
using AmorLib.Utils;
using Enemies;
using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.ShrapnelHit;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using FX_EffectSystem;
using GameData;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class Shrapnel :
        Effect,
        IWeaponProperty<WeaponCreatedContext>,
        ITriggerCallbackImpactSync,
        IReferenceHolder
    {
        public ushort SyncID { get; set; }

        public int Count { get; private set; } = 0;
        public uint ArchetypeID { get; private set; } = 0;
        public float MaxRange { get; private set; } = 0;
        public float MaxAngle { get; private set; } = 180;
        public float Damage { get; private set; } = 0;
        public Vector2 DamageFalloff { get; private set; } = new Vector2(100, 100);
        public float PrecisionDamageMulti { get; private set; } = 1f;
        public float StaggerDamageMulti { get; private set; } = 1f;
        public float FriendlyDamageMulti { get; private set; } = 1f;
        public int PierceLimit { get; private set; } = 1;
        public FireSetting FireFrom { get; private set; } = FireSetting.HitNormal;
        public ShrapnelFallback WallHandling { get; private set; } = ShrapnelFallback.Avoid;
        public float WallHandlingDist { get; private set; } = 1f;
        public bool IgnoreFalloff { get; private set; } = false;
        public bool DamageLimb { get; private set; } = true;
        public bool IgnoreArmor { get; private set; } = false;
        public bool IgnoreBackstab { get; private set; } = false;
        public bool IgnoreShotMods { get; private set; } = false;
        public bool UseParentShotMod { get; private set; } = true;
        public bool DamageFriendly { get; private set; } = true;
        public bool DamageOwner { get; private set; } = true;
        public bool DamageLocks { get; private set; } = true;
        public PropertyList Properties { get; private set; } = new();
        public bool HitTriggerTarget { get; private set; } = false;
        public bool RunHitTriggers { get; private set; } = true;
        public bool ApplyAttackCooldown { get; private set; } = true;
        public TriggerPosMode ApplyPositionMode { get; private set; } = TriggerPosMode.Relative;
        public bool SearchEnabled { get; private set; } = false;
        public int SearchCap { get; private set; } = 0;
        public int SearchCapPerTarget { get; private set; } = 1;
        public bool SearchOverflow { get; private set; } = true;
        public float SearchRange { get; private set; } = 0f;
        public float SearchAngle { get; private set; } = 0f;
        public float SearchShotMaxAngle { get; private set; } = 0f;
        public TargetingMode SearchTargetMode { get; private set; } = TargetingMode.ClosestLimb;
        public TargetingPriority SearchTargetPriority { get; private set; } = TargetingPriority.Distance;

        private const float WallHitBuffer = 0.03f;

        private CustomShotSettings _shotSettings;
        private int _friendlyMask = 0;

        public Shrapnel()
        {
            _shotSettings = new(hitFunc: DoHit);
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.BulletLanded));
            SetValidTriggers(DamageType.Shrapnel);
        }

        public void Invoke(WeaponCreatedContext context)
        {
            if (ArchetypeID != 0)
            {
                var archBlock = ArchetypeDataBlock.GetBlock(ArchetypeID);
                if (archBlock == null)
                {
                    EWCLogger.Error($"Shrapnel: Unable to find Archetype block with ID {ArchetypeID}!");
                    return;
                }

                Damage = archBlock.Damage;
                DamageFalloff = archBlock.DamageFalloff;
                PrecisionDamageMulti = archBlock.PrecisionDamageMulti;
                StaggerDamageMulti = archBlock.StaggerDamageMulti;
                PierceLimit = archBlock.PierceLimit();
            }
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            foreach (var tContext in contexts)
            {
                ShotInfo? info = null;
                IntPtr ignoreBase = IntPtr.Zero;
                float falloff = 1f;
                Vector3 position;
                Vector3 dir = Vector3.zero;
                Vector3 normal = Vector3.zero;
                if (ApplyPositionMode != TriggerPosMode.User && tContext.context is IPositionContext hitContext)
                {
                    info = hitContext.ShotInfo.Orig;
                    position = hitContext.Position;
                    dir = hitContext.Direction;
                    falloff = hitContext.Falloff;
                    if (hitContext is WeaponHitDamageableContextBase damContext)
                    {
                        if (!HitTriggerTarget)
                            ignoreBase = damContext.Damageable.GetBaseDamagable().Pointer;

                        Agent? agent = damContext.Damageable.GetBaseAgent();
                        if (ApplyPositionMode == TriggerPosMode.Relative && agent != null)
                            position = damContext.LocalPosition + agent.Position;
                    }
                    else
                    {
                        position += hitContext.Normal * WallHitBuffer;
                        normal = hitContext.Normal;
                    }
                }
                else
                {
                    position = CWC.Owner.FirePos;
                }

                dir = ComputeBaseDir(dir, normal);

                if (_shotSettings.projectile != null)
                    TriggerManager.SendInstance(this, position, dir, normal, tContext.triggerAmt);
                DoShrapnel(position, dir, normal, tContext.triggerAmt, falloff, info, ignoreBase);
            }
        }

        public void TriggerApplySync(Vector3 position, Vector3 direction, Vector3 normal, float triggerAmt)
        {
            DoShrapnelVisual(position, direction, normal, triggerAmt);
        }

        public override void TriggerReset() { }
        public void TriggerResetSync() { }

        private bool ForceRandomDir(Vector3 normal)
        {
            return MaxAngle >= 180 || FireFrom switch
            {
                FireSetting.HitPos => false,
                _ => normal == Vector3.zero
            };
        }

        private Vector3 ComputeBaseDir(Vector3 dir, Vector3 normal)
        {
            Vector3 baseDir = dir;
            if (!ForceRandomDir(normal))
            {
                switch (FireFrom)
                {
                    case FireSetting.HitNormal:
                        baseDir = normal;
                        break;
                    case FireSetting.HitReflect:
                        baseDir = Vector3.Reflect(dir, normal);
                        break;
                    case FireSetting.HitPos:
                        baseDir = normal == Vector3.zero ? dir : Vector3.Reflect(dir, normal);
                        break;
                }
            }

            return baseDir;
        }

        private List<Vector3> GenerateRayDirs(Vector3 position, Vector3 dir, Vector3 normal)
        {
            List<Vector3> results;
            if (SearchEnabled && CWC.Owner.IsType(OwnerType.Managed))
            {
                results = GetBestEnemyDirs(position, dir);
                for (int i = 0; i < results.Count; i++)
                    results[i] = CustomShotComponent.CalcRayDir(results[i], 0, 0, SearchShotMaxAngle);
                if (!SearchOverflow)
                    return results;
                results.EnsureCapacity(Count);
            }
            else
                results = new(Count);

            for (int i = results.Count; i < Count; i++)
            {
                Vector3 rayDir;
                if (ForceRandomDir(normal))
                    rayDir = UnityEngine.Random.onUnitSphere;
                else
                    rayDir = CustomShotComponent.CalcRayDir(dir, 0, 0, MaxAngle);

                if (normal != Vector3.zero && Vector3.Dot(rayDir, normal) < 0)
                {
                    if (FireFrom == FireSetting.HitReflect || FireFrom == FireSetting.HitPos)
                        rayDir = Vector3.Reflect(rayDir, normal);
                    else if (FireFrom == FireSetting.HitNormal)
                        rayDir = -rayDir;
                }
                results.Add(rayDir);
            }
            return results;
        }

        private void DoShrapnelVisual(Vector3 position, Vector3 direction, Vector3 normal, float triggerAmt)
        {
            HitData hitData = new(DamageType.Shrapnel)
            {
                owner = CWC.Owner.Player,
                pierceLimit = 1,
                damage = Damage * triggerAmt
            };

            var ray = new Ray(position, direction);
            var rayDirs = GenerateRayDirs(position, direction, normal);
            for (int i = 0; i < Count; i++)
            {
                ray.direction = rayDirs[i];
                CWC.ShotComponent.FireCustom(ray, ray.origin, hitData, shotSettings: _shotSettings);
            }
        }

        private void DoShrapnel(Vector3 position, Vector3 dir, Vector3 normal, float triggerAmt, float falloff, ShotInfo? shotInfo = null, IntPtr ignoreEnt = default)
        {
            var ray = new Ray(position, dir);
            var rayDirs = GenerateRayDirs(position, dir, normal);
            for (int i = 0; i < rayDirs.Count; i++)
            {
                HitData hitData = new(DamageType.Shrapnel);
                hitData.owner = CWC.Owner.Player;
                hitData.shotInfo.Reset(Damage * triggerAmt, PrecisionDamageMulti, StaggerDamageMulti, CWC, shotInfo, UseParentShotMod);
                hitData.falloff = IgnoreFalloff ? 1f : falloff;
                hitData.pierceLimit = PierceLimit;
                hitData.damage = hitData.shotInfo.OrigDamage;
                hitData.damageFalloff = DamageFalloff;
                hitData.staggerMulti = hitData.shotInfo.OrigStagger;
                hitData.precisionMulti = PrecisionDamageMulti;
                hitData.maxRayDist = MaxRange;
                hitData.RayHit = default;

                ray.direction = rayDirs[i];
                switch (WallHandling)
                {
                    case ShrapnelFallback.Avoid:
                        if (CheckNearbyWall(ray, out _))
                            ray.direction = normal != Vector3.zero ? Vector3.Reflect(-ray.direction, normal) : -ray.direction;
                        break;
                    case ShrapnelFallback.Ricochet:
                        if (_shotSettings.projectile != null) break;

                        if (CheckNearbyWall(ray, out var bounceHit))
                        {
                            hitData.maxRayDist = bounceHit.distance;
                            ToggleShotModifiers(false);
                            hitData.pierceLimit = CWC.ShotComponent.FireCustom(ray, ray.origin, hitData, _friendlyMask, ignoreEnt, _shotSettings);
                            ToggleShotModifiers(true);
                            if (hitData.pierceLimit == 0) continue;

                            ray.origin = bounceHit.point + bounceHit.normal * WallHitBuffer;
                            ray.direction = Vector3.Reflect(ray.direction, bounceHit.normal);
                            hitData.maxRayDist = MaxRange - bounceHit.distance;
                        }
                        break;
                    default:
                        break;
                }

                ToggleShotModifiers(false);
                CWC.ShotComponent.FireCustom(ray, ray.origin, hitData, _friendlyMask, ignoreEnt, _shotSettings);
                ToggleShotModifiers(true);
            }
        }

        private bool CheckNearbyWall(Ray ray, out RaycastHit rayHit)
        {
            if (Physics.Raycast(ray, out rayHit, MaxRange, LayerUtil.MaskWorld))
            {
                var dotRange = Vector3.Dot(ray.direction * rayHit.distance, -rayHit.normal);
                return dotRange <= WallHandlingDist;
            }
            return false;
        }

        private void ToggleShotModifiers(bool enable)
        {
            if (!enable)
                _shotSettings.projectile?.SetOverrides(DoProjectileHit, _shotSettings.wallPierce);
            else
                _shotSettings.projectile?.DisableOverrides();
            if (!RunHitTriggers)
                CWC.RunHitTriggers = enable;
        }

        private bool DoHit(IWeaponComp _, HitData hitData) {
            hitData.ResetDamage();

            GameObject gameObject = hitData.RayHit.collider.gameObject;
            var colliderMaterial = gameObject.GetComponent<ColliderMaterial>();
            bool isDecalsAllowed = (LayerUtil.MaskDecalValid & gameObject.gameObject.layer) == 0;

            FX_GroupName impactFX = FX_GroupName.Impact_Concrete;
            if (colliderMaterial != null)
                impactFX = (FX_GroupName)colliderMaterial.MaterialId;
            else if (hitData.damageable?.GetBaseAgent()?.Type == AgentType.Player)
                impactFX = FX_GroupName.Impact_PlayerBody;

            FX_Manager.PlayEffect(false, impactFX, null, hitData.RayHit.point, Quaternion.LookRotation(hitData.RayHit.normal), isDecalsAllowed);
            return ShrapnelHitManager.DoHit(this, hitData, CWC.GetContextController());
        }

        private bool DoProjectileHit(HitData hitData, ContextController cc) => ShrapnelHitManager.DoHit(this, hitData, cc, calcFalloff: false);

        private List<Vector3> GetBestEnemyDirs(Vector3 position, Vector3 dir)
        {
            float range;
            if (_shotSettings.projectile != null)
                range = SearchRange > 0 ? Math.Min(MaxRange, SearchRange) : MaxRange;
            else
                range = Math.Max(MaxRange, SearchRange);
            var angle = SearchAngle > 0 ? Math.Min(MaxAngle, SearchAngle) : MaxAngle;

            List<(EnemyAgent enemy, RaycastHit hit)> enemyHits;
            if (SearchTargetMode == TargetingMode.ClosestLimb)
            {
                SearchSetting settings = SearchSetting.ClosestHit | SearchSetting.CheckLOS;
                SearchUtil.SightBlockLayer = LayerUtil.MaskWorld;
                enemyHits = SearchUtil.GetEnemyHitsInRange(new(position, dir), range, angle, CourseNodeUtil.GetCourseNode(position, CWC.Owner.DimensionIndex), settings);
            }
            else
            {
                var enemies = SearchUtil.GetEnemiesInRange(new(position, dir), range, angle, CourseNodeUtil.GetCourseNode(position, CWC.Owner.DimensionIndex));
                enemyHits = enemies.ConvertAll(enemy => (enemy, default(RaycastHit)));
            }

            Vector3 GetTargetPos((EnemyAgent enemy, RaycastHit hit) pair) => SearchTargetMode switch
            {
                TargetingMode.Body => pair.enemy.AimTargetBody.position,
                TargetingMode.ClosestLimb => pair.hit.point,
                _ => pair.enemy.AimTarget.position,
            };

            bool TryGetClosestWeakspotPos((EnemyAgent enemy, RaycastHit hit) pair, out Vector3 pos)
            {
                float minDist = float.MaxValue;
                pos = default;
                foreach (var limb in pair.enemy.Damage.DamageLimbs)
                {
                    if (limb.TryGetComp<Collider>(out var collider) && collider.enabled && limb.m_type == eLimbDamageType.Weakspot)
                    {
                        float dist = (limb.DamageTargetPos - position).magnitude;
                        if (minDist > dist && !Physics.Linecast(position, limb.DamageTargetPos, LayerUtil.MaskWorld))
                        {
                            minDist = dist;
                            pos = limb.DamageTargetPos;
                        }
                    }
                }
                return minDist < float.MaxValue;
            }

            switch (SearchTargetPriority)
            {
                case TargetingPriority.Distance:
                    var distList = enemyHits.ConvertAll(pair => (pair, (GetTargetPos(pair) - position).sqrMagnitude));
                    distList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(distList, enemyHits);
                    break;
                case TargetingPriority.Health:
                    // Since we prefer higher HealthMax, need to invert angles so the reverse gets the right order
                    var healthList = enemyHits.ConvertAll(pair => (pair, pair.enemy.Damage.HealthMax, 180f - Vector3.Angle(dir, GetTargetPos(pair) - position)));
                    healthList.Sort(SortUtil.FloatTuple);
                    healthList.Reverse();
                    SortUtil.CopySortedList(healthList, enemyHits);
                    break;
                default:
                    var angleList = enemyHits.ConvertAll(pair => (pair, Vector3.Angle(dir, GetTargetPos(pair) - position)));
                    angleList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(angleList, enemyHits);
                    break;
            }

            var maxCount = Count;
            if (SearchCap > 0 && SearchCap < maxCount)
                maxCount = SearchCap;
            List<Vector3> targets = new(Math.Min(maxCount, enemyHits.Count * SearchCapPerTarget));
            for (int i = 0; i < enemyHits.Count && targets.Count < maxCount; i++)
            {
                var pair = enemyHits[i];
                if (SearchTargetMode == TargetingMode.Weakspot)
                {
                    if (TryGetClosestWeakspotPos(pair, out var pos))
                        targets.Add((pos - position).normalized);
                }
                else
                {
                    Vector3 pos = GetTargetPos(pair);
                    if (SearchTargetMode == TargetingMode.ClosestLimb || !Physics.Linecast(position, pos, LayerUtil.MaskWorld))
                        targets.Add((pos - position).normalized);
                }
            }

            if (SearchCapPerTarget > 0 && SearchCapPerTarget * targets.Count < maxCount)
                maxCount = SearchCapPerTarget * targets.Count;
            if (targets.Count == maxCount)
                return targets;

            var targetCount = targets.Count;
            for (int i = 0; targets.Count < maxCount; i++)
                targets.Add(targets[i % targetCount]);
            return targets;
        }

        public void OnReferenceSet(WeaponPropertyBase property) => CheckAndSetTrait(property);

        public override WeaponPropertyBase Clone()
        {
            var copy = (Shrapnel)base.Clone();
            copy.Properties = Properties.Clone();
            foreach (var prop in copy.Properties.Properties)
                copy.CheckAndSetTrait(prop);
            copy._friendlyMask = _friendlyMask;
            return copy;
        }

        protected override void OnCWCSet()
        {
            foreach (var property in Properties.Properties)
                property.CWC = CWC;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Count), Count);
            writer.WriteNumber(nameof(MaxRange), MaxRange);
            writer.WriteNumber(nameof(MaxAngle), MaxAngle);
            writer.WriteNumber(nameof(ArchetypeID), ArchetypeID);
            writer.WriteNumber(nameof(Damage), Damage);
            SerializeFalloff(writer);
            writer.WriteNumber(nameof(PrecisionDamageMulti), PrecisionDamageMulti);
            writer.WriteNumber(nameof(StaggerDamageMulti), StaggerDamageMulti);
            writer.WriteNumber(nameof(FriendlyDamageMulti), FriendlyDamageMulti);
            writer.WriteNumber(nameof(PierceLimit), PierceLimit);
            writer.WriteString(nameof(WallHandling), WallHandling.ToString());
            writer.WriteNumber(nameof(WallHandlingDist), WallHandlingDist);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreShotMods), IgnoreShotMods);
            writer.WriteBoolean(nameof(UseParentShotMod), UseParentShotMod);
            writer.WriteBoolean(nameof(DamageFriendly), DamageFriendly);
            writer.WriteBoolean(nameof(DamageOwner), DamageOwner);
            writer.WriteBoolean(nameof(DamageLocks), DamageLocks);
            EWCJson.Serialize(writer, "Traits", Properties);
            writer.WriteBoolean(nameof(HitTriggerTarget), HitTriggerTarget);
            writer.WriteBoolean(nameof(RunHitTriggers), RunHitTriggers);
            writer.WriteBoolean(nameof(ApplyAttackCooldown), ApplyAttackCooldown);
            writer.WriteString(nameof(ApplyPositionMode), ApplyPositionMode.ToString());
            writer.WriteBoolean(nameof(SearchEnabled), SearchEnabled);
            writer.WriteNumber(nameof(SearchCap), SearchCap);
            writer.WriteNumber(nameof(SearchCapPerTarget), SearchCapPerTarget);
            writer.WriteBoolean(nameof(SearchOverflow), SearchOverflow);
            writer.WriteNumber(nameof(SearchRange), SearchRange);
            writer.WriteNumber(nameof(SearchAngle), SearchAngle);
            writer.WriteNumber(nameof(SearchShotMaxAngle), SearchShotMaxAngle);
            writer.WriteString(nameof(SearchTargetMode), SearchTargetMode.ToString());
            writer.WriteString(nameof(SearchTargetPriority), SearchTargetPriority.ToString());
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        private void SerializeFalloff(Utf8JsonWriter writer)
        {
            writer.WritePropertyName(nameof(DamageFalloff));
            writer.WriteStartObject();
            writer.WriteNumber(nameof(DamageFalloff.x), DamageFalloff.x);
            writer.WriteNumber(nameof(DamageFalloff.y), DamageFalloff.y);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "count":
                    Count = Math.Max(0, reader.GetInt32());
                    break;
                case "maxrange":
                case "range":
                    MaxRange = reader.GetSingle();
                    break;
                case "maxangle":
                case "angle":
                    MaxAngle = reader.GetSingle();
                    break;
                case "archetypeid":
                case "archetype":
                case "archid":
                case "arch":
                    ArchetypeID = EWCJson.Deserialize<uint>(ref reader);
                    break;
                case "damage":
                    Damage = reader.GetSingle();
                    break;
                case "damagefalloff":
                    DamageFalloff = EWCJson.Deserialize<Vector2>(ref reader);
                    break;
                case "precisiondamagemulti":
                case "precisionmulti":
                case "precisionmult":
                case "precision":
                    PrecisionDamageMulti = reader.GetSingle();
                    break;
                case "staggerdamagemulti":
                case "staggermulti":
                case "staggermult":
                case "stagger":
                    StaggerDamageMulti = reader.GetSingle();
                    break;
                case "friendlydamagemulti":
                case "friendlymulti":
                case "friendlymult":
                    FriendlyDamageMulti = reader.GetSingle();
                    break;
                case "piercelimit":
                case "piercingdamagecountlimit":
                case "pierce":
                    PierceLimit = reader.GetInt32();
                    break;
                case "firefrom":
                    FireFrom = reader.GetString().ToEnum(FireSetting.HitNormal);
                    if (FireFrom == FireSetting.User)
                        FireFrom = FireSetting.HitNormal;
                    break;
                case "wallhandling":
                    WallHandling = reader.GetString().ToEnum(ShrapnelFallback.None);
                    break;
                case "wallhandlingdist":
                case "walldist":
                    WallHandlingDist = reader.GetSingle();
                    break;
                case "ignorefalloff":
                    IgnoreFalloff = reader.GetBoolean();
                    break;
                case "damagelimb":
                    DamageLimb = reader.GetBoolean();
                    break;
                case "ignorearmor":
                    IgnoreArmor = reader.GetBoolean();
                    break;
                case "ignorebackstab":
                case "ignorebackdamage":
                case "ignorebackbonus":
                    IgnoreBackstab = reader.GetBoolean();
                    break;
                case "ignoredamagemods":
                case "ignoredamagemod":
                case "ignoreshotmods":
                case "ignoreshotmod":
                    IgnoreShotMods = reader.GetBoolean();
                    break;
                case "useparentshotmod":
                case "parentshotmod":
                    UseParentShotMod = reader.GetBoolean();
                    break;
                case "damagefriendly":
                case "friendlyfire":
                    DamageFriendly = reader.GetBoolean();
                    _friendlyMask |= LayerUtil.MaskFriendly;
                    break;
                case "damageowner":
                case "damageuser":
                    DamageOwner = reader.GetBoolean();
                    _friendlyMask |= LayerUtil.MaskOwner;
                    break;
                case "damagelocks":
                    DamageLocks = reader.GetBoolean();
                    break;
                case "traits":
                    Properties = EWCJson.Deserialize<PropertyList>(ref reader)!;
                    Properties.Properties.RemoveAll(prop => !CheckAndSetTrait(prop));
                    break;
                case "hittriggertarget":
                    HitTriggerTarget = reader.GetBoolean();
                    break;
                case "runhittriggers":
                case "hittriggers":
                    RunHitTriggers = reader.GetBoolean();
                    break;
                case "applyattackcooldowns":
                case "applyattackcooldown":
                    ApplyAttackCooldown = reader.GetBoolean();
                    break;
                case "applypositionmode":
                    ApplyPositionMode = reader.GetString()!.ToEnum(TriggerPosMode.Relative);
                    break;
                case "applyonuser":
                    ApplyPositionMode = TriggerPosMode.User;
                    break;
                case "searchenabled":
                case "search":
                    SearchEnabled = reader.GetBoolean();
                    break;
                case "searchcap":
                    SearchCap = Math.Max(0, reader.GetInt32());
                    break;
                case "searchcappertarget":
                    SearchCapPerTarget = Math.Max(1, reader.GetInt32());
                    break;
                case "searchoverflow":
                    SearchOverflow = reader.GetBoolean();
                    break;
                case "searchrange":
                    SearchRange = reader.GetSingle();
                    break;
                case "searchangle":
                    SearchAngle = reader.GetSingle();
                    break;
                case "searchshotmaxangle":
                case "searchshotangle":
                    SearchShotMaxAngle = reader.GetSingle();
                    break;
                case "targetingmode":
                case "targetmode":
                    SearchTargetMode = reader.GetString().ToEnum(SearchTargetMode);
                    break;
                case "targetingpriority":
                case "targetpriority":
                    SearchTargetPriority = reader.GetString().ToEnum(SearchTargetPriority);
                    break;
            }
        }

        private bool CheckAndSetTrait(WeaponPropertyBase property)
        {
            if (property is ThickBullet thick)
                _shotSettings.thickBullet = thick;
            else if (property is WallPierce pierce)
                _shotSettings.wallPierce = pierce;
            else if (property is Traits.Projectile projectile)
                _shotSettings.projectile = projectile;
            else if (property is not ReferenceProperty)
            {
                EWCLogger.Warning($"Shrapnel has trait of type {property.GetType().Name}, expected any of [{typeof(ThickBullet).Name}, {typeof(WallPierce).Name}, {typeof(Traits.Projectile)}]");
                return false;
            }
            return true;
        }
    }

    public enum ShrapnelFallback
    {
        None,
        Avoid,
        Ricochet
    }
}

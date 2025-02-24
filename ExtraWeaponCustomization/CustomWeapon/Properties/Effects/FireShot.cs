using EWC.API;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using EWC.Utils.Log;
using FX_EffectSystem;
using GameData;
using Gear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class FireShot :
        Effect,
        ITriggerCallbackBasicSync,
        ITriggerCallbackDirSync,
        IGunProperty
    {
        public ushort SyncID { get; set; }

        public readonly List<float> Offsets = new(2);
        public uint Repeat { get; private set; } = 0;
        public float Spread { get; private set; } = 0;
        public bool ForceSingleBullet { get; private set; } = false;
        public FireSetting FireFrom { get; private set; } = FireSetting.User;
        public bool DamageFriendly { get; private set; } = true;
        public bool DamageOwner { get; private set; } = false;
        public bool HitTriggerTarget { get; private set; } = false;
        public bool RunHitTriggers { get; private set; } = true;

        private static WallPierce? _wallPierce;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private static Vector3 s_baseDir;
        private readonly HashSet<IntPtr> _hitEnts = new();
        private IntPtr _ignoreEnt = IntPtr.Zero;
        private readonly static HitData s_hitData = new();
        private const float WallHitBuffer = -0.03f;

        public override void TriggerReset() {}
        public void TriggerResetSync() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            _ignoreEnt = IntPtr.Zero;
            float iterations = 0;
            List<(Vector3 pos, Vector3 dir, float amount, IDamageable? damBase)>? hitContexts = null;
            if (FireFrom != FireSetting.User)
            {
                hitContexts = new(5);
                foreach (var context in contexts)
                {
                    if (context.context is WeaponHitContextBase hitContext)
                    {
                        if (context.triggerAmt < 1f) continue;

                        IDamageable? damBase = null;
                        Vector3 position = hitContext.Position;
                        if (hitContext is WeaponHitDamageableContextBase damContext)
                        {
                            damBase = damContext.Damageable.GetBaseDamagable();
                            Agents.Agent? agent = damContext.Damageable.GetBaseAgent();
                            if (agent != null)
                                position = damContext.LocalPosition + agent.Position;
                        }
                        else
                            position += hitContext.Direction * WallHitBuffer;
                        Vector3 direction = FireFrom switch {
                            FireSetting.HitNormal => hitContext.Normal,
                            FireSetting.HitReflect => Vector3.Reflect(hitContext.Direction, hitContext.Normal),
                            _ => hitContext.Direction
                        };
                        hitContexts.Add((position, direction, context.triggerAmt, damBase));
                    }
                    else
                        iterations += context.triggerAmt;
                }
            }
            else
                iterations = contexts.Sum(context => context.triggerAmt);

            if (iterations == 0 && (hitContexts == null || hitContexts.Count == 0)) return;

            _wallPierce = CWC.GetTrait<WallPierce>();

            Ray lastRay = Weapon.s_ray;
            bool isShotgun = CWC.Gun!.TryCast<Shotgun>() != null;

            int shotgunBullets = 1;
            int coneSize = 0;
            float segmentSize = 0;
            var archData = CWC.Weapon.ArchetypeData;
            if (isShotgun && !ForceSingleBullet)
            {
                shotgunBullets = archData.ShotgunBulletCount;
                coneSize = archData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float spread = Spread;
            if (spread < 0f)
            {
                if (isShotgun)
                    spread = archData.ShotgunBulletSpread;
                else
                    spread = CWC.Weapon.FPItemHolder.ItemAimTrigger ? archData.AimSpread : archData.HipFireSpread;
            }

            bool runFX = CWC.Invoke(new WeaponCancelTracerContext()).Allow;

            s_ray.origin = CWC.Weapon.Owner.FPSCamera.Position;
            s_baseDir = CWC.Gun!.Owner.FPSCamera.CameraRayDir;
            for (int iter = 0; iter < iterations; iter++)
                FirePerTrigger(spread, shotgunBullets, segmentSize, coneSize, false, runFX);

            if (FireFrom != FireSetting.User)
            {
                foreach (var (pos, dir, amount, baseDam) in hitContexts!)
                {
                    s_ray.origin = pos;
                    s_baseDir = dir;
                    if (!HitTriggerTarget && baseDam != null)
                        _ignoreEnt = baseDam.Pointer;
                    FirePerTrigger(spread, shotgunBullets, segmentSize, coneSize, true, runFX);
                    TriggerManager.SendInstance(this, pos, dir, amount);
                }
            }
            Weapon.s_ray = lastRay;

            TriggerManager.SendInstance(this, iterations);
        }

        public void TriggerApplySync(Vector3 position, Vector3 direction, float iterations)
        {
            if (CWC.HasTrait<Traits.Projectile>()) return;

            s_ray.origin = position;
            s_baseDir = direction;
            bool isShotgun = CWC.Gun!.TryCast<ShotgunSynced>() != null;

            int shotgunBullets = 1;
            int coneSize = 0;
            float segmentSize = 0;
            if (isShotgun && !ForceSingleBullet)
            {
                shotgunBullets = CWC.Weapon.ArchetypeData.ShotgunBulletCount;
                coneSize = CWC.Weapon.ArchetypeData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float spread = Spread;
            if (spread < 0f)
                spread = isShotgun ? CWC.Weapon.ArchetypeData.ShotgunBulletSpread : 0f;

            bool runFX = CWC.Invoke(new WeaponCancelTracerContext()).Allow;

            for (int iter = 0; iter < iterations; iter++)
                FirePerTrigger(spread, shotgunBullets, segmentSize, coneSize, true, runFX);
        }

        public void TriggerApplySync(float iterations)
        {
            TriggerApplySync(CWC.Weapon.MuzzleAlign.position, CWC.Weapon.MuzzleAlign.forward, iterations);
        }

        private void FirePerTrigger(float spread, int shotgunBullets, float segmentSize, float coneSize, bool visual, bool runFX)
        {
            for (uint mod = 1; mod <= Repeat + 1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod;
                    float y = -Offsets[i + 1] * mod;
                    if (visual)
                        FireVisual(x, y, spread, runFX);
                    else
                        Fire(x, y, spread, runFX);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
                        if (visual)
                            FireVisual(x + coneSize * (float)Math.Cos(angle), y + coneSize * (float)Math.Sin(angle), spread, runFX);
                        else
                            Fire(x + coneSize * (float)Math.Cos(angle), y + coneSize * (float)Math.Sin(angle), spread, runFX);
                    }
                }
            }
        }

        private void Fire(float x, float y, float spread, bool runFX)
        {
            if (!RunHitTriggers)
                CWC.RunHitTriggers = false;

            ArchetypeDataBlock archData = CWC.Weapon.ArchetypeData;
            s_hitData.owner = CWC.Weapon.Owner;
            s_hitData.damage = archData.Damage;
            s_hitData.damageFalloff = archData.DamageFalloff;
            s_hitData.staggerMulti = archData.StaggerDamageMulti;
            s_hitData.precisionMulti = archData.PrecisionDamageMulti;
            s_hitData.maxRayDist = CWC.Gun!.MaxRayDist;
            s_hitData.shotInfo.Reset();
            s_hitData.RayHit = default;

            _hitEnts.Add(_ignoreEnt);
            CalcRayDir(x, y, spread);

            FireShotAPI.FirePreShotFiredCallback(s_hitData, s_ray);

            // Stops at padlocks but that's the same behavior as vanilla so idc
            Vector3 wallPos;
            bool hitWall;
            if ((hitWall = Physics.Raycast(s_ray, out RaycastHit wallRayHit, s_hitData.maxRayDist, LayerUtil.MaskWorld)) && _wallPierce == null)
                wallPos = wallRayHit.point;
            else
                wallPos = s_ray.origin + s_ray.direction * s_hitData.maxRayDist;

            WeaponPostRayContext context = new(s_hitData, s_ray.origin, hitWall, _ignoreEnt);
            CWC.Invoke(context);
            if (!context.Result)
            {
                if (!RunHitTriggers)
                    CWC.RunHitTriggers = true;
                _hitEnts.Clear();
                FireShotAPI.FireShotFiredCallback(s_hitData, s_ray.origin, wallPos);

                if (!runFX) return;

                // Plays bullet FX, since Thick Bullet will cancel the ray but doesn't
                FX_Manager.EffectTargetPosition = wallPos;
                FX_Manager.PlayLocalVersion = false;
                BulletWeapon.s_tracerPool.AquireEffect().Play(null, CWC.Weapon.MuzzleAlign.position, Quaternion.LookRotation(s_ray.direction));
                return;
            }

            FX_Manager.EffectTargetPosition = wallPos;
            int pierceCount = archData.PiercingBullets ? archData.PiercingDamageCountLimit : 1;
            float maxDist = (s_ray.origin - wallPos).magnitude;
            int mask = LayerUtil.MaskEnemy;
            if (DamageFriendly) mask |= LayerUtil.MaskFriendly;
            if (DamageOwner) mask |= LayerUtil.MaskOwner;

            CheckForHits(pierceCount, maxDist, mask);

            if (pierceCount > 0)
            {
                if (_wallPierce == null)
                    CheckForHits(pierceCount, maxDist, LayerUtil.MaskDynamic);

                if (pierceCount > 0 && hitWall && !AlreadyHit(DamageableUtil.GetDamageableFromRayHit(wallRayHit)))
                {
                    s_hitData.RayHit = wallRayHit;
                    FX_Manager.EffectTargetPosition = wallPos;
                    ShotManager.BulletHit(s_hitData);
                }
            }

            FireShotAPI.FireShotFiredCallback(s_hitData, s_ray.origin, FX_Manager.EffectTargetPosition);
            _hitEnts.Clear();
            if (!RunHitTriggers)
                CWC.RunHitTriggers = true;

            if (!runFX) return;

            FX_Manager.PlayLocalVersion = false;
            BulletWeapon.s_tracerPool.AquireEffect().Play(null, CWC.Weapon.MuzzleAlign.position, Quaternion.LookRotation(s_ray.direction));
        }

        private void FireVisual(float x, float y, float spread, bool runFX)
        {
            s_hitData.owner = CWC.Weapon.Owner;
            s_hitData.damage = CWC.Weapon.ArchetypeData.Damage;

            CalcRayDir(x, y, spread, local: false);
            FireShotAPI.FirePreShotFiredCallback(s_hitData, s_ray);

            if (Physics.Raycast(s_ray, out s_rayHit, 20f, LayerUtil.MaskEntityAndWorld))
            {
                FX_Manager.EffectTargetPosition = s_rayHit.point;
                s_hitData.RayHit = s_rayHit;
                BulletWeapon.BulletHit(s_hitData.ToWeaponHitData(), false);
            }
            else
                FX_Manager.EffectTargetPosition = s_ray.origin + s_ray.direction * 20f;

            FireShotAPI.FireShotFiredCallback(s_hitData, s_ray.origin, FX_Manager.EffectTargetPosition);
            
            if (!runFX) return;

            FX_Manager.PlayLocalVersion = false;
            BulletWeapon.s_tracerPool.AquireEffect().Play(null, CWC.Weapon.MuzzleAlign.position, Quaternion.LookRotation(s_ray.direction));
        }

        private void CalcRayDir(float x, float y, float spread, bool local = true)
        {
            Transform cameraTransform = local ? CWC.Weapon.Owner.FPSCamera.transform : CWC.Weapon.transform;
            s_hitData.fireDir = s_baseDir;
            Vector3 up = cameraTransform.up;
            Vector3 right = cameraTransform.right;
            if (x != 0)
                s_hitData.fireDir = Quaternion.AngleAxis(x, up) * s_hitData.fireDir;
            if (y != 0)
                s_hitData.fireDir = Quaternion.AngleAxis(y, right) * s_hitData.fireDir;
            if (spread != 0)
            {
                Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
                insideUnitCircle *= spread;
                s_hitData.fireDir = Quaternion.AngleAxis(insideUnitCircle.x, up) * s_hitData.fireDir;
                s_hitData.fireDir = Quaternion.AngleAxis(insideUnitCircle.y, right) * s_hitData.fireDir;
            }
            s_ray.direction = s_hitData.fireDir;
            Weapon.s_ray = s_ray;
        }

        // Although BulletHit does verify damage search ID, it generates hit FX for every pierced limb.
        private bool AlreadyHit(IDamageable? damageable)
        {
            if (damageable == null) return false;
            return !_hitEnts.Add(damageable.GetBaseDamagable().Pointer);
        }

        private void CheckForHits(int pierceCount, float maxDist, int layer)
        {
            if (pierceCount == 0) return;

            RaycastHit[]? hits = null;
            if (pierceCount == 1)
            {
                if (Physics.Raycast(s_ray, out s_rayHit, maxDist, layer))
                {
                    hits = new RaycastHit[1];
                    hits[0] = s_rayHit;
                }
            }
            else
                hits = Physics.RaycastAll(s_ray, maxDist, layer);

            if (hits == null || hits.Length == 0) return;

            Array.Sort(hits, SortUtil.Rayhit);
            foreach (var hit in hits)
            {
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(hit);
                if (damageable == null) continue;
                if (AlreadyHit(damageable)) continue;
                if (_wallPierce?.IsTargetReachable(s_hitData.owner.CourseNode, damageable.GetBaseAgent()?.CourseNode) == false) continue;

                s_hitData.RayHit = hit;
                FX_Manager.EffectTargetPosition = hit.point;
                if (ShotManager.BulletHit(s_hitData))
                    pierceCount--;

                if (pierceCount == 0) return;
            }
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (FireShot)base.Clone();
            copy.Offsets.AddRange(Offsets);
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WritePropertyName(nameof(Offsets));
            EWCJson.Serialize(writer, Offsets);
            writer.WriteNumber(nameof(Repeat), Repeat);
            writer.WriteNumber(nameof(Spread), Spread);
            writer.WriteBoolean(nameof(ForceSingleBullet), ForceSingleBullet);
            writer.WriteString(nameof(FireFrom), FireFrom.ToString());
            writer.WriteBoolean(nameof(HitTriggerTarget), HitTriggerTarget);
            writer.WriteBoolean(nameof(RunHitTriggers), RunHitTriggers);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "offsets":
                    List<float>? offsets = ReadOffsets(ref reader);
                    if (offsets == null) return;
                    if (offsets.Count % 2 != 0)
                        offsets.RemoveAt(offsets.Count - 1);
                    Offsets.AddRange(offsets);
                    break;
                case "repeats":
                case "repeat":
                    Repeat = reader.GetUInt32();
                    break;
                case "spread":
                    Spread = reader.GetSingle();
                    break;
                case "applyspreadpershot":
                case "applyspread":
                    if (reader.GetBoolean())
                    {
                        EWCLogger.Warning("FireShot field \"ApplySpreadPerShot\" is deprecated. Please use \"Spread\" instead.");
                        Spread = -1f;
                    }
                    break;
                case "forcesinglebullet":
                case "singlebullet":
                    ForceSingleBullet = reader.GetBoolean();
                    break;
                case "firefrom":
                    FireFrom = reader.GetString().ToEnum(FireSetting.User);
                    break;
                case "firefromhitpos":
                    if (reader.GetBoolean())
                    {
                        FireFrom = FireSetting.HitPos;
                        Utils.Log.EWCLogger.Warning("FireShot field \"FireFromHitPos\" is deprecated and will be removed in the future. Use \"FireFrom\" instead.");
                    }
                    break;
                case "damagefriendly":
                case "friendlyfire":
                    DamageFriendly = reader.GetBoolean();
                    break;
                case "damageowner":
                case "damageuser":
                    DamageOwner = reader.GetBoolean();
                    break;
                case "hittriggertarget":
                    HitTriggerTarget = reader.GetBoolean();
                    break;
                case "runhittriggers":
                case "hittriggers":
                    RunHitTriggers = reader.GetBoolean();
                    break;
            }
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
    }

    public enum FireSetting
    {
        User,
        HitPos,
        HitNormal,
        HitReflect
    }
}

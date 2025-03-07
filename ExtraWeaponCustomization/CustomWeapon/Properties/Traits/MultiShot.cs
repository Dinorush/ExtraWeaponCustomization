﻿using EWC.API;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Log;
using FX_EffectSystem;
using GameData;
using Gear;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class MultiShot :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponCancelRayContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
    {
        public readonly List<float> Offsets = new(2);
        public float AimOffsetMod { get; private set; } = 1f;
        public uint Repeat { get; private set; } = 0;
        public bool UseAimDir { get; private set; } = false;
        public float Spread { get; private set; } = 0f;
        public bool CancelShot { get; private set; } = false;
        public bool ForceSingleBullet { get; private set; } = false;
        public bool RunHitTriggers { get; private set; } = true;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private static Vector3 s_baseDir;
        private readonly static HashSet<IntPtr> s_hitEnts = new();
        private readonly static HitData s_hitData = new();
        private static WallPierce? s_wallPierce;
        private static float s_initialShotMod = 1f;

        public void Invoke(WeaponPreFireContext context)
        {
            if (Clock.Time - CWC.Gun!.m_lastFireTime > CWC.Gun!.m_fireRecoilCooldown)
                s_initialShotMod = 0.2f;
            else
                s_initialShotMod = 1f;
        }

        public void Invoke(WeaponCancelRayContext context)
        {
            context.Result &= !CancelShot;
        }

        public void Invoke(WeaponCancelTracerContext context)
        {
            context.Allow &= !CancelShot;
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            if (CWC.HasTrait<Projectile>()) return;

            s_ray.origin = CWC.Weapon.MuzzleAlign.position;
            bool isShotgun = CWC.Gun!.TryCast<ShotgunSynced>() != null;
            s_baseDir = UseAimDir || isShotgun ? CWC.Weapon.MuzzleAlign.forward : Weapon.s_weaponRayData.fireDir;

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

            // CancelShot will cancel tracer FX if we don't disable it here
            bool cancelShot = CancelShot;
            CancelShot = false;
            bool runFX = CWC.Invoke(new WeaponCancelTracerContext()).Allow;
            CancelShot = cancelShot;

            for (uint mod = 1; mod <= Repeat + 1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod;
                    float y = -Offsets[i + 1] * mod;
                    FireVisual(x, y, spread, runFX);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
                        FireVisual(x + coneSize * (float)Math.Cos(angle), y + coneSize * (float)Math.Sin(angle), spread, runFX);
                    }
                }
            }
        }

        public void Invoke(WeaponPostFireContext context)
        {
            s_wallPierce = CWC.GetTrait<WallPierce>();

            s_ray.origin = CWC.Weapon.Owner.FPSCamera.Position;
            bool isShotgun = CWC.Gun!.TryCast<Shotgun>() != null;
            s_baseDir = UseAimDir || isShotgun ? CWC.Weapon.Owner.FPSCamera.CameraRayDir : Weapon.s_weaponRayData.fireDir;

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

            bool isADS = CWC.Weapon.FPItemHolder.ItemAimTrigger;
            float spread = Spread;
            if (spread < 0f)
            {
                if (isShotgun)
                    spread = archData.ShotgunBulletSpread;
                else
                    spread = (isADS ? archData.AimSpread : archData.HipFireSpread) * s_initialShotMod;
            }

            // CancelShot will cancel tracer FX if we don't disable it here
            bool cancelShot = CancelShot;
            CancelShot = false;
            bool runFX = CWC.Invoke(new WeaponCancelTracerContext()).Allow;
            CancelShot = cancelShot;

            float aimMod = isADS ? AimOffsetMod : 1f;
            for (uint mod = 1; mod <= Repeat+1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod * aimMod;
                    float y = -Offsets[i+1] * mod * aimMod;
                    Fire(x, y, spread, runFX);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
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
            s_hitData.RayHit = default;
            s_hitData.shotInfo.Reset();

            s_hitEnts.Clear();
            CalcRayDir(x, y, spread);

            FireShotAPI.FirePreShotFiredCallback(s_hitData, s_ray);

            // Stops at padlocks but that's the same behavior as vanilla so idc
            Vector3 wallPos;
            bool hitWall;
            if ((hitWall = Physics.Raycast(s_ray, out RaycastHit wallRayHit, s_hitData.maxRayDist, LayerUtil.MaskWorld)) && s_wallPierce == null)
                wallPos = wallRayHit.point;
            else
                wallPos = s_ray.origin + s_ray.direction * s_hitData.maxRayDist;

            WeaponPostRayContext context = new(s_hitData, s_ray.origin, hitWall);
            CWC.Invoke(context);
            if (!context.Result)
            {
                if (!RunHitTriggers)
                    CWC.RunHitTriggers = true;

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
            CheckForHits(pierceCount, maxDist, LayerUtil.MaskEntity3P);

            if (pierceCount > 0)
            {
                if (s_wallPierce == null)
                    CheckForHits(pierceCount, maxDist, LayerUtil.MaskDynamic);
                
                if (pierceCount > 0 && hitWall && !AlreadyHit(DamageableUtil.GetDamageableFromRayHit(wallRayHit)))
                {
                    s_hitData.RayHit = wallRayHit;
                    FX_Manager.EffectTargetPosition = wallPos; // Set again since CheckForHits sets it to other things
                    ShotManager.BulletHit(s_hitData);
                }
            }

            if (!RunHitTriggers)
                CWC.RunHitTriggers = true;

            FireShotAPI.FireShotFiredCallback(s_hitData, s_ray.origin, FX_Manager.EffectTargetPosition);

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
            return !s_hitEnts.Add(damageable.GetBaseDamagable().Pointer);
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
                if (s_wallPierce?.IsTargetReachable(s_hitData.owner.CourseNode, damageable.GetBaseAgent()?.CourseNode) == false) continue;

                s_hitData.RayHit = hit;
                FX_Manager.EffectTargetPosition = hit.point;
                if (ShotManager.BulletHit(s_hitData))
                    pierceCount--;

                if (pierceCount == 0) return;
            }
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (MultiShot)base.Clone();
            copy.Offsets.AddRange(Offsets);
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WritePropertyName(nameof(Offsets));
            EWCJson.Serialize(writer, Offsets);
            writer.WriteNumber(nameof(AimOffsetMod), AimOffsetMod);
            writer.WriteNumber(nameof(Repeat), Repeat);
            writer.WriteBoolean(nameof(UseAimDir), UseAimDir);
            writer.WriteNumber(nameof(Spread), Spread);
            writer.WriteBoolean(nameof(CancelShot), CancelShot);
            writer.WriteBoolean(nameof(ForceSingleBullet), ForceSingleBullet);
            writer.WriteBoolean(nameof(RunHitTriggers), RunHitTriggers);
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
                case "aimoffsetmod":
                case "aimmod":
                    AimOffsetMod = reader.GetSingle();
                    break;
                case "repeats":
                case "repeat":
                    Repeat = reader.GetUInt32();
                    break;
                case "useaimdir":
                case "ignorespread":
                    UseAimDir = reader.GetBoolean();
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
                case "cancelnormalshot":
                case "cancelshot":
                    CancelShot = reader.GetBoolean();
                    break;
                case "forcesinglebullet":
                case "singlebullet":
                    ForceSingleBullet = reader.GetBoolean();
                    break;
                case "runhittriggers":
                case "hittriggers":
                    RunHitTriggers = reader.GetBoolean();
                    break;
                default:
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
}

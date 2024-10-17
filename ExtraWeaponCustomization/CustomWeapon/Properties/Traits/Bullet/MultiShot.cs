﻿using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
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
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
    {
        public readonly List<float> Offsets = new(2);
        public float AimOffsetMod { get; private set; } = 1f;
        public uint Repeat { get; private set; } = 0;
        public bool IgnoreSpread { get; private set; } = false;
        public bool ApplySpreadPerShot { get; private set; } = false;
        public bool ForceSingleBullet { get; private set; } = false;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private static Vector3 s_baseDir;
        private readonly static HashSet<IntPtr> s_hitEnts = new();
        private readonly static HitData s_hitData = new();
        private static bool s_wallPierce = false;
        private static bool s_projectile = false;

        public void Invoke(WeaponPostFireContextSync context)
        {
            s_projectile = CWC.HasTrait(typeof(Projectile));

            s_ray.origin = Weapon.s_ray.origin;
            s_baseDir = IgnoreSpread ? CWC.Weapon.MuzzleAlign.forward : Weapon.s_weaponRayData.fireDir;
            bool isShotgun = !ForceSingleBullet && CWC.Gun!.TryCast<ShotgunSynced>() != null;
            int shotgunBullets = 1;
            int coneSize = 0;
            float segmentSize = 0;
            if (isShotgun)
            {
                shotgunBullets = CWC.Weapon.ArchetypeData.ShotgunBulletCount;
                coneSize = CWC.Weapon.ArchetypeData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float spread = ApplySpreadPerShot ? Weapon.s_weaponRayData.randomSpread : 0f;
            for (uint mod = 1; mod <= Repeat + 1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod;
                    float y = Offsets[i + 1] * mod;
                    FireShotVisual(x, y, spread);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
                        x += coneSize * Mathf.Cos(angle);
                        y += coneSize * Mathf.Sin(angle);
                        FireShotVisual(x, y, spread);
                    }
                }
            }
        }

        public void Invoke(WeaponPostFireContext context)
        {
            s_wallPierce = CWC.HasTrait(typeof(WallPierce));
            s_projectile = CWC.HasTrait(typeof(Projectile));

            s_ray.origin = Weapon.s_ray.origin;
            s_baseDir = IgnoreSpread ? CWC.Weapon.Owner.FPSCamera.CameraRayDir : Weapon.s_weaponRayData.fireDir;
            bool isShotgun = !ForceSingleBullet && CWC.Gun!.TryCast<Shotgun>() != null;
            int shotgunBullets = 1;
            int coneSize = 0;
            float segmentSize = 0;
            if (isShotgun)
            {
                shotgunBullets = CWC.Weapon.ArchetypeData.ShotgunBulletCount;
                coneSize = CWC.Weapon.ArchetypeData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float aimMod = CWC.Weapon.FPItemHolder.ItemAimTrigger ? AimOffsetMod : 1f;
            float spread = ApplySpreadPerShot ? Weapon.s_weaponRayData.randomSpread : 0f;
            for (uint mod = 1; mod <= Repeat+1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod * aimMod;
                    float y = Offsets[i+1] * mod * aimMod;
                    FireShot(x, y, spread);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
                        x += coneSize * Mathf.Cos(angle);
                        y += coneSize * Mathf.Sin(angle);
                        FireShot(x, y, spread);
                    }
                }
            }
        }

        private void FireShot(float x, float y, float spread)
        {
            ArchetypeDataBlock archData = CWC.Weapon.ArchetypeData;
            s_hitData.owner = CWC.Weapon.Owner;
            s_hitData.damage = archData.Damage;
            s_hitData.damageFalloff = archData.DamageFalloff;
            s_hitData.staggerMulti = archData.StaggerDamageMulti;
            s_hitData.precisionMulti = archData.PrecisionDamageMulti;
            s_hitData.maxRayDist = CWC.Gun!.MaxRayDist;
            s_hitData.RayHit = default;

            s_hitEnts.Clear();
            CalcRayDir(x, y, spread);

            // Stops at padlocks but that's the same behavior as vanilla so idc
            Vector3 wallPos;
            bool hitWall;
            if ((hitWall = Physics.Raycast(s_ray, out RaycastHit wallRayHit, s_hitData.maxRayDist, LayerUtil.MaskWorld)) && !s_wallPierce)
                wallPos = wallRayHit.point;
            else
                wallPos = s_ray.origin + s_ray.direction * s_hitData.maxRayDist;

            WeaponPostRayContext context = new(s_hitData, s_ray.origin, hitWall);
            CWC.Invoke(context);
            if (!context.Result)
            {
                if (s_projectile) return;

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
                if (s_wallPierce)
                    CheckForHits(pierceCount, maxDist, LayerUtil.MaskDynamic);
                
                if (pierceCount > 0 && hitWall && !AlreadyHit(DamageableUtil.GetDamageableFromRayHit(wallRayHit)))
                {
                    s_hitData.RayHit = wallRayHit;
                    FX_Manager.EffectTargetPosition = wallPos;
                    BulletHit(s_hitData);
                }
            }

            FX_Manager.PlayLocalVersion = false;
            BulletWeapon.s_tracerPool.AquireEffect().Play(null, CWC.Weapon.MuzzleAlign.position, Quaternion.LookRotation(s_ray.direction));
        }

        private void FireShotVisual(float x, float y, float spread)
        {
            if (s_projectile) return;

            CalcRayDir(x, y, spread, local: false);

            if (Physics.Raycast(s_ray, out s_rayHit, 20f, LayerUtil.MaskEntityAndWorld))
            {
                FX_Manager.EffectTargetPosition = s_rayHit.point;
                s_hitData.RayHit = s_rayHit;
                BulletWeapon.BulletHit(s_hitData.ToWeaponHitData(), false);
            }
            else
                FX_Manager.EffectTargetPosition = s_ray.origin + s_ray.direction * 20f;

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

            Array.Sort(hits, SortUtil.RaycastDistance);
            foreach (var hit in hits)
            {
                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(hit);
                if (damageable == null) continue;
                if (AlreadyHit(damageable)) continue;
                if (s_wallPierce && !WallPierce.IsTargetReachable(s_hitData.owner.CourseNode, damageable.GetBaseAgent()?.CourseNode)) continue;

                s_hitData.RayHit = hit;
                FX_Manager.EffectTargetPosition = hit.point;
                if (BulletHit(s_hitData))
                    pierceCount--;

                if (pierceCount == 0) return;
            }
        }

        private bool BulletHit(HitData data) => BulletWeapon.BulletHit(data.Apply(Weapon.s_weaponRayData), true, 0, 0, true);

        public override IWeaponProperty Clone()
        {
            MultiShot copy = new()
            {
                AimOffsetMod = AimOffsetMod,
                Repeat = Repeat,
                IgnoreSpread = IgnoreSpread,
                ApplySpreadPerShot = ApplySpreadPerShot
            };
            copy.Offsets.AddRange(Offsets);

            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            EWCJson.Serialize(writer, Offsets);
            writer.WriteNumber(nameof(AimOffsetMod), AimOffsetMod);
            writer.WriteNumber(nameof(Repeat), Repeat);
            writer.WriteBoolean(nameof(IgnoreSpread), IgnoreSpread);
            writer.WriteBoolean(nameof(ApplySpreadPerShot), ApplySpreadPerShot);
            writer.WriteBoolean(nameof(ForceSingleBullet), ForceSingleBullet);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
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
                case "ignorespread":
                    IgnoreSpread = reader.GetBoolean();
                    break;
                case "applyspreadpershot":
                case "applyspread":
                    ApplySpreadPerShot = reader.GetBoolean();
                    break;
                case "forcesinglebullet":
                case "singlebullet":
                    ForceSingleBullet = reader.GetBoolean();
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

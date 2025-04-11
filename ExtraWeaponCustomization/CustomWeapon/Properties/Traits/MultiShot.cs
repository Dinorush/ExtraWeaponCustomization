using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using GameData;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class MultiShot :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
    {
        public readonly List<float> Offsets = new(2);
        public float AimOffsetMod { get; private set; } = 1f;
        public uint Repeat { get; private set; } = 0;
        public bool UseAimDir { get; private set; } = false;
        public float Spread { get; private set; } = 0f;
        public bool IgnoreSpreadMod { get; private set; } = false;
        public bool CancelShot { get; private set; } = false;
        public bool ForceSingleBullet { get; private set; } = false;
        public bool RunHitTriggers { get; private set; } = true;

        private static Ray s_ray;
        private readonly static HitData s_hitData = new(Enums.DamageType.Bullet);
        private static float s_initialShotMod = 1f;

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponSetupContext) || contextType == typeof(WeaponClearContext)) return CancelShot;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponPreFireContext context)
        {
            if (Clock.Time - CWC.Gun!.m_lastFireTime > CWC.Gun!.m_fireRecoilCooldown)
                s_initialShotMod = 0.2f;
            else
                s_initialShotMod = 1f;
        }

        public void Invoke(WeaponSetupContext context)
        {
            CWC.ShotComponent!.CancelNormalShot = true;
        }

        public void Invoke(WeaponClearContext context)
        {
            CWC.ShotComponent!.CancelNormalShot = false;
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            if (CWC.HasTrait<Projectile>()) return;

            s_ray.origin = CWC.Weapon.MuzzleAlign.position;
            s_ray.direction = UseAimDir || CWC.IsShotgun ? CWC.Weapon.MuzzleAlign.forward : ShotManager.VanillaFireDir;

            int shotgunBullets = 1;
            int coneSize = 0;
            float segmentSize = 0;
            if (CWC.IsShotgun && !ForceSingleBullet)
            {
                shotgunBullets = CWC.ArchetypeData.ShotgunBulletCount;
                coneSize = CWC.ArchetypeData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float spread = Spread;
            if (spread < 0f)
                spread = CWC.IsShotgun ? CWC.ArchetypeData.ShotgunBulletSpread : 0f;

            for (uint mod = 1; mod <= Repeat + 1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod;
                    float y = -Offsets[i + 1] * mod;
                    FireVisual(x, y, spread);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
                        FireVisual(x + coneSize * (float)Math.Cos(angle), y + coneSize * (float)Math.Sin(angle), spread);
                    }
                }
            }
        }

        public void Invoke(WeaponPostFireContext context)
        {
            s_ray.origin = CWC.Weapon.Owner.FPSCamera.Position;
            s_ray.direction = UseAimDir || CWC.IsShotgun ? CWC.Weapon.Owner.FPSCamera.CameraRayDir : ShotManager.VanillaFireDir;

            int shotgunBullets = 1;
            float coneSize = 0;
            float segmentSize = 0;
            var archData = CWC.Weapon.ArchetypeData;
            if (CWC.IsShotgun && !ForceSingleBullet)
            {
                shotgunBullets = archData.ShotgunBulletCount;
                coneSize = archData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            bool isADS = CWC.Weapon.FPItemHolder.ItemAimTrigger;
            float spread = Spread;
            if (spread < 0f)
            {
                if (CWC.IsShotgun)
                    spread = archData.ShotgunBulletSpread;
                else
                    spread = (isADS ? archData.AimSpread : archData.HipFireSpread) * s_initialShotMod;
            }

            float aimMod = isADS ? AimOffsetMod : 1f;
            if (!IgnoreSpreadMod)
            {
                float spreadMod = CWC.SpreadController!.Value;
                spread *= spreadMod;
                coneSize *= spreadMod;
                aimMod *= spreadMod;
            }

            for (uint mod = 1; mod <= Repeat+1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod * aimMod;
                    float y = -Offsets[i+1] * mod * aimMod;
                    Fire(x, y, spread);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
                        Fire(x + coneSize * (float)Math.Cos(angle), y + coneSize * (float)Math.Sin(angle), spread);
                    }
                }
            }
        }

        private void Fire(float x, float y, float spread)
        {
            ArchetypeDataBlock archData = CWC.ArchetypeData;
            s_hitData.owner = CWC.Weapon.Owner;
            s_hitData.damage = archData.GetDamageWithBoosterEffect(s_hitData.owner, CWC.Weapon.ItemDataBlock.inventorySlot);
            s_hitData.damageFalloff = archData.DamageFalloff;
            s_hitData.staggerMulti = archData.StaggerDamageMulti;
            s_hitData.precisionMulti = archData.PrecisionDamageMulti;
            s_hitData.maxRayDist = CWC.Gun!.MaxRayDist;
            s_hitData.angOffsetX = x;
            s_hitData.angOffsetY = y;
            s_hitData.randomSpread = spread;
            s_hitData.shotInfo.Reset(s_hitData.damage, s_hitData.precisionMulti, s_hitData.staggerMulti);
            s_hitData.RayHit = default;
            s_hitData.shotInfo.NewShot(CWC);

            if (!RunHitTriggers)
                CWC.RunHitTriggers = false;
            CWC.ShotComponent!.FireSpread(s_ray, s_hitData);
            if (!RunHitTriggers)
                CWC.RunHitTriggers = true;
        }

        private void FireVisual(float x, float y, float spread)
        {
            s_hitData.owner = CWC.Weapon.Owner;
            s_hitData.damage = CWC.ArchetypeData.Damage;
            s_hitData.angOffsetX = x;
            s_hitData.angOffsetY = y;
            s_hitData.randomSpread = spread;
            CWC.ShotComponent!.FireSpread(s_ray, s_hitData);
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
            EWCJson.Serialize(writer, nameof(Offsets), Offsets);
            writer.WriteNumber(nameof(AimOffsetMod), AimOffsetMod);
            writer.WriteNumber(nameof(Repeat), Repeat);
            writer.WriteBoolean(nameof(UseAimDir), UseAimDir);
            writer.WriteNumber(nameof(Spread), Spread);
            writer.WriteBoolean(nameof(IgnoreSpreadMod), IgnoreSpreadMod);
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
                case "ignorespreadmod":
                    IgnoreSpreadMod = reader.GetBoolean();
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

using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
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
        private readonly static HitData s_hitData = new(DamageType.Bullet);
        private static float s_initialShotMod = 1f;

        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponSetupContext) || contextType == typeof(WeaponClearContext)) return CancelShot;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponPreFireContext context)
        {
            s_initialShotMod = 1f;
            if (CWC.Owner.IsType(OwnerType.Local))
            {
                var gun = ((LocalGunComp)CWC.Weapon).Value;
                if (Clock.Time - gun.m_lastFireTime > gun.m_fireRecoilCooldown)
                    s_initialShotMod = 0.2f;
            }
        }

        public void Invoke(WeaponSetupContext context)
        {
            CGC.ShotComponent.CancelNormalShot = true;
        }

        public void Invoke(WeaponClearContext context)
        {
            CGC.ShotComponent.CancelNormalShot = false;
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            if (CWC.HasTrait<Projectile>()) return;

            bool isShotgun = CGC.Gun.IsShotgun;
            s_ray.origin = CWC.Owner.FirePos;
            s_ray.direction = UseAimDir || isShotgun ? CWC.Owner.FireDir : ShotManager.VanillaFireDir;

            int shotgunBullets = 1;
            int coneSize = 0;
            float segmentSize = 0;
            if (isShotgun && !ForceSingleBullet)
            {
                shotgunBullets = CGC.Gun.ArchetypeData.ShotgunBulletCount;
                coneSize = CGC.Gun.ArchetypeData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float spread = Spread;
            if (spread < 0f)
                spread = isShotgun ? CGC.Gun.ArchetypeData.ShotgunBulletSpread : 0f;

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
            bool isShotgun = CGC.Gun.IsShotgun;
            s_ray.origin = CWC.Owner.FirePos;
            s_ray.direction = UseAimDir || isShotgun ? CWC.Owner.FireDir : ShotManager.VanillaFireDir;

            int shotgunBullets = 1;
            float coneSize = 0;
            float segmentSize = 0;
            var archData = CGC.Gun.ArchetypeData;
            if (isShotgun && !ForceSingleBullet)
            {
                shotgunBullets = archData.ShotgunBulletCount;
                coneSize = archData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            bool isADS = CGC.Gun.IsAiming;
            float spread = Spread;
            if (spread < 0f)
            {
                if (isShotgun)
                    spread = archData.ShotgunBulletSpread;
                else
                    spread = (isADS ? archData.AimSpread : archData.HipFireSpread) * s_initialShotMod;
            }

            float aimMod = isADS ? AimOffsetMod : 1f;
            if (!IgnoreSpreadMod)
            {
                float spreadMod = CGC.SpreadController.Value;
                spread *= spreadMod;
                coneSize *= spreadMod;
                aimMod *= spreadMod;
            }

            for (uint mod = 1; mod <= Repeat+1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod * aimMod;
                    float y = Offsets[i+1] * mod * aimMod;
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
            ArchetypeDataBlock archData = CGC.Gun.ArchetypeData;
            s_hitData.owner = CWC.Owner.Player;
            s_hitData.shotInfo.Reset(archData.Damage, archData.PrecisionDamageMulti, archData.StaggerDamageMulti, CWC);
            s_hitData.damage = s_hitData.shotInfo.OrigDamage;
            s_hitData.damageFalloff = archData.DamageFalloff;
            s_hitData.staggerMulti = s_hitData.shotInfo.OrigStagger;
            s_hitData.precisionMulti = archData.PrecisionDamageMulti;
            s_hitData.maxRayDist = CGC.Gun.MaxRayDist;
            s_hitData.angOffsetX = x;
            s_hitData.angOffsetY = y;
            s_hitData.randomSpread = spread;
            s_hitData.RayHit = default;

            ToggleRunTriggers(false);
            CGC.ShotComponent.FireSpread(s_ray, CWC.Owner.MuzzleAlign.position, s_hitData);
            ToggleRunTriggers(true);
        }

        private void ToggleRunTriggers(bool enable)
        {
            if (!RunHitTriggers)
                CWC.RunHitTriggers = enable;
        }

        private void FireVisual(float x, float y, float spread)
        {
            s_hitData.owner = CWC.Owner.Player;
            s_hitData.damage = CGC.Gun.ArchetypeData.Damage;
            s_hitData.angOffsetX = x;
            s_hitData.angOffsetY = y;
            s_hitData.randomSpread = spread;
            CGC.ShotComponent.FireSpread(s_ray, CWC.Owner.MuzzleAlign.position, s_hitData);
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

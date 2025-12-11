using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using GameData;
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
        ITriggerCallbackDirSync
    {
        public ushort SyncID { get; set; }

        public readonly List<float> Offsets = new(2);
        public uint ArchetypeID { get; set; } = 0;
        public uint Repeat { get; private set; } = 0;
        public float Spread { get; private set; } = 0;
        public bool IgnoreSpreadMod { get; private set; } = false;
        public bool UseParentShotMod { get; private set; } = true;
        public bool ForceSingleBullet { get; private set; } = false;
        public FireSetting FireFrom { get; private set; } = FireSetting.User;
        public bool UserUseAimDir { get; private set; } = false;
        public bool DamageFriendly { get; private set; } = true;
        public bool DamageOwner { get; private set; } = false;
        public bool HitTriggerTarget { get; private set; } = false;
        public bool RunHitTriggers { get; private set; } = true;

        private const float WallHitBuffer = 0.03f;


        private ArchetypeDataBlock? _cachedArchetype;

        public override bool ValidProperty()
        {
            // Caching on ValidProperty since we don't want to add the property if the DB doesn't exist
            if (ArchetypeID != 0)
            {
                var archBlock = ArchetypeDataBlock.GetBlock(ArchetypeID);
                if (archBlock == null)
                {
                    EWCLogger.Error($"FireShot: Unable to find Archetype block with ID {ArchetypeID}!");
                    return false;
                }
                _cachedArchetype = archBlock;
            }
            else if (!CWC.Weapon.IsType(WeaponType.Gun))
                return false;
            else
                _cachedArchetype = null;

            return base.ValidProperty();
        }

        public override void TriggerReset() {}
        public void TriggerResetSync() {}

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            int iterations = 0;
            List<(Vector3 pos, Vector3 dir, float amount, ShotInfo shotInfo, IDamageable? damBase)>? hitContexts = null;
            if (FireFrom != FireSetting.User)
            {
                hitContexts = new(5);
                foreach (var context in contexts)
                {
                    if (context.triggerAmt < 1f || context.context is not IPositionContext hitContext) continue;

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
                        position += hitContext.Normal * WallHitBuffer;
                    Vector3 direction = FireFrom switch
                    {
                        FireSetting.HitNormal => hitContext.Normal,
                        FireSetting.HitReflect => Vector3.Reflect(hitContext.Direction, hitContext.Normal),
                        _ => hitContext.Direction
                    };
                    hitContexts.Add((position, direction, context.triggerAmt, hitContext.ShotInfo.Orig, damBase));
                }

                if (hitContexts.Count == 0) return;
            }
            else
            {
                iterations = (int) contexts.Sum(context => context.triggerAmt);
                if (iterations == 0) return;
            }

            var weapon = (IGunComp)CWC.Weapon;
            int shotgunBullets = 1;
            float coneSize = 0;
            float segmentSize = 0;
            var archData = weapon.ArchetypeData;
            if (weapon.IsShotgun && !ForceSingleBullet)
            {
                shotgunBullets = archData.ShotgunBulletCount;
                coneSize = archData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float spread = Spread;
            if (spread < 0f)
            {
                if (weapon.IsShotgun)
                    spread = archData.ShotgunBulletSpread;
                else
                    spread = weapon.IsAiming ? archData.AimSpread : archData.HipFireSpread;
            }

            Ray ray = new(CWC.Owner.FirePos, UserUseAimDir ? CWC.Owner.FireDir : ShotManager.VanillaFireDir);
            if (FireFrom != FireSetting.User)
            {
                foreach (var (pos, dir, amount, shotInfo, baseDam) in hitContexts!)
                {
                    ray.origin = pos;
                    ray.direction = dir;
                    IntPtr ignoreEnt = IntPtr.Zero;
                    if (!HitTriggerTarget && baseDam != null)
                        ignoreEnt = baseDam.Pointer;
                    FirePerTrigger(ray, spread, shotgunBullets, segmentSize, coneSize, false, shotInfo, ignoreEnt);

                    if (!CWC.HasTrait<Traits.Projectile>())
                        TriggerManager.SendInstance(this, pos, dir, amount);
                }
            }
            else
            {
                for (int iter = 0; iter < iterations; iter++)
                    FirePerTrigger(ray, spread, shotgunBullets, segmentSize, coneSize, false);

                if (!CWC.HasTrait<Traits.Projectile>())
                    TriggerManager.SendInstance(this, iterations);
            }
        }

        public void TriggerApplySync(Vector3 position, Vector3 direction, float triggerSum)
        {
            // Should always be an int, so round JFS in case of network compression errors.
            int iterations = (int)Math.Round(triggerSum);
            Ray ray = new(position, direction);

            var weapon = CWC.Weapon;
            int shotgunBullets = 1;
            int coneSize = 0;
            float segmentSize = 0;
            if (weapon.IsShotgun && !ForceSingleBullet)
            {
                shotgunBullets = weapon.ArchetypeData.ShotgunBulletCount;
                coneSize = weapon.ArchetypeData.ShotgunConeSize;
                segmentSize = Mathf.Deg2Rad * (360f / (shotgunBullets - 1));
            }

            float spread = Spread;
            if (spread < 0f)
                spread = weapon.IsShotgun ? weapon.ArchetypeData.ShotgunBulletSpread : 0f;

            for (int iter = 0; iter < iterations; iter++)
                FirePerTrigger(ray, spread, shotgunBullets, segmentSize, coneSize, true);
        }

        public void TriggerApplySync(float iterations)
        {
            TriggerApplySync(CWC.Owner.FirePos, CWC.Owner.FireDir, iterations);
        }

        private void FirePerTrigger(Ray ray, float spread, int shotgunBullets, float segmentSize, float coneSize, bool visual, ShotInfo? shotInfo = null, IntPtr ignoreEnt = default)
        {
            float spreadMod = 1f;
            if (!IgnoreSpreadMod)
            {
                spreadMod = CWC.SpreadController.Value;
                spread *= spreadMod;
                coneSize *= spreadMod;
            }

            for (uint mod = 1; mod <= Repeat + 1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod * spreadMod;
                    float y = Offsets[i + 1] * mod * spreadMod;
                    if (visual)
                        FireVisual(ray, x, y, spread);
                    else
                        Fire(ray, x, y, spread, shotInfo, ignoreEnt);

                    for (int j = 1; j < shotgunBullets; j++)
                    {
                        float angle = segmentSize * j;
                        if (visual)
                            FireVisual(ray, x + coneSize * (float)Math.Cos(angle), y + coneSize * (float)Math.Sin(angle), spread);
                        else
                            Fire(ray, x + coneSize * (float)Math.Cos(angle), y + coneSize * (float)Math.Sin(angle), spread, shotInfo, ignoreEnt);
                    }
                }
            }
        }

        private void Fire(Ray ray, float x, float y, float spread, ShotInfo? orig, IntPtr ignoreEnt)
        {
            ArchetypeDataBlock archData = ArchetypeID != 0 ? _cachedArchetype! : CWC.Weapon.ArchetypeData;
            HitData hitData = new(DamageType.Bullet);
            hitData.owner = CWC.Owner.Player;
            hitData.shotInfo.Reset(archData.Damage, archData.PrecisionDamageMulti, archData.StaggerDamageMulti, CWC, orig, UseParentShotMod);
            hitData.ResetDamage();
            hitData.damageFalloff = archData.DamageFalloff;
            hitData.pierceLimit = archData.PierceLimit();
            hitData.maxRayDist = CWC.Weapon.MaxRayDist;
            hitData.angOffsetX = x;
            hitData.angOffsetY = y;
            hitData.randomSpread = spread;
            hitData.RayHit = default;

            int friendlyMask = 0;
            if (DamageOwner)
                friendlyMask |= LayerUtil.MaskOwner;
            if (DamageFriendly)
                friendlyMask |= LayerUtil.MaskFriendly;

            ToggleRunTriggers(false);
            CWC.ShotComponent.FireSpread(ray, FireFrom != FireSetting.User ? ray.origin : CWC.Owner.MuzzleAlign.position, hitData, friendlyMask, ignoreEnt);
            ToggleRunTriggers(true);
        }

        private void ToggleRunTriggers(bool enable)
        {
            if (!RunHitTriggers)
                CWC.RunHitTriggers = enable;
        }

        private void FireVisual(Ray ray, float x, float y, float spread)
        {
            ArchetypeDataBlock archData = ArchetypeID != 0 ? _cachedArchetype! : CWC.Weapon.ArchetypeData;
            HitData hitData = new(DamageType.Bullet)
            {
                owner = CWC.Owner.Player,
                damage = archData.Damage,
                pierceLimit = 1,
                fireDir = ray.direction,
                angOffsetX = x,
                angOffsetY = y,
                randomSpread = spread
            };
            CWC.ShotComponent.FireSpread(ray, FireFrom != FireSetting.User ? ray.origin : CWC.Owner.MuzzleAlign.position, hitData);
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
            EWCJson.Serialize(writer, nameof(Offsets),Offsets);
            writer.WriteNumber(nameof(ArchetypeID), ArchetypeID);
            writer.WriteNumber(nameof(Repeat), Repeat);
            writer.WriteNumber(nameof(Spread), Spread);
            writer.WriteBoolean(nameof(IgnoreSpreadMod), IgnoreSpreadMod);
            writer.WriteBoolean(nameof(UseParentShotMod), UseParentShotMod);
            writer.WriteBoolean(nameof(ForceSingleBullet), ForceSingleBullet);
            writer.WriteString(nameof(FireFrom), FireFrom.ToString());
            writer.WriteBoolean(nameof(UserUseAimDir), UserUseAimDir);
            writer.WriteBoolean(nameof(DamageFriendly), DamageFriendly);
            writer.WriteBoolean(nameof(DamageOwner), DamageOwner);
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
                case "offset":
                    List<float>? offsets = ReadOffsets(ref reader);
                    if (offsets == null) return;
                    if (offsets.Count % 2 != 0)
                        offsets.RemoveAt(offsets.Count - 1);
                    Offsets.AddRange(offsets);
                    break;
                case "archetypeid":
                case "archetype":
                case "archid":
                case "arch":
                    ArchetypeID = reader.GetUInt32();
                    break;
                case "repeats":
                case "repeat":
                    Repeat = reader.GetUInt32();
                    break;
                case "spread":
                    Spread = reader.GetSingle();
                    break;
                case "ignorespreadmod":
                    IgnoreSpreadMod = reader.GetBoolean();
                    break;
                case "useparentshotmod":
                case "parentshotmod":
                    UseParentShotMod = reader.GetBoolean();
                    break;
                case "forcesinglebullet":
                case "singlebullet":
                    ForceSingleBullet = reader.GetBoolean();
                    break;
                case "firefrom":
                    FireFrom = reader.GetString().ToEnum(FireSetting.User);
                    break;
                case "useruseaimdir":
                case "useaimdir":
                    UserUseAimDir = reader.GetBoolean();
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

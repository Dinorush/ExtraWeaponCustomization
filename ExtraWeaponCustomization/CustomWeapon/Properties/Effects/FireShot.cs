using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
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
        public bool IgnoreSpreadMod { get; private set; } = false;
        public bool ForceSingleBullet { get; private set; } = false;
        public FireSetting FireFrom { get; private set; } = FireSetting.User;
        public bool DamageFriendly { get; private set; } = true;
        public bool DamageOwner { get; private set; } = false;
        public bool HitTriggerTarget { get; private set; } = false;
        public bool RunHitTriggers { get; private set; } = true;

        private const float WallHitBuffer = -0.03f;

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
                    if (context.triggerAmt < 1f || context.context is not WeaponHitContextBase hitContext) continue;

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

            Ray lastRay = Weapon.s_ray;
            bool isShotgun = CWC.Gun!.TryCast<Shotgun>() != null;

            int shotgunBullets = 1;
            float coneSize = 0;
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

            Ray ray = new(CWC.Weapon.Owner.FPSCamera.Position, CWC.Gun!.Owner.FPSCamera.CameraRayDir);
            
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
                    TriggerManager.SendInstance(this, pos, dir, amount);
                }
            }
            else
            {
                for (int iter = 0; iter < iterations; iter++)
                    FirePerTrigger(ray, spread, shotgunBullets, segmentSize, coneSize, false);
            }

            Weapon.s_ray = lastRay;

            TriggerManager.SendInstance(this, iterations);
        }

        public void TriggerApplySync(Vector3 position, Vector3 direction, float triggerSum)
        {
            if (CWC.HasTrait<Traits.Projectile>()) return;

            // Should always be an int, so round JFS in case of network compression errors.
            int iterations = (int)Math.Round(triggerSum);
            Ray ray = new(position, direction);
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

            for (int iter = 0; iter < iterations; iter++)
                FirePerTrigger(ray, spread, shotgunBullets, segmentSize, coneSize, true);
        }

        public void TriggerApplySync(float iterations)
        {
            TriggerApplySync(CWC.Weapon.MuzzleAlign.position, CWC.Weapon.MuzzleAlign.forward, iterations);
        }

        private void FirePerTrigger(Ray ray, float spread, int shotgunBullets, float segmentSize, float coneSize, bool visual, ShotInfo? shotInfo = null, IntPtr ignoreEnt = default)
        {
            float spreadMod = 1f;
            if (!IgnoreSpreadMod && CWC.IsLocal)
            {
                spreadMod = CWC.SpreadController!.Value;
                spread *= spreadMod;
                coneSize *= spreadMod;
            }

            for (uint mod = 1; mod <= Repeat + 1; mod++)
            {
                for (int i = 0; i < Offsets.Count; i += 2)
                {
                    float x = Offsets[i] * mod * spreadMod;
                    float y = -Offsets[i + 1] * mod * spreadMod;
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
            ArchetypeDataBlock archData = CWC.Weapon.ArchetypeData;
            HitData hitData = new(Enums.DamageType.Bullet);
            hitData.owner = CWC.Weapon.Owner;
            hitData.damage = archData.GetDamageWithBoosterEffect(hitData.owner, CWC.Weapon.ItemDataBlock.inventorySlot);
            hitData.damageFalloff = archData.DamageFalloff;
            hitData.staggerMulti = archData.StaggerDamageMulti;
            hitData.precisionMulti = archData.PrecisionDamageMulti;
            hitData.maxRayDist = CWC.Gun!.MaxRayDist;
            hitData.angOffsetX = x;
            hitData.angOffsetY = y;
            hitData.randomSpread = spread;
            hitData.shotInfo.Reset(hitData.damage, hitData.precisionMulti, hitData.staggerMulti);
            hitData.RayHit = default;
            if (orig != null)
                hitData.shotInfo.PullMods(orig);
            else
                hitData.shotInfo.NewShot(CWC);

            int friendlyMask = 0;
            if (DamageOwner)
                friendlyMask |= LayerUtil.MaskOwner;
            if (DamageFriendly)
                friendlyMask |= LayerUtil.MaskFriendly;

            if (!RunHitTriggers)
                CWC.RunHitTriggers = false;
            CWC.ShotComponent!.FireSpread(ray, hitData, friendlyMask, ignoreEnt);
            if (!RunHitTriggers)
                CWC.RunHitTriggers = true;
        }

        private void FireVisual(Ray ray, float x, float y, float spread)
        {
            HitData hitData = new(Enums.DamageType.Bullet);
            hitData.owner = CWC.Weapon.Owner;
            hitData.damage = CWC.Weapon.ArchetypeData.Damage;
            hitData.angOffsetX = x;
            hitData.angOffsetY = y;
            hitData.randomSpread = spread;
            CWC.ShotComponent!.FireSpread(ray, hitData);
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
            writer.WriteBoolean(nameof(IgnoreSpreadMod), IgnoreSpreadMod);
            writer.WriteBoolean(nameof(ForceSingleBullet), ForceSingleBullet);
            writer.WriteString(nameof(FireFrom), FireFrom.ToString());
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
                case "ignorespreadmod":
                    IgnoreSpreadMod = reader.GetBoolean();
                    break;
                case "forcesinglebullet":
                case "singlebullet":
                    ForceSingleBullet = reader.GetBoolean();
                    break;
                case "firefrom":
                    FireFrom = reader.GetString().ToEnum(FireSetting.User);
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

using Agents;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.ShrapnelHit;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
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
        IWeaponProperty<WeaponOwnerSetContext>,
        ITriggerCallbackBasicSync,
        ITriggerCallbackDirSync,
        IGunProperty,
        IReferenceHolder
    {
        public ushort SyncID { get; set; }

        public uint Count { get; private set; } = 0;
        public uint ArchetypeID { get; private set; } = 0;
        public float Damage { get; private set; } = 0;
        public Vector2 DamageFalloff { get; private set; } = new Vector2(100, 100);
        public float PrecisionDamageMulti { get; private set; } = 1f;
        public float StaggerDamageMulti { get; private set; } = 1f;
        public float FriendlyDamageMulti { get; private set; } = 1f;
        public int PierceLimit { get; private set; } = 1;
        public float MaxRange { get; private set; } = 0;
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

        private const float WallHitBuffer = 0.03f;

        private CustomShotSettings _shotSettings;
        private int _friendlyMask = 0;

        public override void TriggerReset() { }
        public void TriggerResetSync() { }

        public Shrapnel()
        {
            _shotSettings = new(hitFunc: DoHit);
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.BulletLanded));
            SetValidTriggers(DamageType.Shrapnel);
        }

        public void Invoke(WeaponOwnerSetContext context)
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

            _shotSettings.pierceLimit = PierceLimit;
            _shotSettings.projectile?.SetOverrides(DoProjectileHit, _shotSettings.wallPierce, PierceLimit);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            foreach (var tContext in contexts)
            {
                var amount = (int)(tContext.triggerAmt * Count);
                if (amount < 1) continue;

                ShotInfo? info = null;
                IntPtr ignoreBase = IntPtr.Zero;
                float falloff = 1f;
                Vector3 position;
                Vector3 normal = Vector3.zero;
                if (tContext.context is WeaponHitContextBase hitContext)
                {
                    info = hitContext.ShotInfo.Orig;
                    position = hitContext.Position;
                    falloff = hitContext.Falloff;
                    if (hitContext is WeaponHitDamageableContextBase damContext)
                    {
                        if (!HitTriggerTarget)
                            ignoreBase = damContext.Damageable.GetBaseDamagable().Pointer;

                        Agent? agent = damContext.Damageable.GetBaseAgent();
                        if (agent != null)
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
                    var owner = CWC.Weapon.Owner;
                    position = owner.FPSCamera.Position;
                }

                if (!CWC.HasTrait<Traits.Projectile>())
                    TriggerManager.SendInstance(this, position, normal, tContext.triggerAmt);
                DoShrapnel(position, normal, amount, falloff, info, ignoreBase);
            }
        }

        public void TriggerApplySync(Vector3 position, Vector3 direction, float triggerAmt)
        {
            // Should always be an int, so round JFS in case of network compression errors.
            var amount = (int)Math.Round(triggerAmt * Count);
            DoShrapnelVisual(position, direction, amount);
        }

        public void TriggerApplySync(float iterations)
        {
            TriggerApplySync(CWC.Weapon.Owner.EyePosition, CWC.Weapon.Owner.EyePosition, iterations);
        }

        private void DoShrapnelVisual(Vector3 position, Vector3 normal, int amount)
        {
            HitData hitData = new(DamageType.Shrapnel)
            {
                owner = CWC.Weapon.Owner,
                damage = CWC.ArchetypeData.Damage
            };
            var ray = new Ray(position, Vector3.zero);

            for (int i = 0; i < amount; i++)
            {
                ray.direction = UnityEngine.Random.onUnitSphere;
                if (normal != Vector3.zero && Vector3.Dot(ray.direction, normal) < 0)
                    ray.direction = -ray.direction;
                hitData.fireDir = ray.direction;
                CWC.ShotComponent!.FireCustom(ray, ray.origin, hitData, shotSettings: _shotSettings);
            }
        }

        private void DoShrapnel(Vector3 position, Vector3 normal, int amount, float falloff, ShotInfo? shotInfo = null, IntPtr ignoreEnt = default)
        {
            var ray = new Ray(position, Vector3.zero);

            var inventorySlot = CWC.Weapon.AmmoType.ToInventorySlot();
            for (int i = 0; i < amount; i++)
            {
                HitData hitData = new(DamageType.Shrapnel);
                hitData.owner = CWC.Weapon.Owner;
                hitData.falloff = IgnoreFalloff ? 1f : falloff;
                hitData.damage = AgentModifierManager.ApplyModifier(CWC.Weapon.Owner, inventorySlot == Player.InventorySlot.GearStandard ? AgentModifier.StandardWeaponDamage : AgentModifier.SpecialWeaponDamage, Damage) * hitData.shotInfo.XpMod;
                hitData.damageFalloff = DamageFalloff;
                hitData.staggerMulti = StaggerDamageMulti;
                hitData.precisionMulti = PrecisionDamageMulti;
                hitData.maxRayDist = MaxRange;
                hitData.shotInfo.Reset(hitData.damage, hitData.precisionMulti, hitData.staggerMulti, true);
                hitData.RayHit = default;
                if (shotInfo != null)
                    hitData.shotInfo.PullMods(shotInfo, UseParentShotMod);
                else
                    hitData.shotInfo.NewShot(CWC);

                ray.direction = UnityEngine.Random.onUnitSphere;
                if (normal != Vector3.zero && Vector3.Dot(ray.direction, normal) < 0)
                    ray.direction = -ray.direction;

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
                            hitData.fireDir = ray.direction;
                            ToggleRunTriggers(false);
                            CWC.ShotComponent!.FireCustom(ray, ray.origin, hitData, _friendlyMask, ignoreEnt, _shotSettings);
                            ToggleRunTriggers(true);
                            ray.origin = bounceHit.point + bounceHit.normal * WallHitBuffer;
                            ray.direction = Vector3.Reflect(ray.direction, bounceHit.normal);
                            hitData.maxRayDist = MaxRange - bounceHit.distance;
                        }
                        break;
                    default:
                        break;
                }

                hitData.fireDir = ray.direction;
                ToggleRunTriggers(false);
                CWC.ShotComponent!.FireCustom(ray, ray.origin, hitData, _friendlyMask, ignoreEnt, _shotSettings);
                ToggleRunTriggers(true);
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

        private void ToggleRunTriggers(bool enable)
        {
            if (!RunHitTriggers)
                CWC.RunHitTriggers = enable;
        }

        private bool DoHit(Gear.BulletWeapon _, HitData hitData) {
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
                    Count = reader.GetUInt32();
                    break;
                case "maxrange":
                case "range":
                    MaxRange = reader.GetSingle();
                    break;
                case "archetypeid":
                case "archetype":
                case "archid":
                case "arch":
                    ArchetypeID = reader.GetUInt32();
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

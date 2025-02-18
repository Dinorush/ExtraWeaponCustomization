﻿using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public enum TriggerName
    {
        PreFire,
        Fire,
        Aim,
        AimEnd,
        ReloadStart,
        Reload,
        Wield,
        BulletLanded,
        ChargeLanded,
        Hit,
        PreHit,
        Charge,
        Damage,
        Kill,
        HitTaken,
        DamageTaken,
        Crouch,
        CrouchEnd,
        Sprint,
        SprintEnd,
        Jump,
        JumpEnd
    }

    public interface ITrigger
    {
        TriggerName Name { get; }
        bool Invoke(WeaponTriggerContext context, out float amount);
        void Reset();
        ITrigger Clone();
        void DeserializeProperty(string property, ref Utf8JsonReader reader);

        public static ITrigger GetTrigger(TriggerName triggerName) => GetTrigger(triggerName.ToString())!;

        public static ITrigger? GetTrigger(string? name)
        {
            if (name == null) return null;

            name = name.ToLowerInvariant().Replace(" ", null).Replace("on", null);
            return name switch
            {
                "prefire" or "preshot" or "preswing" => new BasicTrigger<WeaponPreFireContext>(TriggerName.PreFire),
                "fire" or "shot" or "swing" => new BasicTrigger<WeaponPostFireContext>(TriggerName.Fire),
                "aim" or "zoomin" => new BasicTrigger<WeaponAimContext>(TriggerName.Aim),
                "aimend" or "zoomout" => new BasicTrigger<WeaponAimEndContext>(TriggerName.AimEnd),
                "reloadstart" or "startreload" => new BasicTrigger<WeaponReloadStartContext>(TriggerName.ReloadStart),
                "reload" => new BasicTrigger<WeaponPostReloadContext>(TriggerName.Reload),
                "wield" => new BasicTrigger<WeaponWieldContext>(TriggerName.Wield),
                "hittaken" => new BasicTrigger<WeaponDamageTakenContext>(TriggerName.HitTaken),
                "damagetaken" => new DamageTakenTrigger(),
                "crouch" => new BasicTrigger<WeaponCrouchContext>(TriggerName.Crouch),
                "crouchend" or "uncrouch" => new BasicTrigger<WeaponCrouchEndContext>(TriggerName.CrouchEnd),
                "sprint" or "run" => new BasicTrigger<WeaponSprintContext>(TriggerName.Sprint),
                "sprintend" or "runend" => new BasicTrigger<WeaponSprintEndContext>(TriggerName.SprintEnd),
                "jump" => new BasicTrigger<WeaponJumpContext>(TriggerName.Jump),
                "jumpend" => new BasicTrigger<WeaponJumpEndContext>(TriggerName.JumpEnd),
                "bulletlanded" or "landedbullet" or "meleelanded" or "landedmelee" => new BulletLandedTrigger(),
                "chargelanded" or "landedcharge" => new ChargeLandedTrigger(),
                string prehit when prehit.Contains("prehit") => new DamageableTrigger<WeaponPreHitDamageableContext>(TriggerName.PreHit, name.ToDamageTypes()),
                string hit when hit.Contains("hit") => new DamageableTrigger<WeaponHitDamageableContext>(TriggerName.Hit, name.ToDamageTypes()),
                string charge when charge.Contains("charge") => new ChargeTrigger(name.ToDamageTypes()),
                string damage when damage.Contains("damage") => new DamageTrigger(name.ToDamageTypes()),
                string kill when kill.Contains("kill") => new DamageTypeTrigger<WeaponPostKillContext>(TriggerName.Kill, name.ToDamageTypes()),
                _ => null
            };
        }
    }

    public interface IDamageTypeTrigger : ITrigger
    {
        DamageType[] DamageTypes { get; }
        DamageType BlacklistType { get; set; }
    }
}

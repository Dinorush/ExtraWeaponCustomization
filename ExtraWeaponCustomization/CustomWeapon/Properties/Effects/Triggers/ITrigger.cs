using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public enum TriggerName
    {
        PreFire,
        Fire,
        StartFiring,
        EndFiring,
        Aim,
        AimEnd,
        ReloadStart,
        Reload,
        Unwield,
        Wield,
        BulletLanded,
        ChargeLanded,
        Hit,
        PreHit,
        Charge,
        Damage,
        Kill,
        Miss,
        HitTaken,
        DamageTaken,
        Health,
        Crouch,
        CrouchEnd,
        Sprint,
        SprintEnd,
        Jump,
        JumpEnd,
        PerTarget,
        Init
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
                "startfiring" => new BasicTrigger<WeaponPostStartFireContext>(TriggerName.StartFiring),
                "endfiring" or "stopfiring" => new BasicTrigger<WeaponPostStopFiringContext>(TriggerName.EndFiring),
                "aim" or "zoomin" => new BasicTrigger<WeaponAimContext>(TriggerName.Aim),
                "aimend" or "zoomout" => new BasicTrigger<WeaponAimEndContext>(TriggerName.AimEnd),
                "reloadstart" or "startreload" => new BasicTrigger<WeaponReloadStartContext>(TriggerName.ReloadStart),
                "reload" => new BasicTrigger<WeaponPostReloadContext>(TriggerName.Reload),
                "unwield" => new BasicTrigger<WeaponUnWieldContext>(TriggerName.Unwield),
                "wield" => new BasicTrigger<WeaponWieldContext>(TriggerName.Wield),
                "hittaken" => new BasicTrigger<WeaponDamageTakenContext>(TriggerName.HitTaken),
                "damagetaken" => new DamageTakenTrigger(),
                "health" => new HealthTrigger(),
                "crouch" => new BasicTrigger<WeaponCrouchContext>(TriggerName.Crouch),
                "crouchend" or "uncrouch" or "stand" => new BasicTrigger<WeaponCrouchEndContext>(TriggerName.CrouchEnd),
                "sprint" or "run" => new BasicTrigger<WeaponSprintContext>(TriggerName.Sprint),
                "sprintend" or "runend" => new BasicTrigger<WeaponSprintEndContext>(TriggerName.SprintEnd),
                "jump" => new BasicTrigger<WeaponJumpContext>(TriggerName.Jump),
                "jumpend" => new BasicTrigger<WeaponJumpEndContext>(TriggerName.JumpEnd),
                "setup" or "init" or "drop" => new BasicTrigger<WeaponInitContext>(TriggerName.Init),
                string perTarget when perTarget.StartsWith("per") => DeterminePerTargetTrigger(perTarget),
                string landed when landed.Contains("landed") => DetermineLandedTrigger(landed),
                string prehit when prehit.Contains("prehit") => new DamageableTrigger<WeaponPreHitDamageableContext>(TriggerName.PreHit, name.ToDamageTypes()),
                string hit when hit.Contains("hit") => new DamageableTrigger<WeaponHitDamageableContext>(TriggerName.Hit, name.ToDamageTypes()),
                string miss when miss.Contains("miss") => new MissTrigger(name.ToDamageTypes()),
                string charge when charge.Contains("charge") => new ChargeTrigger(name.ToDamageTypes()),
                string damage when damage.Contains("damage") => new DamageTrigger(name.ToDamageTypes()),
                string kill when kill.Contains("kill") => new KillTrigger(name.ToDamageTypes()),
                _ => null
            };
        }

        private static ITrigger DetermineLandedTrigger(string name)
        {
            DamageType type = name.Contains("shrapnel") ? DamageType.Shrapnel : DamageType.Bullet;
            if (name.Contains("terrain"))
                type |= DamageType.Terrain;
            return name.Contains("charge") ? new ChargeLandedTrigger(type) : new BulletLandedTrigger(type);
        }

        private static ITrigger? DeterminePerTargetTrigger(string name)
        {
            if (name == "pertarget") return new PerTargetTrigger();

            int sep = name.IndexOf("on",3);
            if (sep == -1) return null;
            ITrigger? activate = GetTrigger(name[3..sep]);
            if (activate == null) return null;
            ITrigger? apply = GetTrigger(name[(sep+2)..]);
            if (apply == null) return null;
            return new PerTargetTrigger(activate, apply);
        }
    }

    public interface IDamageTypeTrigger : ITrigger
    {
        DamageType[] DamageTypes { get; }
        DamageType BlacklistType { get; set; }
    }
}

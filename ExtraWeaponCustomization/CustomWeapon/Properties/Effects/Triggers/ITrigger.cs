using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public enum TriggerName
    {
        Empty,
        PreFire,
        Fire,
        StartFiring,
        EndFiring,
        Aim,
        AimEnd,
        ReloadStart,
        Reload,
        ChargeStart,
        ChargeEnd,
        Unwield,
        Wield,
        PrePush,
        Push,
        PushHit,
        BulletLanded,
        ChargeLanded,
        Hit,
        PreHit,
        Charge,
        Damage,
        Backstab,
        Stagger,
        Kill,
        Miss,
        HitTaken,
        DamageTaken,
        Health,
        Clip,
        Crouch,
        CrouchEnd,
        Sprint,
        SprintEnd,
        Jump,
        JumpEnd,
        PerTarget,
        Key,
        Reference,
        Init
    }

    public interface ITrigger
    {
        public static readonly TriggerName[] HitTriggers = new TriggerName[] { TriggerName.Empty, TriggerName.PushHit, TriggerName.PreHit, TriggerName.Hit, TriggerName.Charge, TriggerName.Damage, TriggerName.Backstab, TriggerName.Stagger, TriggerName.PerTarget };

        TriggerName Name { get; }
        bool StoreZeroAmount => false;
        bool Invoke(WeaponTriggerContext context, out float amount);
        void Reset();
        void OnPropertiesSetup(CustomWeaponComponent cwc) { }
        ITrigger Clone();
        void DeserializeProperty(string property, ref Utf8JsonReader reader);

        public static ITrigger GetTrigger(TriggerName triggerName) => GetTrigger(triggerName.ToString())!;

        public static ITrigger? GetTrigger(string? name)
        {
            if (name == null) return null;

            var origName = name;
            var nameWithOn = name.ToLowerInvariant().Replace(" ", null);
            name = nameWithOn.StartsWith("on") ? nameWithOn[2..] : nameWithOn;
            return name switch
            {
                "invalid" or "null" => null,
                "empty" => EmptyTrigger.Instance,
                "prefire" or "preshot" or "preswing" => new BasicTrigger<WeaponPreFireContext>(TriggerName.PreFire),
                "fire" or "shot" or "swing" => new BasicTrigger<WeaponPostFireContext>(TriggerName.Fire),
                "startfiring" => new BasicTrigger<WeaponPostStartFireContext>(TriggerName.StartFiring),
                "endfiring" or "stopfiring" => new BasicTrigger<WeaponPostStopFiringContext>(TriggerName.EndFiring),
                "pushhit" => new BasicTrigger<WeaponPushHitContext>(TriggerName.PushHit),
                "prepush" => new BasicTrigger<WeaponPrePushContext>(TriggerName.PrePush),
                "push" => new BasicTrigger<WeaponPostPushContext>(TriggerName.Push),
                "aim" or "zoomin" => new BasicTrigger<WeaponAimContext>(TriggerName.Aim),
                "aimend" or "zoomout" => new BasicTrigger<WeaponAimEndContext>(TriggerName.AimEnd),
                "reloadstart" or "startreload" => new BasicTrigger<WeaponReloadStartContext>(TriggerName.ReloadStart),
                "reload" => new BasicTrigger<WeaponPostReloadContext>(TriggerName.Reload),
                "chargestart" => new BasicTrigger<WeaponChargeStartContext>(TriggerName.ChargeStart),
                "chargeend" => new ChargeEndTrigger(),
                "unwield" => new BasicTrigger<WeaponUnWieldContext>(TriggerName.Unwield),
                "wield" => new BasicTrigger<WeaponWieldContext>(TriggerName.Wield),
                "health" => new HealthTrigger(),
                "clip" or "mag" => new ClipTrigger(),
                "crouch" => new BasicTrigger<WeaponCrouchContext>(TriggerName.Crouch),
                "crouchend" or "uncrouch" or "stand" => new BasicTrigger<WeaponCrouchEndContext>(TriggerName.CrouchEnd),
                "sprint" or "run" => new BasicTrigger<WeaponSprintContext>(TriggerName.Sprint),
                "sprintend" or "runend" => new BasicTrigger<WeaponSprintEndContext>(TriggerName.SprintEnd),
                "jump" => new BasicTrigger<WeaponJumpContext>(TriggerName.Jump),
                "jumpend" => new BasicTrigger<WeaponJumpEndContext>(TriggerName.JumpEnd),
                string reference when reference.StartsWith("reference") => new ReferenceCallTrigger(origName.TrimStart(' ')["reference".Length..]),
                string key when key.StartsWith("key") => DetermineKeyTrigger(name),
                string init when init.ContainsAny("setup", "init", "drop") => new InitTrigger(name),
                string sync when sync.Contains("sync") => DetermineModSyncTrigger(origName, sync),
                string perTarget when perTarget.StartsWith("per") => DeterminePerTargetTrigger(name),
                string hitTaken when hitTaken.Contains("hittaken") => new PlayerDamageTypeTrigger(TriggerName.HitTaken, name.ToPlayerDamageTypes()),
                string damageTaken when damageTaken.Contains("damagetaken") => new DamageTakenTrigger(name.ToPlayerDamageTypes()),
                string landed when landed.Contains("landed") => DetermineLandedTrigger(landed),
                string prehit when prehit.Contains("prehit") => new DamageableTrigger<WeaponPreHitDamageableContext>(TriggerName.PreHit, name.ToDamageTypes()),
                string hit when hit.Contains("hit") => new DamageableTrigger<WeaponHitDamageableContext>(TriggerName.Hit, name.ToDamageTypes()),
                string miss when miss.Contains("miss") => new MissTrigger(name.ToDamageTypes()),
                string charge when charge.Contains("charge") => new ChargeTrigger(name.ToDamageTypes()),
                string backstab when backstab.Contains("backstab") => new BackstabTrigger(name.ToDamageTypes()),
                string kill when kill.Contains("kill") => new KillTrigger(name.ToDamageTypes()),
                string damage when damage.Contains("damage") => new DamageTrigger(name.ToDamageTypes()),
                string stagger when stagger.ContainsAny("stagger", "stun") => new StaggerTrigger(name.ToDamageTypes()),
                _ => new ReferenceCallTrigger(origName)
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

        private static ITrigger? DetermineModSyncTrigger(string origName, string name)
        {
            if (name == "modsync") return new ModSyncTrigger();

            origName = origName.Replace(" ", null);
            int sep = name.IndexOf("sync");
            if (sep == -1) return null;
            string id = origName[0..sep];
            if (string.IsNullOrEmpty(id)) return null;
            ITrigger? activate = GetTrigger(name[(sep+4)..]);
            if (activate == null) return null;
            return new ModSyncTrigger(activate, id);
        }

        private static ITrigger? DetermineKeyTrigger(string name)
        {
            name = name[3..];
            if (name.Length == 0) return null;

            bool onDown = true;
            int downIndex;

            if ((downIndex = name.IndexOf("down")) > -1)
            {
                if (downIndex == 0)
                    name = name[4..];
                else
                    name = name[..^4];
            }

            if ((downIndex = name.IndexOf("up")) > -1)
            {
                onDown = false;
                if (downIndex == 0)
                    name = name[2..];
                else
                    name = name[..^2];
            }

            if (name.Length == 0) return null;

            UnityEngine.KeyCode key;
            if (name.StartsWith("config"))
            {
                if (name.Length == 6) return null;

                key = name[6] switch
                {
                    '1' => Configuration.Keybind1,
                    '2' => Configuration.Keybind2,
                    '3' => Configuration.Keybind3,
                    '4' => Configuration.Keybind4,
                    _ => UnityEngine.KeyCode.None
                };
            }
            else
                key = name.ToEnum(UnityEngine.KeyCode.None);

            if (key == UnityEngine.KeyCode.None) return null;

            return new KeyTrigger(key, onDown);
        }
    }

    public interface IDamageTypeTrigger : ITrigger
    {
        DamageType[] DamageTypes { get; }
        DamageType BlacklistType { get; set; }
    }
}

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
        Hit,
        PreHit,
        Charge,
        Damage,
        Kill
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
                "bulletlanded" or "landedbullet" or "meleelanded" or "landedmelee" => new BasicTrigger<WeaponHitContext>(TriggerName.BulletLanded),
                string prehit when prehit.Contains("prehit") => new DamageableTrigger<WeaponPreHitDamageableContext>(TriggerName.PreHit, name.ToDamageType()),
                string hit when hit.Contains("hit") => new DamageableTrigger<WeaponHitDamageableContext>(TriggerName.Hit, name.ToDamageType()),
                string charge when charge.Contains("charge") => new ChargeTrigger(name.ToDamageType()),
                string damage when damage.Contains("damage") => new DamageTrigger(name.ToDamageType()),
                string kill when kill.Contains("kill") => new DamageTypeTrigger<WeaponPostKillContext>(TriggerName.Kill, name.ToDamageType()),
                _ => null
            };
        }
    }

    public interface IDamageTypeTrigger : ITrigger
    {
        DamageType DamageType { get; }
        DamageType BlacklistType { get; set; }
    }
}

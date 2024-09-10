using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public interface ITrigger
    {
        string Name { get; }
        float Invoke(WeaponTriggerContext context);
        void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options);

        public static ITrigger? GetTrigger(string? triggerName)
        {
            if (triggerName == null) return null;

            triggerName = triggerName.ToLowerInvariant().Replace(" ", null).Replace("on", null);
            return triggerName switch
            {
                "prefire" or "preshot" => new BasicTrigger<WeaponPreFireContext>(PreFire),
                "fire" or "shot" => new BasicTrigger<WeaponPostFireContext>(Fire),
                "aim" or "zoomin" => new BasicTrigger<WeaponAimContext>(Aim),
                "aimend" or "zoomout" => new BasicTrigger<WeaponAimEndContext>(AimEnd),
                "reloadstart" => new BasicTrigger<WeaponReloadStartContext>(ReloadStart),
                "reload" => new BasicTrigger<WeaponPostReloadContext>(Reload),
                "wield" => new BasicTrigger<WeaponWieldContext>(Wield),
                "bulletlanded" or "landedbullet" => new DamageTypeTrigger<WeaponPreHitContext>(BulletLanded, DamageType.Bullet),
                string hit when hit.Contains("hit") => new DamageTypeTrigger<WeaponPreHitEnemyContext>(Hit, ResolveDamageType(triggerName)),
                string damage when damage.Contains("damage") => new DamageTrigger(ResolveDamageType(triggerName)),
                string kill when kill.Contains("kill") => new DamageTypeTrigger<WeaponPostKillContext>(Kill, ResolveDamageType(triggerName)),
                _ => null
            };
        }

        public const string PreFire = "PreFire";
        public const string Fire = "Fire";
        public const string Aim = "Aim";
        public const string AimEnd = "AimEnd";
        public const string ReloadStart = "ReloadStart";
        public const string Reload = "Reload";
        public const string Wield = "Wield";
        public const string BulletLanded = "BulletLanded";
        public const string Hit = "Hit";
        public const string Damage = "Damage";
        public const string Kill = "Kill";

        private static DamageType ResolveDamageType(string name) => IDamageTypeTrigger.ResolveDamageType(name);
    }

    public interface IDamageTypeTrigger : ITrigger
    {
        DamageType DamageType { get; set; }
        DamageType BlacklistType { get; set; }

        public static DamageType ResolveDamageType(string? name)
        {
            if (name == null) return DamageType.Invalid;

            name = name.Replace(" ", null).ToLowerInvariant();
            DamageType flag = DamageType.Any;
            if (name.Contains("prec") || name.Contains("weakspot"))
                flag |= DamageType.Weakspot;
            if (name.Contains("bullet"))
                flag |= DamageType.Bullet;
            else if (name.Contains("explo"))
                flag |= DamageType.Explosive;
            else if (name.Contains("dot"))
                flag |= DamageType.DOT;
            return flag;
        }
    }
}

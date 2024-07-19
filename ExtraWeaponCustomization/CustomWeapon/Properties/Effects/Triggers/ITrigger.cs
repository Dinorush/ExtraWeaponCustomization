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

            triggerName = triggerName.Replace(" ", null).Replace("on", null).ToLowerInvariant();
            return triggerName switch
            {
                "fire" or "shot" => new BasicTrigger<WeaponPostFireContext>(Fire),
                "reload" => new BasicTrigger<WeaponPostReloadContext>(Reload),
                "wield" => new BasicTrigger<WeaponWieldContext>(Wield),
                "bulletlanded" => new BasicTrigger<WeaponPreHitContext>(BulletLanded),
                string hit when hit.Contains("hit") => new DamageFlagTrigger<WeaponPreHitEnemyContext>(Hit, ResolveDamageFlags(triggerName)),
                string damage when damage.Contains("damage") => new DamageTrigger(ResolveDamageFlags(triggerName)),
                string kill when kill.Contains("kill") => new DamageFlagTrigger<WeaponPostKillContext>(Kill, ResolveDamageFlags(triggerName)),
                _ => null
            };
        }

        public const string Fire = "Fire";
        public const string Reload = "Reload";
        public const string Wield = "Wield";
        public const string BulletLanded = "BulletLanded";
        public const string Hit = "Hit";
        public const string Damage = "Damage";
        public const string Kill = "Kill";

        private static DamageFlag ResolveDamageFlags(string name) => IDamageFlagTrigger.ResolveDamageFlags(name);
    }

    public interface IDamageFlagTrigger : ITrigger
    {
        DamageFlag Type { get; set; }
        DamageFlag BlacklistType { get; set; }

        public static DamageFlag ResolveDamageFlags(string? name)
        {
            if (name == null) return DamageFlag.Invalid;

            name = name.Replace(" ", null).ToLowerInvariant();
            DamageFlag flag = DamageFlag.Any;
            if (name.Contains("prec") || name.Contains("weakspot"))
                flag |= DamageFlag.Weakspot;
            if (name.Contains("bullet"))
                flag |= DamageFlag.Bullet;
            else if (name.Contains("explo"))
                flag |= DamageFlag.Explosive;
            else if (name.Contains("dot"))
                flag |= DamageFlag.DOT;
            return flag;
        }
    }
}

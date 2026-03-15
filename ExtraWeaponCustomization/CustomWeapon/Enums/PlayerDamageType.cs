using System;

namespace EWC.CustomWeapon.Enums
{
    [Flags]
    public enum PlayerDamageType
    {
        Invalid = -1,
        Any = 0,
        Enemy = 1,
        Tentacle = 1<<1 | Enemy,
        Shooter = 1<<2 | Enemy,
        Melee =  1<<3 | Enemy,
        Bleed = 1<<4 | Enemy,
        Player = 1<<5,
        Bullet = 1<<6 | Player,
        Explosive = 1<<7, // May be incurred by EEC/mine/EWC explosions, can't assume player
        DOT = 1<<8 | Player,
        Shrapnel = 1<<9 | Player,
        Heal = 1<<10 | Player,
        Syringe = 1<<11 | Player,
        Fall = 1<<12 | Player
    }

    public static class PlayerDamageTypeConst
    {
        public static readonly PlayerDamageType[] Any = new[] { PlayerDamageType.Any };
    }

    public static class PlayerDamageTypeMethods
    {
        public static PlayerDamageType[] ToPlayerDamageTypes(this string? name)
        {
            if (name == null) return new[] { PlayerDamageType.Invalid };

            name = name.Replace(" ", null).ToLowerInvariant();
            string[] names = name.Split('|');

            PlayerDamageType[] types = new PlayerDamageType[names.Length];
            for (int i = 0; i < names.Length; i++)
                types[i] = Internal_ToDamageType(names[i]);

            return types;
        }

        public static PlayerDamageType ToPlayerDamageType(this string? name)
        {
            if (name == null) return PlayerDamageType.Invalid;

            name = name.Replace(" ", null).ToLowerInvariant();
            return Internal_ToDamageType(name);
        }

        private static PlayerDamageType Internal_ToDamageType(string name)
        {
            PlayerDamageType flag = PlayerDamageType.Any;
            if (name.Contains("enemy"))
                flag |= PlayerDamageType.Enemy;
            else if (name.Contains("player") | name.Contains("friendly"))
                flag |= PlayerDamageType.Player;

            if (name.Contains("tentacle") | name.Contains("tongue"))
                flag |= PlayerDamageType.Tentacle;
            else if (name.Contains("shooter"))
                flag |= PlayerDamageType.Shooter;
            else if (name.Contains("melee"))
                flag |= PlayerDamageType.Melee;
            else if (name.Contains("bleed"))
                flag |= PlayerDamageType.Bleed;

            if (name.Contains("bullet"))
                flag |= PlayerDamageType.Bullet;
            else if (name.Contains("explo"))
                flag |= PlayerDamageType.Explosive;
            else if (name.Contains("dot"))
                flag |= PlayerDamageType.DOT;
            else if (name.Contains("shrapnel"))
                flag |= PlayerDamageType.Shrapnel;
            else if (name.Contains("heal"))
                flag |= PlayerDamageType.Heal;
            else if (name.Contains("syringe"))
                flag |= PlayerDamageType.Syringe;
            else if (name.Contains("fall"))
                flag |= PlayerDamageType.Fall;

            return flag;
        }

        public static bool HasAnyFlag(this PlayerDamageType type, PlayerDamageType flagSet) => (type & flagSet) != 0;
        public static bool HasFlagIn(this PlayerDamageType type, PlayerDamageType[] flagSet)
        {
            foreach (var flag in flagSet)
                if (type.HasFlag(flag))
                    return true;
            return false;
        }
    }
}

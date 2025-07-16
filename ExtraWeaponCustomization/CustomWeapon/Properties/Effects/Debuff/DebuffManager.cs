using EWC.Attributes;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EWC.CustomWeapon.Properties.Effects.Debuff
{
    public static class DebuffManager
    {
        private readonly static Dictionary<BaseDamageableWrapper, DebuffShotHolder[]> _shotMods = new();
        private readonly static Dictionary<BaseDamageableWrapper, DebuffBasicHolder> _armorShreds = new();

        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        [InvokeOnCleanup]
        private static void Reset()
        {
            _shotMods.Clear();
            _armorShreds.Clear();
        }

        public static DebuffModifierBase AddShotModDebuff(IDamageable damageable, float mod, StatType statType, StackType layer, DamageType[] damageType, uint group)
        {
            if (!_shotMods.TryGetValue(TempWrapper.Set(damageable), out var shotMods))
            {
                _shotMods
                    .Where(kv => !kv.Key.Alive)
                    .ToList()
                    .ForEach(kv =>
                    {
                        foreach(var holder in kv.Value)
                            holder?.Reset();
                        _shotMods.Remove(kv.Key);
                    });

                _shotMods.Add(new BaseDamageableWrapper(TempWrapper), shotMods = new DebuffShotHolder[StackTypeConst.Count]);
            }

            int statInt = (int)statType;
            shotMods[statInt] ??= new DebuffShotHolder();
            return shotMods[statInt].AddModifier(mod, layer, damageType, group);
        }

        public static bool TryGetShotModDebuff(IDamageable damageable, StatType statType, DamageType damageType, HashSet<uint> groups, out StackValue mod)
        {
            int statInt = (int)statType;
            if (_shotMods.TryGetValue(TempWrapper.Set(damageable), out var shotMods) && shotMods[statInt] != null)
            {
                mod = shotMods[statInt].GetMod(damageType, groups);
                return true;
            }
            mod = default;
            return false;
        }

        public static DebuffModifierBase AddArmorShredDebuff(IDamageable damageable, float mod, uint group)
        {
            if (!_armorShreds.TryGetValue(TempWrapper.Set(damageable), out var armorPierces))
            {
                _armorShreds
                    .Where(kv => !kv.Key.Alive)
                    .ToList()
                    .ForEach(kv =>
                    {
                        kv.Value.Reset();
                        _armorShreds.Remove(kv.Key);
                    });

                _armorShreds.Add(new BaseDamageableWrapper(TempWrapper), armorPierces = new());
            }

            return armorPierces.AddModifier(mod, group);
        }

        public static bool TryGetArmorShredDebuff(IDamageable damageable, HashSet<uint> groups, out float mod)
        {
            if (_armorShreds.TryGetValue(TempWrapper.Set(damageable), out var shotMods))
            {
                mod = shotMods.GetMod(groups);
                return true;
            }
            mod = 1f;
            return false;
        }

        public const uint DefaultGroup = 0u;
        public static readonly HashSet<uint> DefaultGroupSet = new(1) { DefaultGroup };
        private readonly static Dictionary<string, uint> s_stringToIDDict = new();
        private static uint s_nextID = uint.MaxValue;
        public static uint StringIDToInt(string id)
        {
            if (!s_stringToIDDict.ContainsKey(id))
                s_stringToIDDict.Add(id, s_nextID--);

            return s_stringToIDDict[id];
        }
    }
}

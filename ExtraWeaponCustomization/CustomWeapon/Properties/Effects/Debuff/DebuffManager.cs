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
        private readonly static Dictionary<BaseDamageableWrapper, DebuffStackHolder> _armorShreds = new();

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

                _shotMods.Add(new BaseDamageableWrapper(TempWrapper), shotMods = new DebuffShotHolder[StatTypeConst.Count]);
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

        public static DebuffModifierBase AddArmorShredDebuff(IDamageable damageable, float mod, StackType layer, uint group)
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

            return armorPierces.AddModifier(mod, layer, group);
        }

        public static bool TryGetArmorShredDebuff(IDamageable damageable, HashSet<uint> groups, out float mod)
        {
            if (_armorShreds.TryGetValue(TempWrapper.Set(damageable), out var shotMods))
            {
                mod = Math.Max(0f, shotMods.GetMod(groups).Value - 1);
                return true;
            }
            mod = 0f;
            return false;
        }

        public static void ApplyArmorShredDebuff(ref float armorMulti, float mod)
        {
            if (armorMulti >= 1) // If armor was already pierced
                armorMulti = Math.Max(armorMulti, mod);
            else if (mod < 1) // If partial shred
                armorMulti += (1f - armorMulti) * mod;
            else // If shred exceeds armor
                armorMulti = mod;
        }

        public static void GetAndApplyArmorShredDebuff(ref float armorMulti, IDamageable damageable, HashSet<uint> groups)
        {
            if (TryGetArmorShredDebuff(damageable, groups, out var mod))
                ApplyArmorShredDebuff(ref armorMulti, mod);
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

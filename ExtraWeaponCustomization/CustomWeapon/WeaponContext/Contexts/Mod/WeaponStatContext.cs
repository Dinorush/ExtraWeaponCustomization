using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Structs;
using EWC.Utils;
using System.Collections.Generic;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponStatContext : IWeaponContext
    {
        private readonly StackMod _damageMod;
        public float Damage => CalcMod(StatType.Damage);
        private readonly StackMod _precisionMod;
        public float Precision => CalcMod(StatType.Precision);
        private readonly StackMod _staggerMod;
        public float Stagger => CalcMod(StatType.Stagger);

        public DamageType DamageType { get; }
        public bool BypassTumorCap { get; set; }
        public IDamageable Damageable { get; set; }
        public ShotInfo ShotInfo { get; }

        private readonly HashSet<uint> _debuffIDs;

        public WeaponStatContext(HitData data, HashSet<uint> debuffIDs) : this(data.damage, data.precisionMulti, data.staggerMulti, data.damageType, data.damageable!, data.shotInfo, debuffIDs) { }
        public WeaponStatContext(float damage, float precision, float stagger, DamageType damageType, IDamageable damageable, ShotInfo shotInfo, HashSet<uint> debuffIDs)
        {
            _damageMod = new(damage);
            _precisionMod = new(precision);
            _staggerMod = new(stagger);
            _debuffIDs = debuffIDs;
            DamageType = damageType;
            Damageable = damageable;
            ShotInfo = shotInfo;
            BypassTumorCap = false;
        }

        public void AddMod(StatType type, float mod, StackType layer)
        {
            switch (type)
            {
                case StatType.Damage:
                    _damageMod.AddMod(mod, layer);
                    break;
                case StatType.Precision:
                    _precisionMod.AddMod(mod, layer);
                    break;
                case StatType.Stagger:
                    _staggerMod.AddMod(mod, layer);
                    break;
            }
        }

        private float CalcMod(StatType type)
        {
            StackMod baseMod;
            ShotStackMod shotMod;
            ShotStackMod groupMod;
            switch (type)
            {
                case StatType.Damage:
                    baseMod = _damageMod;
                    shotMod = ShotInfo.Mod.Damage;
                    groupMod = ShotInfo.GroupMod.Damage;
                    break;
                case StatType.Precision:
                    baseMod = _precisionMod;
                    shotMod = ShotInfo.Mod.Precision;
                    groupMod = ShotInfo.GroupMod.Precision;
                    break;
                case StatType.Stagger:
                    baseMod = _staggerMod;
                    shotMod = ShotInfo.Mod.Stagger;
                    groupMod = ShotInfo.GroupMod.Stagger;
                    break;
                default:
                    Utils.Log.EWCLogger.Error($"Invalid stat type in StatContext! {type}");
                    return 1f;
            }

            StackValue result = baseMod.StackValue;
            if (shotMod.HasMod(DamageType, Damageable))
                result.Combine(shotMod.StackValue);
            if (groupMod.HasMod(DamageType, Damageable))
                result.Combine(groupMod.StackValue);
            if (DebuffManager.TryGetShotModDebuff(Damageable, type, DamageType, _debuffIDs, out var stackValue))
                result.Combine(stackValue);
            return result.Value * baseMod.BaseValue;
        }
    }
}

using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils;
using EWC.Utils.Log;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponStatContext : IWeaponContext
    {
        private readonly StackMod _damageMod;
        public float Damage => CalcMod(_damageMod, ShotInfo.Mod.Damage, ShotInfo.GroupMod.Damage);
        private readonly StackMod _precisionMod;
        public float Precision => CalcMod(_precisionMod, ShotInfo.Mod.Precision, ShotInfo.GroupMod.Precision);
        private readonly StackMod _staggerMod;
        public float Stagger => CalcMod(_staggerMod, ShotInfo.Mod.Stagger, ShotInfo.GroupMod.Stagger);

        public DamageType DamageType { get; }
        public bool BypassTumorCap { get; set; }
        public IDamageable Damageable { get; set; }
        public ShotInfo ShotInfo { get; }

        public WeaponStatContext(HitData data) : this(data.damage, data.precisionMulti, data.staggerMulti, data.damageType, data.damageable!, data.shotInfo) { }
        public WeaponStatContext(float damage, float precision, float stagger, DamageType damageType, IDamageable damageable, ShotInfo shotInfo)
        {
            _damageMod = new(damage);
            _precisionMod = new(precision);
            _staggerMod = new(stagger);
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

        private float CalcMod(StackMod stack, ShotStackMod shotMod, ShotStackMod groupMod)
        {
            return stack.Combine(DamageType, Damageable, shotMod, groupMod);
        }
    }
}

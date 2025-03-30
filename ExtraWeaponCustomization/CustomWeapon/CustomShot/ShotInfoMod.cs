using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Triggers;

namespace EWC.CustomWeapon.CustomShot
{
    public sealed class ShotInfoMod
    {
        public readonly ShotStackMod Damage;
        public readonly ShotStackMod Precision;
        public readonly ShotStackMod Stagger;

        public ShotInfoMod()
        {
            Damage = new();
            Precision = new();
            Stagger = new();
        }

        public ShotInfoMod(ShotInfoMod mod)
        {
            Damage = new(mod.Damage);
            Precision = new(mod.Precision);
            Stagger = new(mod.Stagger);
        }

        public void Add(TriggerMod triggerMod, StatType type, float mod, IDamageable? damageable = null, params DamageType[] types) => Add(triggerMod, type, mod, triggerMod.Cap, triggerMod.StackType, triggerMod.StackLayer, damageable, types);

        public void Add(WeaponPropertyBase property, StatType type, float mod, float cap, StackType stack, StackType layer, IDamageable? damageable = null, params DamageType[] types)
        {
            switch (type)
            {
                case StatType.Damage:
                    Damage.AddMod(property, mod, cap, stack, layer, damageable, types);
                    break;
                case StatType.Precision:
                    Precision.AddMod(property, mod, cap, stack, layer, damageable, types);
                    break;
                case StatType.Stagger:
                    Stagger.AddMod(property, mod, cap, stack, layer, damageable, types);
                    break;
            }
        }

        public void Reset()
        {
            Damage.Reset();
            Precision.Reset();
            Stagger.Reset();
        }
    }
}

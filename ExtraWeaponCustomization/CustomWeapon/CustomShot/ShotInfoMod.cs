using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Triggers;

namespace EWC.CustomWeapon.CustomShot
{
    public sealed class ShotInfoMod
    {
        public readonly ShotStackMod Damage = new();
        public readonly ShotStackMod Precision = new();
        public readonly ShotStackMod Stagger = new();

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

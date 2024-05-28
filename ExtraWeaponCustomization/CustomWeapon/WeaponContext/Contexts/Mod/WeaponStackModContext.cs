using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using Gear;
namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponStackModContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public float Value
        {
            get
            {
                return _value * (_overrideMod >= 0 ? _overrideMod : _addMod * _multMod);
            }
        }

        private readonly float _value = 0;
        private float _addMod = 1f;
        private float _multMod = 1f;
        private float _overrideMod = -1f;

        public WeaponStackModContext(float value, BulletWeapon weapon)
        {
            _value = value;
            Weapon = weapon;
        }

        public void AddMod(float mod, StackType type)
        {
            if (mod == 1f) return;

            switch (type)
            {
                case StackType.Override:
                    _overrideMod = mod;
                    break;
                case StackType.Add:
                    _addMod += (mod - 1);
                    break;
                case StackType.Multiply:
                    _multMod *= mod;
                    break;
            }
        }
    }
}

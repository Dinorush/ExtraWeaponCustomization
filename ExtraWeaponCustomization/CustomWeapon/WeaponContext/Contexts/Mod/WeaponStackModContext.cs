using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using Gear;
namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponStackModContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        protected readonly StackMod _stackMod;
        public float Value => _stackMod.Value;

        public WeaponStackModContext(float value, BulletWeapon weapon)
        {
            _stackMod = new(value);
            Weapon = weapon;
        }

        public void AddMod(float mod, StackType type) => _stackMod.AddMod(mod, type);
    }
}

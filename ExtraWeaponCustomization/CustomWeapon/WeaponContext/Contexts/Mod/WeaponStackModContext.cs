using EWC.CustomWeapon.Properties.Effects;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponStackModContext : IWeaponContext
    {

        protected readonly StackMod _stackMod;
        public float Value => _stackMod.Value;

        public WeaponStackModContext(float value)
        {
            _stackMod = new(value);
        }

        public void AddMod(float mod, StackType type) => _stackMod.AddMod(mod, type);
    }
}

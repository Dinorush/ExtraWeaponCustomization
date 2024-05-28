using Agents;

namespace ExtraWeaponCustomization.CustomWeapon.ObjectWrappers
{
    internal class DamageableWrapper : KeyWrapper
    {
        // Used so a new wrapper need not be created when looking for an existing one.
        // Should ONLY be used by first calling SetAgent (i.e. assume its current state is garbage)
        public static readonly DamageableWrapper SharedInstance = new(null, 0);
        public IDamageable? Damageable { get; private set; }

        public DamageableWrapper(IDamageable? damageable, int iD) : base(iD)
        {
            Damageable = damageable;
        }

        public DamageableWrapper(DamageableWrapper wrapper) : base(wrapper.ID)
        {
            Damageable = wrapper.Damageable;
        }

        public void SetDamageable(IDamageable damageable, int iD)
        {
            ID = iD;
            Damageable = damageable;
        }
    }
}

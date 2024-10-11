using System;

namespace EWC.CustomWeapon.ObjectWrappers
{
    internal class DamageableWrapper : KeyWrapper
    {
        // Used so a new wrapper need not be created when looking for an existing one.
        // Should ONLY be used by first calling SetAgent (i.e. assume its current state is garbage)
        public static readonly DamageableWrapper SharedInstance = new(null, IntPtr.Zero);
        public IDamageable? Damageable { get; private set; }

        public DamageableWrapper(IDamageable? damageable, IntPtr ptr = default) : base(ptr)
        {
            if (ptr == IntPtr.Zero && damageable != null)
                Pointer = damageable.Pointer;
            Damageable = damageable;
        }

        public DamageableWrapper(DamageableWrapper wrapper) : base(wrapper.Pointer)
        {
            Damageable = wrapper.Damageable;
        }

        public void SetDamageable(IDamageable damageable, IntPtr ptr)
        {
            Pointer = ptr;
            Damageable = damageable;
        }
    }
}

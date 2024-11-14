using Agents;
using System;

namespace EWC.CustomWeapon.ObjectWrappers
{
    internal class BaseDamageableWrapper : ObjectWrapper<IDamageable>
    {
        public static readonly new BaseDamageableWrapper SharedInstance = new(null, IntPtr.Zero);
        public bool Alive => Object != null && Object.GetHealthRel() > 0;

        public BaseDamageableWrapper(BaseDamageableWrapper wrapper) : base(wrapper) { }

        public BaseDamageableWrapper(IDamageable? obj, IntPtr ptr) : base(obj?.GetBaseDamagable(), ptr) { }

        public BaseDamageableWrapper(IDamageable obj) : base(obj.GetBaseDamagable()) { }

        public override void SetObject(IDamageable damageable)
        {
            damageable = damageable.GetBaseDamagable();
            base.SetObject(damageable);
        }
    }
}

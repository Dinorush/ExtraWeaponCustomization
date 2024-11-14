using Agents;
using System;

namespace EWC.CustomWeapon.ObjectWrappers
{
    internal class BaseDamageableWrapper : ObjectWrapper<IDamageable>
    {
        public static readonly new BaseDamageableWrapper SharedInstance = new(null, IntPtr.Zero);
        public Agent? Agent { get; private set; }

        public BaseDamageableWrapper(BaseDamageableWrapper wrapper) : base(wrapper)
        {
            Agent = wrapper.Agent;
        }

        public BaseDamageableWrapper(IDamageable? obj, IntPtr ptr) : base(obj?.GetBaseDamagable(), ptr)
        {
            Agent = obj?.GetBaseAgent();
        }

        public BaseDamageableWrapper(IDamageable obj) : base(obj.GetBaseDamagable())
        {
            Agent = obj.GetBaseAgent();
        }

        public override void SetObject(IDamageable damageable)
        {
            damageable = damageable.GetBaseDamagable();
            base.SetObject(damageable);
            Agent = damageable.GetBaseAgent();
        }
    }
}

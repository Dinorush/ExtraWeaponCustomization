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

        public override BaseDamageableWrapper Set(IDamageable damageable)
        {
            damageable = damageable.GetBaseDamagable();
            base.Set(damageable);
            return this;
        }
    }

    internal class BaseDamageableWrapper<T> : ObjectWrapper<T> where T : Dam_SyncedDamageBase
    {
        public static readonly new BaseDamageableWrapper<T> SharedInstance = new(null, IntPtr.Zero);
        public bool Alive => Object != null && Object.GetHealthRel() > 0;

        public BaseDamageableWrapper(BaseDamageableWrapper<T> wrapper) : base(wrapper) { }

        public BaseDamageableWrapper(IDamageable? obj, IntPtr ptr) : base(obj?.GetBaseDamagable().Cast<T>(), ptr) { }

        public BaseDamageableWrapper(IDamageable obj) : base(obj.GetBaseDamagable().Cast<T>()) { }

        public override BaseDamageableWrapper<T> Set(T damageable)
        {
            base.Set(damageable);
            return this;
        }

        public BaseDamageableWrapper<T> Set(IDamageable damageable)
        {
            base.Set(damageable.GetBaseDamagable().Cast<T>());
            return this;
        }
    }
}

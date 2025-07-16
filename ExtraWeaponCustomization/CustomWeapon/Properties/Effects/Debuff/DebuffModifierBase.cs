namespace EWC.CustomWeapon.Properties.Effects.Debuff
{
    public abstract class DebuffModifierBase
    {
        private float _mod = 0f;
        public float Mod
        {
            get => _mod;
            set
            {
                var oldMod = _mod;
                _mod = value;
                if (Active && oldMod != _mod)
                    RefreshGroup();
            }
        }

        public bool Active { get; internal set; }

        public DebuffModifierBase(float mod)
        {
            Active = false;
            _mod = mod;
        }

        protected abstract void RefreshGroup();
        protected abstract void AddToGroup();
        protected abstract void RemoveFromGroup();

        public void Enable()
        {
            if (Active) return;
            Active = true;
            AddToGroup();
            RefreshGroup();
        }

        public void Enable(float mod)
        {
            if (!Active)
            {
                _mod = mod;
                Enable();
            }
            else
                Mod = mod;
        }

        public void Disable()
        {
            if (!Active) return;
            Active = false;
            RemoveFromGroup();
            RefreshGroup();
        }
    }
}

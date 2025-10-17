using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Dependencies;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects.Spread
{
    // Since we need to know the crosshair size at all times, we can't calculate only at shot time
    // like other mod effects (e.g. FireRateMod). This manages merging spread mods together
    // and updating the crosshair size if it changes.
    public sealed class SpreadController
    {
        private Dictionary<SpreadMod, float>? _mods;
        private Dictionary<SpreadMod, float> Mods => _mods ??= new (3);

        private readonly bool _isLocal;
        private bool _active = false;
        public bool Active
        { 
            get => _active;
            set
            {
                if (_active == value || !_isLocal) return;

                if (value)
                    ACAPIWrapper.UpdateCrosshairSpread(Value);
                else
                    ACAPIWrapper.ResetCrosshairSpread();
                _active = value;
            }
        }

        public float Value => _stackMod.Value;

        private readonly StackMod _stackMod;

        public SpreadController(bool local)
        {
            _stackMod = new(1f, 0f);
            _mods = null;
            _isLocal = local;
        }

        public void Reset()
        {
            _stackMod.Reset(1f, 0f);
            _mods = null;
            if (Active)
                ACAPIWrapper.ResetCrosshairSpread();
        }

        public void ClearMod(SpreadMod spreadMod)
        {
            Mods.Remove(spreadMod);
            Recompute();

            if (Active)
                ACAPIWrapper.UpdateCrosshairSpread(Value);
        }

        public void SetMod(SpreadMod spreadMod, float newMod)
        {
            if (Mods.TryGetValue(spreadMod, out float oldMod) && oldMod == newMod) return;

            Mods[spreadMod] = newMod;
            Recompute();

            if (Active)
                ACAPIWrapper.UpdateCrosshairSpread(Value);
        }

        private void Recompute()
        {
            _stackMod.Reset(1f, 0f);
            foreach (var (spreadProp, mod) in Mods)
                _stackMod.AddMod(mod, spreadProp.StackLayer);
        }
    }
}

using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Dependencies;
using System.Collections.Generic;

namespace EWC.CustomWeapon.CustomShot
{
    // Since we need to know the crosshair size at all times, we can't calculate only at shot time
    // like other mod effects (e.g. FireRateMod). This manages merging spread mods together
    // and updating the crosshair size if it changes.
    public sealed class SpreadController
    {
        private Dictionary<SpreadMod, float>? _mods;
        private Dictionary<SpreadMod, float> Mods => _mods ??= new (3);

        private readonly bool _modifyCrosshair;
        private bool _active = false;
        public bool Active
        { 
            get => _active;
            set
            {
                if (_active == value || !_modifyCrosshair) return;

                _active = value;

                if (value)
                    ACAPIWrapper.UpdateCrosshairSpread(Value);
                else
                    ACAPIWrapper.ResetCrosshairSpread();
            }
        }

        public float Value => _stackMod.Value;

        private readonly StackMod _stackMod;

        public SpreadController(OwnerType owner, WeaponType weapon)
        {
            _stackMod = new(1f, 0f);
            _mods = null;
            _modifyCrosshair = owner.HasFlag(OwnerType.Local) && weapon.HasFlag(WeaponType.Gun);
        }

        public void Reset()
        {
            _stackMod.Reset(1f, 0f);
            _mods = null;

            if (Active)
                ACAPIWrapper.ResetCrosshairSpread();
        }

        public void ClearMod(SpreadMod spreadMod, bool updateCrosshair = true)
        {
            Mods.Remove(spreadMod);
            Recompute();

            if (updateCrosshair && Active)
                ACAPIWrapper.UpdateCrosshairSpread(Value);
        }

        public void SetMod(SpreadMod spreadMod, float newMod, bool updateCrosshair = true)
        {
            if (Mods.TryGetValue(spreadMod, out float oldMod) && oldMod == newMod) return;

            Mods[spreadMod] = newMod;
            Recompute();

            if (updateCrosshair && Active)
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

using EWC.CustomWeapon.Properties.Effects.Triggers;
using System;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpreadMod :
        TriggerModTimed,
        IGunProperty
    {
        public override bool ShouldRegister(Type contextType) => CWC.IsLocal && base.ShouldRegister(contextType);

        protected override void OnUpdate(float mod) => CWC.SpreadController!.SetMod(this, mod);
        protected override void OnDisable() => CWC.SpreadController!.ClearMod(this);
    }
}

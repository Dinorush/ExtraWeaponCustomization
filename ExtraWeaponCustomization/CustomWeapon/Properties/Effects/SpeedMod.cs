using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using MovementSpeedAPI;
using System;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpeedMod :
        TriggerModTimed,
        IGunProperty,
        IMeleeProperty
    {
        private const string APIGroup = "EWC";

        private ISpeedModifier _speedModifier = null!;

        public override bool ShouldRegister(Type contextType) => CWC.IsLocal && base.ShouldRegister(contextType);

        protected override void OnUpdate(float mod) => _speedModifier.Enable(mod);
        protected override void OnDisable() => _speedModifier.Disable();

        public override WeaponPropertyBase Clone()
        {
            var copy = (SpeedMod) base.Clone();
            copy._speedModifier = MoveSpeedAPI.AddModifier(1f, LayerToAPILayer(), APIGroup);
            copy._speedModifier.Disable();
            return copy;
        }

        private StackLayer LayerToAPILayer()
        {
            return StackLayer switch
            {
                StackType.Override => MovementSpeedAPI.StackLayer.Override,
                StackType.Max or StackType.Min => Mod > 1 ? MovementSpeedAPI.StackLayer.Max : MovementSpeedAPI.StackLayer.Min,
                StackType.Mult => MovementSpeedAPI.StackLayer.Multiply,
                StackType.Add => MovementSpeedAPI.StackLayer.Add,
                _ => MovementSpeedAPI.StackLayer.Multiply
            };
        }
    }
}

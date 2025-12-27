using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using ModifierAPI;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpeedMod :
        TriggerModTimed
    {
        private const string APIGroup = "EWC";

        private IStatModifier _speedModifier = null!;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;

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
                StackType.Override => ModifierAPI.StackLayer.Override,
                StackType.Max or StackType.Min => Mod > 1 ? ModifierAPI.StackLayer.Max : ModifierAPI.StackLayer.Min,
                StackType.Mult => ModifierAPI.StackLayer.Multiply,
                StackType.Add => ModifierAPI.StackLayer.Add,
                _ => ModifierAPI.StackLayer.Multiply
            };
        }
    }
}

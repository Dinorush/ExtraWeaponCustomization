using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class KillTrigger : HitTrackerTrigger<WeaponPostKillContext>
    {
        public KillTrigger(params DamageType[] types) : base(TriggerName.Kill, types)
        {
            BlacklistType &= ~DamageType.Dead;
        }
    }
}

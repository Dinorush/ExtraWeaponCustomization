﻿using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class BulletLandedTrigger : DamageTypeTrigger<WeaponHitContextBase>
    {
        public BulletLandedTrigger(params DamageType[] damageTypes) : base(TriggerName.BulletLanded, damageTypes) {}

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            // Want to trigger when a bullet lands but NOT on a pre-hit context.
            if (base.Invoke(context, out amount) && (context is WeaponHitContext || context is WeaponHitDamageableContext))
                return true;
            amount = 0;
            return false;
        }
    }
}

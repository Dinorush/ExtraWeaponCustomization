﻿using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponHitContext : WeaponHitContextBase
    {
        public WeaponHitContext(Collider collider, Vector3 position, Vector3 direction, float falloff) :
            base(collider, position, direction, falloff, Properties.Effects.Triggers.DamageType.Bullet) {}

        public WeaponHitContext(HitData data) :
            this(data.collider, data.hitPos, data.fireDir.normalized, data.falloff) {}
    }
}

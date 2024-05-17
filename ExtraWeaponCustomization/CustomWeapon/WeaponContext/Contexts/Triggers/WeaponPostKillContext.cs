﻿using Enemies;
using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostKillContext : WeaponTriggerContext
    {
        public EnemyAgent Enemy { get; }

        public WeaponPostKillContext(EnemyAgent enemy, BulletWeapon weapon) : base(weapon, TriggerType.OnKill)
        {
            Enemy = enemy;
        }
    }
}

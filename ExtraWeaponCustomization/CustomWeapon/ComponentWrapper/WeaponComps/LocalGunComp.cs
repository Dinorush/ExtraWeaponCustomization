using Gear;
using System;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public class LocalGunComp : BulletWeaponComp<BulletWeapon>
    {
        private BulletWeaponArchetype _gunArchetype;
        private BWA_Burst? _burstArchetype;

        public LocalGunComp(BulletWeapon value) : base(value, value.TryCast<Shotgun>() != null)
        {
            _gunArchetype = value.m_archeType;
        }

        public BulletWeaponArchetype GunArchetype
        {
            get => _gunArchetype;
            set
            {
                Value.m_archeType = value;
                _gunArchetype = value;
                FireMode = ArchetypeData.FireMode;
                _burstArchetype = FireMode == eWeaponFireMode.Burst ? value!.Cast<BWA_Burst>() : null;
            }
        }

        public bool TryGetBurstArchetype([MaybeNullWhen(false)] out BWA_Burst burstArchetype)
        {
            burstArchetype = _burstArchetype;
            return FireMode == eWeaponFireMode.Burst;
        }

        public override bool IsAiming => Value.FPItemHolder.ItemAimTrigger;

        public override float ModifyFireRate(float lastFireTime, float shotDelay, float burstDelay, float cooldownDelay)
        {
            burstDelay = Math.Max(shotDelay, burstDelay);
            GunArchetype.m_nextShotTimer = lastFireTime + shotDelay;
            if (GunArchetype.BurstIsDone())
                GunArchetype.m_nextBurstTimer = GunArchetype.HasCooldown ? lastFireTime + cooldownDelay : Math.Max(lastFireTime + burstDelay, GunArchetype.m_nextShotTimer);
            API.FireRateAPI.FireCooldownSetCallback(Value, shotDelay, burstDelay, cooldownDelay);
            return Math.Max(GunArchetype.m_nextShotTimer, GunArchetype.m_nextBurstTimer);
        }
    }
}

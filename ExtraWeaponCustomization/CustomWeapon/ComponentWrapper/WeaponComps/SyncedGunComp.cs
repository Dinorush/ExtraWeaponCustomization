using Gear;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public class SyncedGunComp : BulletWeaponComp<BulletWeaponSynced>
    {
        public SyncedGunComp(BulletWeaponSynced value) : base(value, value.TryCast<ShotgunSynced>() != null) { }

        public override bool IsAiming => true;
        public override float ModifyFireRate(float lastFireTime, float shotDelay, float burstDelay, float cooldownDelay)
        {
            return Value.m_lastFireTime = lastFireTime + shotDelay - ArchetypeData.ShotDelay;
        }
    }
}

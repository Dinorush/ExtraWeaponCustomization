using EWC.CustomWeapon.Enums;
using GameData;
using Player;
using System;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public class SentryHolderComp : WeaponComp<SentryGunFirstPerson>, IAmmoComp
    {
        private ArchetypeDataBlock _archetypeData;
        public SentryHolderComp(SentryGunFirstPerson value) : base(value)
        {
            _archetypeData = value.ArchetypeData;
            CostOfBullet = _archetypeData.CostOfBullet * Math.Max(1, _archetypeData.ShotgunBulletCount) * Value.ItemDataBlock.ClassAmmoCostFactor;
        }

        public override WeaponType Type => WeaponType.SentryHolder;
        public override AmmoType AmmoType => Value.AmmoType;
        public override CellSoundPlayer Sound => Value.Sound;

        public override ArchetypeDataBlock ArchetypeData
        {
            get => _archetypeData;
            set
            {
                _archetypeData = value;
                Value.ArchetypeData = value;
                CostOfBullet = _archetypeData.CostOfBullet * Math.Max(1, _archetypeData.ShotgunBulletCount) * Value.ItemDataBlock.ClassAmmoCostFactor;
            }
        }

        public float CostOfBullet { get; private set; }

        public int GetCurrentClip() => 0;
        public int GetMaxClip() => 0;
        public void SetCurrentClip(int clip) { }
        public override bool AllowBackstab => true;
    }
}

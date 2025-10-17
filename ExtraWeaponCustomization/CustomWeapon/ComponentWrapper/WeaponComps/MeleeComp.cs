using EWC.CustomWeapon.Enums;
using Gear;
using Player;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public class MeleeComp : WeaponComp<MeleeWeaponFirstPerson>
    {
        public MeleeComp(MeleeWeaponFirstPerson value) : base(value) { }

        public override WeaponType Type => WeaponType.Melee;
        public override AmmoType AmmoType => AmmoType.None;
        public override CellSoundPlayer Sound => Value.Sound;
        public override bool AllowBackstab => true;
    }
}

using EWC.CustomWeapon.Enums;
using Gear;
using Player;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public class SyncedMeleeComp : WeaponComp<MeleeWeaponThirdPerson>
    {
        public SyncedMeleeComp(MeleeWeaponThirdPerson value) : base(value) { }

        public override WeaponType Type => WeaponType.Melee;
        public override AmmoType AmmoType => AmmoType.None;
        public override InventorySlot InventorySlot => InventorySlot.GearMelee;
        public override CellSoundPlayer Sound => Value.Sound;
        public override bool AllowBackstab => true;
    }
}

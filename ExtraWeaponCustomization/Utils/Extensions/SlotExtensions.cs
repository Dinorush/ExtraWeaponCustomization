using Player;

namespace EWC.Utils.Extensions
{
    internal static class SlotExtensions
    {
        public static InventorySlot ToInventorySlot(this AmmoType ammo)
        {
            return ammo switch
            {
                AmmoType.Standard => InventorySlot.GearStandard,
                AmmoType.Special => InventorySlot.GearSpecial,
                AmmoType.Class => InventorySlot.GearClass,
                AmmoType.ResourcePackRel => InventorySlot.ResourcePack,
                _ => InventorySlot.None
            };
        }

        public static AmmoType ToAmmoType(this InventorySlot slot)
        {
            return slot switch
            {
                InventorySlot.GearStandard=> AmmoType.Standard,
                InventorySlot.GearSpecial => AmmoType.Special,
                InventorySlot.GearClass => AmmoType.Class,
                InventorySlot.ResourcePack => AmmoType.ResourcePackRel,
                _ => AmmoType.None
            };
        }
    }
}

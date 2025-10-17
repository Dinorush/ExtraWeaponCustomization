using EWC.CustomWeapon.Enums;
using EWC.Utils.Extensions;
using Player;

namespace EWC.CustomWeapon.ComponentWrapper
{
    public abstract class WeaponComp<T> : IWeaponComp where T : ItemEquippable
    {
        public readonly T Value;

        public WeaponComp(T value)
        {
            Value = value;
        }

        public ItemEquippable Component => Value;
        public abstract WeaponType Type { get; }
        public abstract AmmoType AmmoType { get; }
        public abstract CellSoundPlayer Sound { get; }
        public abstract bool AllowBackstab { get; }
    }

    public interface IWeaponComp
    {
        public ItemEquippable Component { get; }
        public WeaponType Type { get; }
        public bool IsType(WeaponType type) => Type.HasFlag(type);
        public bool IsAnyType(WeaponType type) => Type.HasAnyFlag(type);
        public AmmoType AmmoType { get; }
        public InventorySlot InventorySlot => AmmoType.ToInventorySlot();
        public CellSoundPlayer Sound { get; }
        public bool AllowBackstab { get; }
    }
}

using EWC.CustomWeapon.Enums;
using EWC.Utils.Extensions;
using FX_EffectSystem;
using GameData;
using Gear;
using Player;
using System;

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

        // Defaults to support shot effects on non-guns
        public virtual ArchetypeDataBlock ArchetypeData { get => throw new NotImplementedException($"WeaponComp does not support ArchetypeData - is this called on an invalid weapon?"); set => throw new NotImplementedException($"WeaponComp does not support ArchetypeData - is this called on an invalid weapon?"); }
        public virtual bool IsShotgun => false;
        public virtual bool IsAiming => false;
        public virtual Weapon.WeaponHitData VanillaHitData => Weapon.s_weaponRayData ??= new();
        public virtual FX_Pool TracerPool => BulletWeapon.s_tracerPool;
        public virtual float MaxRayDist { get; set; } = 100f;
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

        // Shot-related fields
        public ArchetypeDataBlock ArchetypeData { get; set; }
        public bool IsShotgun { get; }
        public bool IsAiming { get; }
        public Weapon.WeaponHitData VanillaHitData { get; }
        public FX_Pool TracerPool { get; }
        public float MaxRayDist { get; set; }
    }
}

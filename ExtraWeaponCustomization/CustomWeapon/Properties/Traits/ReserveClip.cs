using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using Player;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class ReserveClip :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPostAmmoPackContext>,
        IWeaponProperty<WeaponPreAmmoUIContext>,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponWieldContext>
    {
        public void Invoke(WeaponPostStartFireContext context)
        {
            if (!CWC.TryGetBurstArchetype(out var arch)) return;

            int bullets = CWC.Weapon.GetCurrentClip() + PlayerBackpackManager.GetBulletsInPack(CWC.Weapon.AmmoType, CWC.Weapon.Owner.Owner);
            arch.m_burstCurrentCount = Math.Min(arch.m_burstMax, bullets);
        }

        public void Invoke(WeaponPostAmmoPackContext context)
        {
            BulletWeapon weapon = CWC.Gun!;
            if (weapon.Owner.Inventory.WieldedItem != CWC.Weapon)
            {
                weapon.SetCurrentClip(context.AmmoStorage.GetClipBulletsFromPack(weapon.GetCurrentClip(), weapon.AmmoType));
                weapon.m_wasOutOfAmmo = false;
            }
            else
                weapon.Owner.Inventory.DoReload();
        }

        public void Invoke(WeaponPostFireContext context)
        {
            CWC.Weapon.Owner.Inventory.DoReload();
        }

        public void Invoke(WeaponWieldContext context)
        {
            CWC.Weapon.Owner.Inventory.DoReload();
        }

        public void Invoke(WeaponPreAmmoUIContext context)
        {
            context.Reserve += context.Clip;
            context.ShowClip = false;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }
    }
}

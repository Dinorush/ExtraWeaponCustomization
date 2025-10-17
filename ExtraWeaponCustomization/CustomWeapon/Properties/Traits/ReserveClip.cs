using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Player;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class ReserveClip :
        Trait,
        IWeaponProperty<WeaponPostAmmoPackContext>,
        IWeaponProperty<WeaponPreAmmoUIContext>,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponWieldContext>
    {
        protected override OwnerType RequiredOwnerType => OwnerType.Managed | OwnerType.Player;
        protected override WeaponType ValidWeaponType => WeaponType.Gun;

        public void Invoke(WeaponPostStartFireContext context)
        {
            if (!CWC.Owner.IsType(OwnerType.Local)) return;

            var gun = (LocalGunComp)CGC.Gun;
            if (!gun.TryGetBurstArchetype(out var arch)) return;

            int bullets = gun.GetCurrentClip() + PlayerBackpackManager.GetBulletsInPack(gun.AmmoType, CWC.Owner.Player.Owner);
            arch.m_burstCurrentCount = Math.Min(arch.m_burstMax, bullets);
        }

        public void Invoke(WeaponPostAmmoPackContext context)
        {
            var gun = (IBulletWeaponComp)CGC.Gun;
            if (CWC.Owner.Player.Inventory.WieldedItem != gun.BulletWeapon)
            {
                gun.SetCurrentClip(context.AmmoStorage.GetClipBulletsFromPack(gun.GetCurrentClip(), gun.AmmoType));
                gun.BulletWeapon.m_wasOutOfAmmo = false;
            }
            else
                CWC.Owner.Player.Inventory.DoReload();
        }

        public void Invoke(WeaponPostFireContext context)
        {
            CWC.Owner.Player.Inventory.DoReload();
        }

        public void Invoke(WeaponWieldContext context)
        {
            CWC.Owner.Player.Inventory.DoReload();
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

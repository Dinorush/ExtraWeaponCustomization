﻿using ExtraWeaponCustomization.Networking;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers
{
    internal sealed class EWCProjectileSyncDestroy : SyncedEvent<ProjectileDataDestroy>
    {
        public override string GUID => "PROJDST";

        protected override void Receive(ProjectileDataDestroy packet)
        {
            EWCProjectileManager.Internal_ReceiveProjectileDestroy(packet.characterIndex, packet.id);
        }
    }
}
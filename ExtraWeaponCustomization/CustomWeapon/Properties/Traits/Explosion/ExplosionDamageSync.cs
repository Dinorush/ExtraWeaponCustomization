using ExtraWeaponCustomization.Networking;
using Enemies;
using Player;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.Explosion
{
    internal sealed class ExplosionDamageSync : SyncedEvent<ExplosionDamageData>
    {
        public override string GUID => "EXPDMG";

        protected override void Receive(ExplosionDamageData packet)
        {
            if (!packet.target.TryGet(out EnemyAgent target)) return;
            packet.source.TryGet(out PlayerAgent? source);

            ExplosionManager.Internal_ReceiveExplosionDamage(
                target,
                source,
                packet.limbID,
                packet.localPosition.Get(10f),
                packet.damage.Get(target.Damage.DamageMax),
                packet.staggerMult.Get(ExplosionManager.MaxStagger)
                );
        }

        
    }
}

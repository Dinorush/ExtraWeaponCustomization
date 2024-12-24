using Enemies;
using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam
{
    internal sealed class FoamEnemySync : SyncedEventMasterOnly<FoamEnemyData>
    {
        public override string GUID => "FOAME";

        protected override void Receive(FoamEnemyData packet)
        {
            FoamManager.Internal_ReceiveFoamEnemy(packet);
        }
    }

    internal sealed class FoamStaticSync : SyncedEventMasterOnly<FoamStaticData>
    {
        public override string GUID => "FOAMS";

        protected override void Receive(FoamStaticData packet)
        {
            FoamManager.Internal_ReceiveFoamStatic(packet);
        }
    }

    internal sealed class FoamDoorSync : SyncedEventMasterOnly<FoamDoorData>
    {
        public override string GUID => "FOAMD";

        protected override void Receive(FoamDoorData packet)
        {
            FoamManager.Internal_ReceiveFoamDoor(packet);
        }
    }
}

using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam
{
    internal sealed class FoamEnemySync : SyncedEventMasterOnly<FoamEnemyData>
    {
        public override string GUID => "FMENMY";

        protected override void Receive(FoamEnemyData packet)
        {
            FoamActionManager.Internal_ReceiveFoamEnemy(packet);
        }
    }

    internal sealed class FoamStaticSync : SyncedEventMasterOnly<FoamStaticData>
    {
        public override string GUID => "FMSTAT";

        protected override void Receive(FoamStaticData packet)
        {
            FoamActionManager.Internal_ReceiveFoamStatic(packet);
        }
    }

    internal sealed class FoamDoorSync : SyncedEventMasterOnly<FoamDoorData>
    {
        public override string GUID => "FMDOOR";

        protected override void Receive(FoamDoorData packet)
        {
            FoamActionManager.Internal_ReceiveFoamDoor(packet);
        }
    }

    internal sealed class FoamDirectSync : SyncedEventMasterOnly<FoamDirectData>
    {
        public override string GUID => "FMDRCT";

        protected override void Receive(FoamDirectData packet)
        {
            FoamActionManager.Internal_ReceiveFoamDirect(packet);
        }
    }

    internal sealed class FoamSync : SyncedEvent<FoamSyncData>
    {
        public override string GUID => "FMSYNC";

        protected override void Receive(FoamSyncData packet)
        {
            FoamActionManager.Internal_ReceiveFoamSync(packet);
        }
    }
}

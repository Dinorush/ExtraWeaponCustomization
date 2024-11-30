using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    internal sealed class TriggerSync : SyncedEvent<TriggerInstanceData>
    {
        public override string GUID => "TRGIN";

        protected override void Receive(TriggerInstanceData packet) => TriggerManager.Internal_ReceiveInstance(packet);
    }

    internal sealed class TriggerDirSync : SyncedEvent<TriggerDirInstanceData>
    {
        public override string GUID => "TRGDIR";

        protected override void Receive(TriggerDirInstanceData packet) => TriggerManager.Internal_ReceiveInstance(packet);
    }

    internal sealed class TriggerAgentSync : SyncedEvent<TriggerAgentInstanceData>
    {
        public override string GUID => "TRGAGT";

        protected override void Receive(TriggerAgentInstanceData packet) => TriggerManager.Internal_ReceiveInstance(packet);
    }

    internal sealed class TriggerResetSync : SyncedEvent<TriggerResetData>
    {
        public override string GUID => "TRGRS";

        protected override void Receive(TriggerResetData packet) => TriggerManager.Internal_ReceiveReset(packet);
    }
}

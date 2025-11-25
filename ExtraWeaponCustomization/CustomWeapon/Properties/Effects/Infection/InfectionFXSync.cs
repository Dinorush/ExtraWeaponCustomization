using Agents;
using EWC.Networking;

namespace EWC.CustomWeapon.Properties.Effects.Infection
{
    internal sealed class InfectionFXSync : SyncedEvent<InfectionFXData>
    {
        public override string GUID => "INFFX";

        protected override void Receive(InfectionFXData data)
        {
            if (!data.player.TryGet(out var player)) return;

            InfectionManager.Internal_ReceiveInfectionFX(player, data.localPos.Get(10f), data.dir.Value);
        }
    }

    internal sealed class DirectInfectionFXSync : SyncedEvent<pPlayerAgent>
    {
        public override string GUID => "DINFFX";

        protected override void Receive(pPlayerAgent pPlayer)
        {
            if (!pPlayer.TryGet(out var player)) return;

            InfectionManager.Internal_ReceiveDirectInfectionFX(player);
        }
    }
}
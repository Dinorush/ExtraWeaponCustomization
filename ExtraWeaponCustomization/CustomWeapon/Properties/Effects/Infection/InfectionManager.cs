using Agents;
using AK;
using EWC.Attributes;
using Player;
using SNetwork;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Infection
{
    public static class InfectionManager
    {
        private readonly static InfectionFXSync _fxSync = new();
        private readonly static DirectInfectionFXSync _directFXSync = new();

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _fxSync.Setup();
            _directFXSync.Setup();
        }

        public static void DoInfectFX(PlayerAgent player, float infect, Vector3 pos, Vector3 dir)
        {
            if (infect == 0 || player.Owner.IsBot) return;

            InfectionFXData data = new() { isDirect = false };
            data.player.Set(player);
            data.localPos.Set(pos - player.Position, 10f);
            data.dir.Value = dir;
            _fxSync.Send(data, player.Owner);
        }

        public static void DoDirectInfectFX(PlayerAgent player, float infect)
        {
            if (infect == 0 || player.Owner.IsBot) return;

            pPlayerAgent pPlayer = new();
            pPlayer.Set(player);
            _directFXSync.Send(pPlayer, player.Owner);
        }

        internal static void Internal_ReceiveInfectionFX(PlayerAgent player, Vector3 localPos, Vector3 dir)
        {
            ScreenLiquidManager.Apply(ScreenLiquidSettingName.spitterJizz, localPos + player.Position, dir);
            player.Sound.Post(EVENTS.VISOR_SPLATTER_INFECTION);
        }

        internal static void Internal_ReceiveDirectInfectionFX(PlayerAgent player)
        {
            ScreenLiquidManager.DirectApply(ScreenLiquidSettingName.spitterJizz, new(0.5f, 0.5f), Vector2.zero);
            player.Sound.Post(EVENTS.VISOR_SPLATTER_INFECTION);
        }
    }

    public struct InfectionFXData
    {
        public pPlayerAgent player;
        public LowResVector3 localPos;
        public LowResVector3_Normalized dir;
        public bool isDirect;
    }
}

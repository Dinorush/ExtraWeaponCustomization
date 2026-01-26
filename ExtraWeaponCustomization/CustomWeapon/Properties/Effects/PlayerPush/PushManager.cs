using EWC.Attributes;
using Player;
using SNetwork;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.PlayerPush
{
    public static class PushManager
    {
        private readonly static PushSync _sync = new();

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
        }

        public static void DoPush(PlayerAgent player, Vector3 force, Push settings)
        {
            if (force == Vector3.zero) return;

            PushData data = new() {
                force = force,
                propertyID = settings.SyncPropertyID
            };

            _sync.Send(data, player.Owner, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceivePush(Vector3 force, Push settings)
        {
            PushHandler.AddInstance(force, settings);
        }
    }

    public struct PushData
    {
        public Vector3 force;
        public ushort propertyID;
    }
}

using EWC.Attributes;
using EWC.CustomWeapon.Structs;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.Properties.Effects.Speed
{
    public static class SpeedManager
    {
        private readonly static SpeedSync _sync = new();

        private const float MaxMod = 256f;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
        }

        public static void ApplySpeedMod(SNet_Player player, SpeedMod speedMod, float triggerAmt)
        {
            if (triggerAmt == 0) return;

            SpeedData data = new() { propertyID = speedMod.SyncID };
            data.cwc.Set(speedMod.CWC);
            data.mod.Set(triggerAmt, MaxMod);

            _sync.Send(data, player, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveSpeed(SpeedData data)
        {
            if (TryGetSpeedMod(data, out var speedMod))
                speedMod.TriggerApplySync(data.mod.Get(MaxMod));
        }

        private static bool TryGetSpeedMod(SpeedData data, [MaybeNullWhen(false)] out SpeedMod speedMod)
        {
            if (!data.cwc.TryGet(out var cwc))
            {
                speedMod = null;
                return false;
            }

            speedMod = cwc.GetTriggerSync(data.propertyID) as SpeedMod;
            return speedMod != null;
        }
    }

    public struct SpeedData
    {
        public pCWC cwc;
        public ushort propertyID;
        public UFloat16 mod;
    }
}

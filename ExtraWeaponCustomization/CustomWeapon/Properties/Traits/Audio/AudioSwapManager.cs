using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils.Log;
using Player;
using SNetwork;
using static SNetwork.SNetStructs;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits.Audio
{
    public static class AudioSwapManager
    {
        private readonly static AudioSwapSync _sync = new();

        internal static void Init()
        {
            _sync.Setup();
        }

        public static void SendInstance(SNet_Player source, InventorySlot slot)
        {
            TriggerData data = default;
            data.source.SetPlayer(source);
            data.slot = slot;
            _sync.Send(data);
        }

        internal static void Internal_ReceiveInstance(SNet_Player source, InventorySlot slot)
        {
            if (!PlayerBackpackManager.TryGetBackpack(source, out var backpack)) return;
            if (!backpack.TryGetBackpackItem(slot, out var item)) return;

            CustomWeaponComponent? cwc = item.Instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                EWCLogger.Error("Received custom info for weapon that has no custom info. Client may have modified weapons.");
                cwc = item.Instance.gameObject.AddComponent<CustomWeaponComponent>();
                cwc.SetToSync();
                return;
            }

            cwc.Invoke(StaticContext<WeaponAudioSwapContextSync>.Instance);
        }
    }

    public struct TriggerData
    {
        public pPlayer source;
        public InventorySlot slot;
    }
}

using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Player;
using SNetwork;
using static SNetwork.SNetStructs;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.FireRate
{
    public static class FireRateModManager
    {
        private readonly static FireRateModSync _sync = new();
        public const float MaxMod = 256f;

        internal static void Init()
        {
            _sync.Setup();
        }

        public static void SendInstance(SNet_Player source, InventorySlot slot, float mod)
        {
            TriggerInstanceData data = default;
            data.source.SetPlayer(source);
            data.slot = slot;
            data.mod.Set(mod, MaxMod);
            _sync.Send(data);
        }

        internal static void Internal_ReceiveInstance(SNet_Player source, InventorySlot slot, float mod)
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

            cwc.Invoke(new WeaponFireRateModContextSync(mod, cwc.Weapon));
        }
    }

    public struct TriggerInstanceData
    {
        public pPlayer source;
        public InventorySlot slot;
        public UFloat16 mod;
    }
}

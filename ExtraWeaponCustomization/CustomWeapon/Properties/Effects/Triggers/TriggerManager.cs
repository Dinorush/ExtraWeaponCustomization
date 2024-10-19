using EWC.Utils.Log;
using Player;
using SNetwork;
using static SNetwork.SNetStructs;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public static class TriggerManager
    {
        private readonly static TriggerSync _triggerSync = new();
        private readonly static TriggerResetSync _resetSync = new();
        public const float MaxMod = 256f;

        internal static void Init()
        {
            _triggerSync.Setup();
            _resetSync.Setup();
        }

        public static void SendInstance(ITriggerCallbackSync caller, float mod = 1f)
        {
            TriggerInstanceData data = default;
            data.source.SetPlayer(caller.CWC.Weapon.Owner.Owner);
            data.slot = PlayerAmmoStorage.GetSlotFromAmmoType(caller.CWC.Weapon.AmmoType);
            data.mod.Set(mod, MaxMod);
            data.id = caller.SyncID;
            EWCLogger.Log("Sent instance for " + caller.GetType() + " | " + caller.SyncID);
            _triggerSync.Send(data);
        }

        internal static void Internal_ReceiveInstance(SNet_Player source, InventorySlot slot, float mod, ushort id)
        {
            if (!PlayerBackpackManager.TryGetBackpack(source, out var backpack)) return;
            if (!backpack.TryGetBackpackItem(slot, out var item)) return;

            CustomWeaponComponent? cwc = item.Instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                EWCLogger.Error("Received a networked trigger for a weapon that has no custom info. Client has incorrect EWC properties.");
                cwc = item.Instance.gameObject.AddComponent<CustomWeaponComponent>();
                cwc.SetToSync();
            }

            cwc.GetTriggerSync(id).TriggerApplySync(mod);
        }

        public static void SendReset(ITriggerCallbackSync caller)
        {
            TriggerResetData data = default;
            data.source.SetPlayer(caller.CWC.Weapon.Owner.Owner);
            data.slot = PlayerAmmoStorage.GetSlotFromAmmoType(caller.CWC.Weapon.AmmoType);
            data.id = caller.SyncID;
            EWCLogger.Log("Sent reset for " + caller.GetType() + " | " + caller.SyncID);

            _resetSync.Send(data);
        }

        internal static void Internal_ReceiveReset(SNet_Player source, InventorySlot slot, ushort id)
        {
            if (!PlayerBackpackManager.TryGetBackpack(source, out var backpack)) return;
            if (!backpack.TryGetBackpackItem(slot, out var item)) return;

            CustomWeaponComponent? cwc = item.Instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                EWCLogger.Error("Received a networked reset for a weapon that has no custom info. Client has incorrect EWC properties.");
                cwc = item.Instance.gameObject.AddComponent<CustomWeaponComponent>();
                cwc.SetToSync();
            }

            cwc.GetTriggerSync(id).TriggerResetSync();
        }
    }

    public struct TriggerInstanceData
    {
        public pPlayer source;
        public InventorySlot slot;
        public UFloat16 mod;
        public ushort id;
    }

    public struct TriggerResetData
    {
        public pPlayer source;
        public InventorySlot slot;
        public ushort id;
    }
}

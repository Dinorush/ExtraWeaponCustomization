using Agents;
using EWC.Utils.Log;
using Player;
using SNetwork;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using static SNetwork.SNetStructs;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public static class TriggerManager
    {
        private readonly static TriggerSync _triggerSync = new();
        private readonly static TriggerDirSync _triggerDirSync = new();
        private readonly static TriggerAgentSync _triggerAgentSync = new();
        private readonly static TriggerResetSync _resetSync = new();
        public const float MaxMod = 256f;

        internal static void Init()
        {
            _triggerSync.Setup();
            _triggerDirSync.Setup();
            _triggerAgentSync.Setup();
            _resetSync.Setup();
        }

        public static void SendInstance(ITriggerCallbackBasicSync caller, float mod = 1f)
        {
            if (caller.CWC.Weapon.Owner == null) return;

            _triggerSync.Send(PackInstance(caller, mod));
        }

        internal static void Internal_ReceiveInstance(TriggerInstanceData data)
        {
            if (TryGetTriggerSync(data, out var callback))
                ((ITriggerCallbackBasicSync)callback).TriggerApplySync(data.mod.Get(MaxMod));
        }

        private static TriggerInstanceData PackInstance(ITriggerCallbackSync caller, float mod)
        {
            TriggerInstanceData data = default;
            data.source.SetPlayer(caller.CWC.Weapon.Owner.Owner);
            data.slot = PlayerAmmoStorage.GetSlotFromAmmoType(caller.CWC.Weapon.AmmoType);
            data.mod.Set(mod, MaxMod);
            data.id = caller.SyncID;
            return data;
        }

        public static void SendInstance(ITriggerCallbackDirSync caller, Vector3 position, Vector3 dir, float mod = 1f)
        {
            if (caller.CWC.Weapon.Owner == null) return;

            TriggerDirInstanceData data = default;
            data.position = position;
            data.dir.Value = dir;
            data.instance = PackInstance(caller, mod);
            _triggerDirSync.Send(data);
        }

        internal static void Internal_ReceiveInstance(TriggerDirInstanceData data)
        {
            if (TryGetTriggerSync(data.instance, out var callback))
                ((ITriggerCallbackDirSync)callback).TriggerApplySync(data.position, data.dir.Value, data.instance.mod.Get(MaxMod));
        }

        public static void SendInstance(ITriggerCallbackAgentSync caller, Agent target, float mod = 1f)
        {
            if (caller.CWC.Weapon.Owner == null) return;

            TriggerAgentInstanceData data = default;
            data.target.Set(target);
            data.instance = PackInstance(caller, mod);
            _triggerAgentSync.Send(data);
        }

        internal static void Internal_ReceiveInstance(TriggerAgentInstanceData data)
        {
            if (TryGetTriggerSync(data.instance, out var callback) && data.target.TryGet(out var target))
                ((ITriggerCallbackAgentSync)callback).TriggerApplySync(target, data.instance.mod.Get(MaxMod));
        }

        public static void SendReset(ITriggerCallbackSync caller)
        {
            if (caller.CWC.Weapon.Owner == null) return;

            TriggerResetData data = default;
            data.source.SetPlayer(caller.CWC.Weapon.Owner.Owner);
            data.slot = PlayerAmmoStorage.GetSlotFromAmmoType(caller.CWC.Weapon.AmmoType);
            data.id = caller.SyncID;
            _resetSync.Send(data);
        }

        internal static void Internal_ReceiveReset(TriggerResetData data)
        {
            if (TryGetTriggerSync(data, out var callback))
                callback.TriggerResetSync();
        }

        private static bool TryGetTriggerSync(TriggerInstanceData data, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            callback = null;
            if (!data.source.TryGetPlayer(out var source)) return false;
            return TryGetTriggerSync(source, data.slot, data.id, out callback);
        }

        private static bool TryGetTriggerSync(TriggerResetData data, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            callback = null;
            if (!data.source.TryGetPlayer(out var source)) return false;
            return TryGetTriggerSync(source, data.slot, data.id, out callback);
        }

        private static bool TryGetTriggerSync(SNet_Player source, InventorySlot slot, ushort id, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            callback = null;
            if (!PlayerBackpackManager.TryGetBackpack(source, out var backpack)) return false;
            if (!backpack.TryGetBackpackItem(slot, out var item)) return false;

            CustomWeaponComponent? cwc = item.Instance.GetComponent<CustomWeaponComponent>();
            if (cwc == null)
            {
                EWCLogger.Error("Received a networked trigger for a weapon that has no custom info. Client has incorrect EWC properties.");
                cwc = item.Instance.gameObject.AddComponent<CustomWeaponComponent>();
            }

            callback = cwc.GetTriggerSync(id);
            return true;
        }
    }

    public struct TriggerInstanceData
    {
        public pPlayer source;
        public InventorySlot slot;
        public UFloat16 mod;
        public ushort id;
    }

    public struct TriggerDirInstanceData
    {
        public Vector3 position;
        public LowResVector3_Normalized dir;
        public TriggerInstanceData instance;
    }

    public struct TriggerAgentInstanceData
    {
        public pAgent target;
        public TriggerInstanceData instance;
    }

    public struct TriggerResetData
    {
        public pPlayer source;
        public InventorySlot slot;
        public ushort id;
    }
}

using Agents;
using EWC.Attributes;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
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
        private readonly static Dictionary<(InventorySlot slot, bool isSentry), Queue<Action>> _queuedReceives = new();
        public const float MaxMod = 256f;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _triggerSync.Setup();
            _triggerDirSync.Setup();
            _triggerAgentSync.Setup();
            _resetSync.Setup();
        }

        [InvokeOnCleanup]
        private static void Cleanup()
        {
            _queuedReceives.Clear();
        }

        internal static void RunQueuedReceives(InventorySlot slot, bool isSentry)
        {
            if (!_queuedReceives.TryGetValue((slot, isSentry), out var queue)) return;

            while (queue.TryDequeue(out var onProcess))
                onProcess();
        }

        public static void SendInstance(ITriggerCallbackBasicSync caller, float mod = 1f)
        {
            if (caller.CWC.Owner.Player == null) return;

            _triggerSync.Send(PackInstance(caller, mod));
        }

        internal static void Internal_ReceiveInstance(TriggerInstanceData data, bool storeOnFail = true)
        {
            if (TryGetTriggerSync(data, out var callback))
                ((ITriggerCallbackBasicSync)callback).TriggerApplySync(data.mod.Get(MaxMod));
            else if (storeOnFail)
                QueueReceive(data, () => Internal_ReceiveInstance(data, storeOnFail: false));
            else
                LogFailMessage(data);
        }

        private static TriggerInstanceData PackInstance(ITriggerCallbackSync caller, float mod)
        {
            TriggerInstanceData data = default;
            data.source.SetPlayer(caller.CWC.Owner.Player.Owner);
            data.isSentry = caller.CWC.Owner.IsType(Enums.OwnerType.Sentry);
            data.slot = PlayerAmmoStorage.GetSlotFromAmmoType(caller.CWC.Weapon.AmmoType);
            data.mod.Set(mod, MaxMod);
            data.id = caller.SyncID;
            return data;
        }

        public static void SendInstance(ITriggerCallbackDirSync caller, Vector3 position, Vector3 dir, float mod = 1f)
        {
            if (caller.CWC.Owner.Player == null) return;

            TriggerDirInstanceData data = default;
            data.position = position;
            data.dir.Value = dir;
            data.instance = PackInstance(caller, mod);
            _triggerDirSync.Send(data);
        }

        internal static void Internal_ReceiveInstance(TriggerDirInstanceData data, bool storeOnFail = true)
        {
            if (TryGetTriggerSync(data.instance, out var callback))
                ((ITriggerCallbackDirSync)callback).TriggerApplySync(data.position, data.dir.Value, data.instance.mod.Get(MaxMod));
            else if (storeOnFail)
                QueueReceive(data.instance, () => Internal_ReceiveInstance(data, storeOnFail: false));
            else
                LogFailMessage(data.instance);
        }

        public static void SendInstance(ITriggerCallbackAgentSync caller, Agent target, float mod = 1f)
        {
            if (caller.CWC.Owner.Player == null) return;

            TriggerAgentInstanceData data = default;
            data.target.Set(target);
            data.instance = PackInstance(caller, mod);
            _triggerAgentSync.Send(data);
        }

        internal static void Internal_ReceiveInstance(TriggerAgentInstanceData data, bool storeOnFail = true)
        {
            if (TryGetTriggerSync(data.instance, out var callback) && data.target.TryGet(out var target))
                ((ITriggerCallbackAgentSync)callback).TriggerApplySync(target, data.instance.mod.Get(MaxMod));
            else if (storeOnFail)
                QueueReceive(data.instance, () => Internal_ReceiveInstance(data, storeOnFail: false));
            else
                LogFailMessage(data.instance);
        }

        public static void SendReset(ITriggerCallbackSync caller)
        {
            if (caller.CWC.Owner.Player == null) return;

            TriggerResetData data = default;
            data.source.SetPlayer(caller.CWC.Owner.Player.Owner);
            data.slot = PlayerAmmoStorage.GetSlotFromAmmoType(caller.CWC.Weapon.AmmoType);
            data.id = caller.SyncID;
            _resetSync.Send(data);
        }

        internal static void Internal_ReceiveReset(TriggerResetData data, bool storeOnFail = true)
        {
            if (TryGetTriggerSync(data, out var callback))
                callback.TriggerResetSync();
            else if (storeOnFail)
                QueueReceive(data, () => Internal_ReceiveReset(data, storeOnFail: false));
            else
                LogFailMessage(data);
        }

        private static bool TryGetTriggerSync(TriggerInstanceData data, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            callback = null;
            if (!data.source.TryGetPlayer(out var source)) return false;
            return TryGetTriggerSync(source, data.isSentry, data.slot, data.id, out callback);
        }

        private static bool TryGetTriggerSync(TriggerResetData data, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            callback = null;
            if (!data.source.TryGetPlayer(out var source)) return false;
            return TryGetTriggerSync(source, data.isSentry, data.slot, data.id, out callback);
        }

        private static bool TryGetTriggerSync(SNet_Player source, bool isSentry, InventorySlot slot, ushort id, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            callback = null;
            CustomWeaponComponent? cwc;
            if (isSentry)
            {
                if (!source.HasPlayerAgent) return false;
                if (!CustomWeaponManager.TryGetSentry(source.PlayerAgent.Cast<PlayerAgent>(), out var info)) return false;

                cwc = info.cgc;
            }
            else
            {
                if (!PlayerBackpackManager.TryGetBackpack(source, out var backpack)) return false;
                if (!backpack.TryGetBackpackItem(slot, out var item)) return false;

                cwc = item.Instance.GetComponent<CustomWeaponComponent>();
            }

            if (cwc == null) return false;

            callback = cwc.GetTriggerSync(id);
            return true;
        }

        private static void QueueReceive(TriggerInstanceData instance, Action onProcess)
        {
            (InventorySlot, bool) target = (instance.slot, instance.isSentry);
            if (!_queuedReceives.TryGetValue(target, out var queue))
                _queuedReceives.Add(target, queue = new());
            queue.Enqueue(onProcess);
        }

        private static void QueueReceive(TriggerResetData instance, Action onProcess)
        {
            (InventorySlot, bool) target = (instance.slot, instance.isSentry);
            if (!_queuedReceives.TryGetValue(target, out var queue))
                _queuedReceives.Add(target, queue = new());
            queue.Enqueue(onProcess);
        }

        private static void LogFailMessage(TriggerInstanceData instance) => EWCLogger.Error($"Unable to get custom weapon for {(instance.source.TryGetPlayer(out var player) ? player.NickName : "No Player")} on slot {instance.slot} ({(instance.isSentry ? "Sentry" : "Not Sentry")}");
        private static void LogFailMessage(TriggerResetData instance) => EWCLogger.Error($"Unable to get custom weapon for {(instance.source.TryGetPlayer(out var player) ? player.NickName : "No Player")} on slot {instance.slot} ({(instance.isSentry ? "Sentry" : "Not Sentry")}");
    }

    public struct TriggerInstanceData
    {
        public pPlayer source;
        public bool isSentry;
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
        public bool isSentry;
        public InventorySlot slot;
        public ushort id;
    }
}

using Agents;
using EWC.Attributes;
using EWC.CustomWeapon.Structs;
using EWC.Networking.Structs;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public static class TriggerManager
    {
        private readonly static TriggerSync _triggerSync = new();
        private readonly static TriggerDirSync _triggerDirSync = new();
        private readonly static TriggerImpactSync _triggerImpactSync = new();
        private readonly static TriggerAgentSync _triggerAgentSync = new();
        private readonly static TriggerResetSync _resetSync = new();
        private readonly static Dictionary<(ushort repKey, InventorySlot slot), Queue<Action>> _queuedReceives = new();
        public const float MaxMod = 256f;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _triggerSync.Setup();
            _triggerDirSync.Setup();
            _triggerImpactSync.Setup();
            _triggerAgentSync.Setup();
            _resetSync.Setup();
        }

        [InvokeOnCleanup]
        private static void Cleanup()
        {
            _queuedReceives.Clear();
        }

        private static bool CheckInvalidCaller(ITriggerCallback caller)
        {
            if (caller.CWC.Weapon.InventorySlot == InventorySlot.None)
            {
                var player = caller.CWC.Owner.Player;
                EWCLogger.Warning($"Attempting to send a trigger from {caller.CWC.Weapon.Component?.name}, but slot is None! (owner: {(player != null ? player.Owner.NickName : "none")})");
                return true;
            }
            return false;
        }

        internal static void RunQueuedReceives(CustomWeaponComponent cwc)
        {
            if (!_queuedReceives.TryGetValue((cwc.Owner.ReplicatorKey, cwc.Weapon.InventorySlot), out var queue)) return;

            while (queue.TryDequeue(out var onProcess))
                onProcess();
        }

        public static void SendInstance(ITriggerCallbackBasicSync caller, float mod = 1f)
        {
            if (CheckInvalidCaller(caller)) return;

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
            data.cwc.Set(caller.CWC);
            data.mod.Set(mod, MaxMod);
            data.id = caller.SyncID;
            return data;
        }

        public static void SendInstance(ITriggerCallbackDirSync caller, Vector3 position, Vector3 dir, float mod = 1f)
        {
            if (CheckInvalidCaller(caller)) return;

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

        public static void SendInstance(ITriggerCallbackImpactSync caller, Vector3 position, Vector3 dir, Vector3 normal, float mod = 1f)
        {
            if (CheckInvalidCaller(caller)) return;

            TriggerImpactInstanceData data = default;
            data.position = position;
            data.dir.Value = dir;
            data.normal.Value = normal;
            data.instance = PackInstance(caller, mod);
            _triggerImpactSync.Send(data);
        }

        internal static void Internal_ReceiveInstance(TriggerImpactInstanceData data, bool storeOnFail = true)
        {
            if (TryGetTriggerSync(data.instance, out var callback))
                ((ITriggerCallbackImpactSync)callback).TriggerApplySync(data.position, data.dir.Value, data.normal.Value, data.instance.mod.Get(MaxMod));
            else if (storeOnFail)
                QueueReceive(data.instance, () => Internal_ReceiveInstance(data, storeOnFail: false));
            else
                LogFailMessage(data.instance);
        }

        public static void SendInstance(ITriggerCallbackAgentSync caller, Agent target, float mod = 1f)
        {
            if (CheckInvalidCaller(caller)) return;

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
            if (CheckInvalidCaller(caller)) return;

            TriggerResetData data = default;
            data.cwc.Set(caller.CWC);
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
            return TryGetTriggerSync(data.cwc, data.id, out callback);
        }

        private static bool TryGetTriggerSync(TriggerResetData data, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            return TryGetTriggerSync(data.cwc, data.id, out callback);
        }

        private static bool TryGetTriggerSync(pCWC packet, ushort id, [MaybeNullWhen(false)] out ITriggerCallbackSync callback)
        {
            if (!packet.TryGet(out var cwc))
            {
                callback = null;
                return false;
            }

            callback = cwc.GetTriggerSync(id);
            return true;
        }

        private static void QueueReceive(TriggerInstanceData instance, Action onProcess)
        {
            (ushort, InventorySlot) target = ((ushort) (instance.cwc.RepKeyPlusOne - 1), instance.cwc.slot);
            if (!_queuedReceives.TryGetValue(target, out var queue))
                _queuedReceives.Add(target, queue = new());
            queue.Enqueue(onProcess);
        }

        private static void QueueReceive(TriggerResetData instance, Action onProcess)
        {
            (ushort, InventorySlot) target = ((ushort)(instance.cwc.RepKeyPlusOne - 1), instance.cwc.slot);
            if (!_queuedReceives.TryGetValue(target, out var queue))
                _queuedReceives.Add(target, queue = new());
            queue.Enqueue(onProcess);
        }

        private static void LogFailMessage(TriggerInstanceData instance) => LogFailMessage(instance.cwc);
        private static void LogFailMessage(TriggerResetData instance) => LogFailMessage(instance.cwc);
        private static void LogFailMessage(pCWC cwc)
        {
            StringBuilder sb = new($"Unable to get custom weapon for {(cwc.TryGetPlayer(out var player) ? player.Owner.NickName : "No Player")} on slot {cwc.slot} (Is sentry: {cwc.isSentry}).");

            if (player != null)
            {
                SNet_Player? owner = player.Owner;
                if (PlayerBackpackManager.TryGetBackpack(owner, out var bp))
                {
                    sb.Append(" Inventory: ");
                    if (bp.TryGetBackpackItem(InventorySlot.GearStandard, out var bpItem))
                        sb.Append($"Primary: {bpItem.Instance.PublicName}, ");
                    else
                        sb.Append($"Primary: None, ");
                    if (bp.TryGetBackpackItem(InventorySlot.GearSpecial, out bpItem))
                        sb.Append($"Secondary: {bpItem.Instance.PublicName}, ");
                    else
                        sb.Append($"Secondary: None, ");
                    if (bp.TryGetBackpackItem(InventorySlot.GearMelee, out bpItem))
                        sb.Append($"Melee: {bpItem.Instance.PublicName}, ");
                    else
                        sb.Append($"Melee: None, ");
                    if (bp.TryGetBackpackItem(InventorySlot.GearClass, out bpItem))
                        sb.Append($"Tool: {bpItem.Instance.PublicName}");
                    else
                        sb.Append($"Tool: None");
                }
                else
                    sb.Append($"Inventory: None");
            }

            EWCLogger.Error(sb.ToString());
        }
    }

    public struct TriggerInstanceData
    {
        public pCWC cwc;
        public UFloat16b mod;
        public ushort id;
    }

    public struct TriggerDirInstanceData
    {
        public Vector3 position;
        public Vector3_24b_Normalized dir;
        public TriggerInstanceData instance;
    }

    public struct TriggerImpactInstanceData
    {
        public Vector3 position;
        public Vector3_24b_Normalized dir;
        public Vector3_24b_Normalized normal;
        public TriggerInstanceData instance;
    }

    public struct TriggerAgentInstanceData
    {
        public pAgent target;
        public TriggerInstanceData instance;
    }

    public struct TriggerResetData
    {
        public pCWC cwc;
        public ushort id;
    }
}

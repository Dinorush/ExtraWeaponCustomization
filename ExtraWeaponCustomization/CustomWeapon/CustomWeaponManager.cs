using EWC.Attributes;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.Utils.Extensions;
using Gear;
using Player;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon
{
    public sealed class CustomWeaponManager
    {
        private struct ListenerInfo
        {
            public bool hasSpawned = false;
            public PlayerAgent? owner = null;
            public readonly Func<GameObject, CustomWeaponComponent>? addCWC = null;

            public readonly bool IsReady() => hasSpawned && owner != null && IsValid();
            public readonly bool IsValid() => addCWC != null;

            public ListenerInfo(ItemEquippable item)
            {
                if (item.TryCast<MeleeWeaponFirstPerson>() != null)
                {
                    addCWC = (go) => go.AddComponent<CustomMeleeComponent>();
                    hasSpawned = true;
                }
                else if (item.TryCast<BulletWeapon>() != null)
                    addCWC = (go) => go.AddComponent<CustomGunComponent>();
                else if (item.TryCast<SentryGunFirstPerson>() != null)
                    addCWC = (go) => go.AddComponent<CustomWeaponComponent>();
            }
        }

        public static readonly CustomWeaponManager Current = new();
        private static bool s_inLevel = false;
        private static bool s_assetsLoaded = false;

        private readonly Dictionary<ObjectWrapper<ItemEquippable>, ListenerInfo> _weaponListeners = new();
        private readonly Dictionary<IntPtr, (SentryGunInstance sentry,  CustomGunComponent? cgc)> _trackedSentries = new();

        private static ObjectWrapper<ItemEquippable> TempWrapper => ObjectWrapper<ItemEquippable>.SharedInstance;

        [InvokeOnAssetLoad]
        private static void OnAssetsLoaded()
        {
            s_assetsLoaded = true;
        }

        [InvokeOnEnter]
        private static void OnEnterLevel()
        {
            s_inLevel = true;
            Current.AddAllEquippedItems();
        }

        [InvokeOnCleanup]
        private static void OnCleanup()
        {
            s_inLevel = false;
            Current.ResetCWCs(activate: false);
        }

        [InvokeOnCheckpoint]
        private static void OnCheckpoint()
        {
            Current.ResetCWCs(activate: true, reacquireOwners: true);
        }

        public static void InvokeOnGear<T>(SNetwork.SNet_Player owner, T context) where T : WeaponContext.IWeaponContext => InvokeOnGear(owner, (null, context));
        public static void InvokeOnGear<T>(SNetwork.SNet_Player owner, Func<T>? func) where T : WeaponContext.IWeaponContext => InvokeOnGear(owner, (func, default(T)));
        private static void InvokeOnGear<T>(SNetwork.SNet_Player owner, (Func<T>? func, T? obj) pair) where T : WeaponContext.IWeaponContext
        {
            if (!PlayerBackpackManager.TryGetBackpack(owner, out var backpack)) return;

            if (backpack.TryGetBackpackItem(InventorySlot.GearStandard, out BackpackItem primary))
            {
                CustomWeaponComponent? cwc = primary.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(pair.func != null ? pair.func() : pair.obj!);
            }

            if (backpack.TryGetBackpackItem(InventorySlot.GearSpecial, out BackpackItem special))
            {
                CustomWeaponComponent? cwc = special.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(pair.func != null ? pair.func() : pair.obj!);
            }

            if (backpack.TryGetBackpackItem(InventorySlot.GearMelee, out BackpackItem melee))
            {
                CustomWeaponComponent? cwc = melee.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(pair.func != null ? pair.func() : pair.obj!);
            }

            if (backpack.TryGetBackpackItem(InventorySlot.GearClass, out BackpackItem tool))
            {
                CustomWeaponComponent? cwc = tool.Instance?.GetComponent<CustomWeaponComponent>();
                cwc?.Invoke(pair.func != null ? pair.func() : pair.obj!);
            }

            if (owner.HasPlayerAgent && Current._trackedSentries.TryGetValue(owner.PlayerAgent.Pointer, out var sentryInfo))
            {
                sentryInfo.cgc?.Invoke(pair.func != null ? pair.func() : pair.obj!);
            }
        }

        public static void ActivateSentry(SentryGunInstance sentry)
        {
            if (!s_inLevel) return;

            if (CustomDataManager.TryGetCustomGunData(sentry.ArchetypeID, out var data))
            {
                if (!sentry.TryGetComp<CustomGunComponent>(out var cgc))
                    cgc = sentry.gameObject.AddComponent<CustomGunComponent>();
                cgc.Register(data);
                Current._trackedSentries.Add(sentry.Owner.Pointer, (sentry, cgc));
            }
            else
                Current._trackedSentries.Add(sentry.Owner.Pointer, (sentry, null));
        }

        public static void RemoveSentry(SentryGunInstance sentry)
        {
            if (!s_inLevel) return;

            Current._trackedSentries.Remove(sentry.Owner.Pointer);
        }

        public static bool TryGetSentry(PlayerAgent player, [MaybeNullWhen(false)] out (SentryGunInstance sentry, CustomGunComponent? cgc) sentryInfo) => Current._trackedSentries.TryGetValue(player.Pointer, out sentryInfo);


        public static void AddSpawnedItem(ItemEquippable item)
        {
            if (!s_assetsLoaded || (item.ArchetypeData == null && item.MeleeArchetypeData == null)) return;

            if (!TryGetInfo(item, out var info)) return;

            info.hasSpawned = true;
            Current._weaponListeners[TempWrapper] = info;
            ActivateItem(item);
        }

        public static void RemoveSpawnedItem(ItemEquippable item)
        {
            Current._weaponListeners.Remove(TempWrapper.Set(item));
        }

        public static void AddEquippedItem(PlayerAgent owner, ItemEquippable item)
        {
            if (!TryGetInfo(item, out var info)) return;

            info.owner = owner;
            Current._weaponListeners[TempWrapper] = info;
            ActivateItem(item);
        }

        private static bool TryGetInfo(ItemEquippable item, out ListenerInfo info)
        {
            var listeners = Current._weaponListeners;
            if (!listeners.TryGetValue(TempWrapper.Set(item), out info))
                listeners.Add(new(TempWrapper), info = new(item));

            return info.IsValid();
        }

        private static void ActivateItem(ItemEquippable item)
        {
            if (!s_inLevel || !TryGetInfo(item, out var info)) return;

            if (!info.IsReady()) return;

            item.SetOwner(info.owner);
            if (CustomDataManager.TryGetCustomData(item, out var data))
            {
                if (!item.TryGetComp<CustomWeaponComponent>(out var cwc))
                    cwc = info.addCWC!(item.gameObject);
                cwc.Register(data);
            }
        }

        public static void ReloadCWCs()
        {
            if (s_inLevel)
                Current.ResetCWCs();
        }

        private void ResetCWCs(bool activate = true, bool reacquireOwners = false)
        {
            // JFS - Reset crosshair modifier. Should be cleared by other stuff but doesn't hurt
            Dependencies.ACAPIWrapper.ResetCrosshairSpread();

            List<ObjectWrapper<ItemEquippable>> deletedItems = new();
            var listeners = Current._weaponListeners;
            foreach ((var wrapper, var info) in listeners)
            {
                if (wrapper.Object != null)
                {
                    if (!info.IsValid()) continue;

                    var item = wrapper.Object!;
                    if (item.TryGetComp<CustomWeaponComponent>(out var cwc))
                    {
                        cwc.Clear();
                        if (reacquireOwners)
                            cwc.ResetOwner();
                    }

                    if (activate)
                        ActivateItem(item);
                }
                else
                    deletedItems.Add(wrapper);
            }

            foreach (var item in deletedItems)
                listeners.Remove(item);
        }

        private void AddAllEquippedItems()
        {
            foreach (var backpack in PlayerBackpackManager.Current.m_backpacks.Values)
            {
                if (!backpack.Owner.HasPlayerAgent)
                {
                    EWCLogger.Error($"Tried to activate CWCs for {backpack.Owner.NickName} but they have no player agent!");
                    continue;
                }

                var agent = backpack.Owner.PlayerAgent.Cast<PlayerAgent>();
                foreach (var slot in CustomDataManager.ValidSlots)
                {
                    if (!backpack.TryGetBackpackItem(slot, out var bpItem)) continue;

                    AddEquippedItem(agent, bpItem.Instance.Cast<ItemEquippable>());
                }
            }
        }
    }
}

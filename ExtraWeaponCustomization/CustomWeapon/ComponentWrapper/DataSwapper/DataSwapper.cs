using AmorLib.Utils.Extensions;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects;
using EWC.Dependencies;
using GameData;
using Gear;
using Player;
using System;
using System.Collections.Generic;

namespace EWC.CustomWeapon.ComponentWrapper.DataSwapper
{
    public sealed class LocalDataSwapper : DataSwapper
    {
        private readonly LocalGunComp _localGun;
        public LocalDataSwapper(CustomGunComponent cgc) : base(cgc)
        {
            _localGun = (LocalGunComp)cgc.Gun;
        }

        protected override SwapData GetBaseSwapData(IGunComp gun) => new(((LocalGunComp)gun).GunArchetype, gun.ArchetypeData, gun.AudioData);

        protected override void OnArchetypeChange()
        {
            var oldArch = _lastSwap.GunArchetype!;
            var newArch = _activeSwap.GunArchetype!;
            CopyArchetypeVars(newArch, oldArch);
            _localGun.GunArchetype = newArch;
            ResetERDComponent();
        }

        protected override void OnArchetypeChangeComplete()
        {
            var newArch = _activeSwap.GunArchetype;
            if (newArch != null)
                newArch.m_clip = _localGun.GetCurrentClip();
            if (_cgc.SpreadController.Active && (!_localGun.IsAiming || _localGun.Component.ItemDataBlock.ShowCrosshairWhenAiming))
                GuiManager.CrosshairLayer.ShowSpreadCircle(_localGun.Component.RecoilData.hipFireCrosshairSizeDefault);
        }

        protected override void SetLoopingAudio(bool start)
        {
            if (!_localGun.GunArchetype.m_firing) return;

            if (_localGun.FireMode == eWeaponFireMode.Auto && !_localGun.AudioData.TriggerAutoAudioForEachShot)
            {
                if (start)
                    _localGun.Value.TriggerAutoFireStartAudio();
                else
                    _localGun.Value.TriggerAutoFireEndAudio();
            }

            if (_localGun.FireMode == eWeaponFireMode.Burst && !_localGun.AudioData.TriggerBurstAudioForEachShot)
            {
                if (start)
                    _localGun.Value.TriggerBurstFireAudio();
            }
        }

        private void CopyArchetypeVars(BulletWeaponArchetype newArch, BulletWeaponArchetype oldArch)
        {
            var settings = _activeSwap.Settings;
            if (oldArch.m_inChargeup)
            {
                if (settings.HasFlag(SwapSettings.EndCharge))
                {
                    oldArch.OnStopChargeup();
                    oldArch.m_nextShotTimer = Clock.Time + oldArch.ShotDelay();
                }
                else if (newArch.HasChargeup)
                {
                    newArch.m_inChargeup = oldArch.m_inChargeup;
                    newArch.m_chargeupTimer = oldArch.m_chargeupTimer - oldArch.ChargeupDelay() + newArch.ChargeupDelay();
                }
                else
                    oldArch.OnStopChargeup();
            }

            if (oldArch.m_archetypeData.FireMode == eWeaponFireMode.Burst && !oldArch.BurstIsDone())
            {
                var sendBurst = oldArch.Cast<BWA_Burst>();
                if (settings.HasFlag(SwapSettings.EndBurst))
                {
                    sendBurst.m_burstCurrentCount = 0;
                    sendBurst.PostFireCheck();
                }
                else if (newArch.m_archetypeData.FireMode == eWeaponFireMode.Burst)
                {
                    var recvBurst = newArch.Cast<BWA_Burst>();
                    recvBurst.m_burstCurrentCount = Math.Min(sendBurst.m_burstCurrentCount, recvBurst.m_burstMax);
                    sendBurst.m_burstCurrentCount = 0;
                }
            }

            if (oldArch.m_firing && (settings.HasFlag(SwapSettings.EndFiring) || oldArch.m_archetypeData.FireMode != newArch.m_archetypeData.FireMode))
                oldArch.OnStopFiring();

            newArch.m_nextBurstTimer = oldArch.m_nextBurstTimer;
            newArch.m_nextShotTimer = oldArch.m_nextShotTimer;
            newArch.m_fireHeld = oldArch.m_fireHeld;
            newArch.m_firePressed = oldArch.m_firePressed;
            newArch.m_firing = oldArch.m_firing;
            newArch.m_readyToFire = oldArch.m_readyToFire;
        }

        public void ResetERDComponent()
        {
            ERDAPIWrapper.ChangeCustomRecoil(_localGun.ArchetypeData.persistentID, _localGun.BulletWeapon);
        }
    }

    public class DataSwapper
    {
        private protected readonly IGunComp _gun;
        private protected IOwnerComp _owner;
        private protected readonly CustomGunComponent _cgc;
        private protected readonly SortedDictionary<uint, LinkedList<SwapData>> _priorityStacks = new();
        private protected LinkedList<SwapData> _currentList;
        private protected SwapData _activeSwap;
        private protected SwapData _lastSwap;
        private protected uint _highestPriority;
        private protected SwapData _baseData;
        private int _magBuffer = 0;
        private float _costBuffer = 0f;

        protected DataSwapper(CustomGunComponent cgc)
        {
            _gun = cgc.Gun;
            _owner = cgc.Owner;
            _cgc = cgc;
            _lastSwap = null!;
            _baseData = _activeSwap = GetBaseSwapData(_gun);
            _currentList = new();
            _priorityStacks.Add(0, _currentList);
            _currentList.AddFirst(_activeSwap);
            _highestPriority = 0;
        }

        protected virtual SwapData GetBaseSwapData(IGunComp gun) => new(null, gun.ArchetypeData, gun.AudioData);

        public static DataSwapper Create(CustomGunComponent cgc)
        {
            if (cgc.Owner.IsType(OwnerType.Local))
                return new LocalDataSwapper(cgc);
            else
                return new DataSwapper(cgc);
        }

        public void RefreshOwner() => _owner = _cgc.Owner;

        public LinkedListNode<SwapData> AddSwap(DataSwap dataSwap, ArchetypeDataBlock? arch, WeaponAudioDataBlock? audio, BulletWeaponArchetype? gunArchetype = null)
        {
            SwapSettings settings = SwapSettings.None;
            if (dataSwap.KeepMagCost)
                settings |= SwapSettings.KeepMagCost;
            if (dataSwap.EndBurst)
                settings |= SwapSettings.EndBurst;
            if (dataSwap.EndCharge)
                settings |= SwapSettings.EndCharge;
            if (dataSwap.EndFiring)
                settings |= SwapSettings.EndFiring;

            var priority = dataSwap.Priority;
            var data = new SwapData(gunArchetype ?? _baseData.GunArchetype, arch ?? _baseData.ArchetypeData, audio ?? _baseData.AudioData, priority, settings);
            if (priority < _highestPriority)
            {
                var list = _priorityStacks.GetOrAddNew(priority);
                return list.AddLast(data);
            }
            else
            {
                _currentList = _priorityStacks.GetOrAddNew(priority);
                _highestPriority = priority;
            }

            var node = _currentList.AddLast(data);
            UpdateData();
            return node;
        }

        public void AddSwap(LinkedListNode<SwapData> node)
        {
            var list = node.List;
            if (list != null)
            {
                list.Remove(node);
                list.AddLast(node);
                if (list == _currentList)
                    UpdateData();
            }
            else
            {
                var priority = node.Value.Priority;
                list = _priorityStacks.GetOrAddNew(priority);
                list.AddLast(node);
                if (_highestPriority <= priority)
                {
                    _highestPriority = priority;
                    _currentList = list;
                    UpdateData();
                }
            }
        }

        public void RemoveSwap(LinkedListNode<SwapData> node)
        {
            var list = node.List;
            if (list == null) return;

            list.Remove(node);
            if (_activeSwap == node.Value)
                UpdateData();
        }

        private void UpdateData()
        {
            if (_currentList.Count == 0)
            {
                foreach ((var priority, var list) in _priorityStacks)
                {
                    if (list.Count > 0)
                    {
                        _currentList = list;
                        _highestPriority = priority;
                    }
                }
            }
            var newSwap = _currentList.Last!.Value;
            if (newSwap == _activeSwap) return;

            _lastSwap = _activeSwap;
            _activeSwap = newSwap;

            bool archChanged = !_lastSwap.ArchMatch(_activeSwap);
            bool audioChanged = !_lastSwap.AudioMatch(_activeSwap) || _lastSwap.ArchetypeData.FireMode != _activeSwap.ArchetypeData.FireMode;
            if (audioChanged)
                SetLoopingAudio(false);

            if (archChanged)
                ChangeArchetype();

            if (audioChanged)
            {
                _gun.AudioData = _activeSwap.AudioData;
                SetLoopingAudio(true);
            }
        }

        protected virtual void OnArchetypeChange() { }
        protected virtual void OnArchetypeChangeComplete() { }

        private void ChangeArchetype()
        {
            var settings = _activeSwap.Settings;
            var newBlock = _activeSwap.ArchetypeData;

            int clip = _gun.GetCurrentClip();
            int clipSize = _gun.GetMaxClip();

            var oldBlock = _gun.ArchetypeData;
            _gun.ArchetypeData = newBlock;
            OnArchetypeChange();
            _cgc.RefreshArchetypeCache(oldBlock);

            int newClipSize = _gun.GetMaxClip();

            bool hasOwner = _owner.Player != null && PlayerBackpackManager.TryGetBackpack(_owner.Player.Owner, out var backpack);
            PlayerAmmoStorage? ammoStorage;
            InventorySlotAmmo? slotAmmo;
            float clipCost;
            if (hasOwner)
            {
                ammoStorage = PlayerBackpackManager.GetBackpack(_owner.Player!.Owner).AmmoStorage;
                slotAmmo = ammoStorage.GetInventorySlotAmmo(_gun.AmmoType);
                clipCost = clip * slotAmmo.CostOfBullet;
            }
            else if (_owner.IsType(OwnerType.Sentry))
            {
                ammoStorage = null;
                slotAmmo = null;
                clipCost = ((SentryGunComp)_gun).CostOfBullet;
            }
            else
            {
                if (_cgc.SpreadController.Active && (!_gun.IsAiming || _gun.Component.ItemDataBlock.ShowCrosshairWhenAiming))
                    GuiManager.CrosshairLayer.ShowSpreadCircle(_gun.Component.RecoilData.hipFireCrosshairSizeDefault);
                return;
            }

            int slotAmmoClipSize = newClipSize;
            float cost = newBlock.CostOfBullet;
            if (_owner.IsType(OwnerType.Sentry))
            {
                slotAmmoClipSize = 0;
                var sentryComp = (SentryGunComp)_gun;
                cost = sentryComp.CostOfBullet;
            }

            if (hasOwner)
            {
                slotAmmo!.Setup(cost / EXPAPIWrapper.GetCapacityMod(_owner), slotAmmoClipSize);
                cost = slotAmmo.CostOfBullet;
            }

            if (settings.HasFlag(SwapSettings.KeepMagCost))
            {
                float newBuffer = clipCost;
                _costBuffer += newBuffer + 1e-10f; // Some small constant to avoid rounding errors
                int newMag = Math.Min(newClipSize, (int)(_costBuffer / cost));
                _costBuffer -= newMag * cost;
                newBuffer -= newMag * cost;

                if (hasOwner)
                    slotAmmo!.AmmoInPack += newBuffer;
                _gun.SetCurrentClip(newMag);
            }
            else
            {
                // Calculate the target magazine
                float newMagFloat = (float)clip / clipSize * newClipSize + _magBuffer;
                int newMag = Math.Min(newClipSize, (int)newMagFloat);

                // Calculate the maximum amount of ammo we can give
                float maxCost = clipCost + (hasOwner ? slotAmmo!.AmmoInPack : 0);
                if (maxCost < newMag * cost)
                    newMag = (int)(maxCost / cost);

                _magBuffer = Math.Max(0, clip - newMag * clipSize / newClipSize);
                if (hasOwner)
                    slotAmmo!.AmmoInPack += clipCost - newMag * cost;
                _gun.SetCurrentClip(newMag);
            }

            if (hasOwner)
            {
                ammoStorage!.UpdateSlotAmmoUI(slotAmmo, _gun.GetCurrentClip());
                ammoStorage.NeedsSync = true;
            }

            OnArchetypeChangeComplete();
        }

        protected virtual void SetLoopingAudio(bool start)
        {
            if (_gun is SentryGunComp sentry)
            {
                if (!sentry.Value.m_isFiring || sentry.FireMode != eWeaponFireMode.Auto) return;

                if (start)
                    sentry.Firing.TriggerAutoFireStartAudio();
                else
                    sentry.Firing.TriggerAutoFireEndAudio();
            }
        }
    }
}

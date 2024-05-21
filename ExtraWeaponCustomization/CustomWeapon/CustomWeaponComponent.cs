using AK;
using ExtraWeaponCustomization.CustomWeapon.Properties;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts.Firing;
using ExtraWeaponCustomization.Utils;
using Gear;
using System;
using System.Collections.Generic;
using UnityEngine;
using static GameData.GD;

namespace ExtraWeaponCustomization.CustomWeapon
{
    public sealed class CustomWeaponComponent : MonoBehaviour
    {
        public BulletWeapon Weapon { get; private set; }

        private readonly ContextController _contextController;

        private AutoAim? _autoAim;

        public bool CancelShot { get; set; }

        private readonly HashSet<Type> _propertyTypes;
        // When canceling a shot, holds the next shot timer so we can reset back to it
        private float _lastShotTimer = 0f;
        private float _lastBurstTimer = 0f;
        private float _lastFireRate = 0f;
        public float CurrentFireRate { get; private set; }
        public float CurrentBurstDelay { get; private set; }

        private readonly float _fireRate;
        private readonly float _burstDelay;

        public CustomWeaponComponent(IntPtr value) : base(value) {
            _contextController = new ContextController();
            _propertyTypes = new HashSet<Type>();

            BulletWeapon? bulletWeapon = GetComponent<BulletWeapon>();
            if (bulletWeapon == null)
                throw new ArgumentException("Parent Object", "Custom Weapon Component was added to an object without a Bullet Weapon component.");
            Weapon = bulletWeapon;

            _fireRate = 1f / Math.Max(Weapon.m_archeType.ShotDelay(), CustomWeaponData.MinShotDelay);
            _lastFireRate = _fireRate;
            CurrentFireRate = _fireRate;
            _burstDelay = Weapon.m_archeType.BurstDelay();
            CustomWeaponManager.Current.AddCWCListener(this);
        }

        public void Update()
        {
            _autoAim?.Update();
        }

        public void OnEnable()
        {
            _autoAim?.OnEnable();
        }

        public void OnDisable()
        {
            _autoAim?.OnDisable();
        }

        public void Invoke<TContext>(TContext context) where TContext : IWeaponContext => _contextController.Invoke(context);

        public void Register(IWeaponProperty property)
        {
            if (property is AutoAim autoAim)
            {
                if (Weapon.Owner != null && !Weapon.Owner.IsLocallyOwned)
                    return;
                _autoAim ??= autoAim;
            }

            _contextController.Register(property);

            _propertyTypes.Add(property.GetType());
        }

        public void Register(CustomWeaponData data)
        {
            List<IWeaponProperty> properties = data.Properties.ConvertAll(property => property.Clone());
            foreach (IWeaponProperty property in properties)
                Register(property);

            Invoke(new WeaponPostSetupContext(Weapon));
        }

        public void Clear()
        {
            _propertyTypes.Clear();
            _contextController.Clear();
            _autoAim?.OnDisable();
            _autoAim = null;
            CurrentFireRate = _fireRate;
            CurrentBurstDelay = _burstDelay;
        }

        public bool HasProperty(Type type) => _propertyTypes.Contains(type);

        public void StoreCancelShot()
        {
            Invoke(new WeaponCancelFireContext(Weapon));
            CancelShot = true;
        }

        public bool ResetShotIfCancel(BulletWeaponArchetype archetype)
        {
            if (CancelShot)
            {
                archetype.m_fireHeld = false;
                archetype.m_nextShotTimer = _lastShotTimer;
                archetype.m_nextBurstTimer = _lastBurstTimer;
                CurrentFireRate = _lastFireRate;
                Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
                if (archetype.m_archetypeData.FireMode == eWeaponFireMode.Burst)
                    archetype.TryCast<BWA_Burst>()!.m_burstCurrentCount = 0;
                return true;
            }
            return false;
        }

        public void UpdateStoredFireRate(BulletWeaponArchetype archetype)
        {
            _lastFireRate = CurrentFireRate;
            _lastShotTimer = archetype.m_nextShotTimer;
            _lastBurstTimer = archetype.m_nextBurstTimer;

            // Invoke callbacks that override base fire rate
            WeaponFireRateSetContext context = new(Weapon, _fireRate);
            Invoke(context);

            // Invoke callbacks that modify current fire rate
            WeaponFireRateContext postContext = new(Weapon, context.FireRate);
            Invoke(postContext);

            if (CurrentFireRate != postContext.FireRate)
            {
                CurrentFireRate = Math.Clamp(postContext.FireRate, 0.001f, CustomWeaponData.MaxFireRate);
                CurrentBurstDelay = _burstDelay * _fireRate / CurrentFireRate;
                Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
            }
        }

        public void ModifyFireRate(BulletWeaponArchetype archetype) {
            archetype.m_nextShotTimer = Clock.Time + 1f / CurrentFireRate;
            if (archetype.BurstIsDone())
                archetype.m_nextBurstTimer = Math.Max(Clock.Time + CurrentBurstDelay, archetype.m_nextShotTimer);
        }
    }
}

using EWC.CustomWeapon.Enums;
using EWC.Utils;
using UnityEngine;

namespace EWC.API
{
    public static class FireShotAPI
    {
        public delegate void PreShotFiredCallback(HitData hitData, Ray ray, WeaponType weaponType);
        public delegate void ShotFiredCallback(HitData hitData, Vector3 startPos, Vector3 endPos, WeaponType weaponType);

        public static event PreShotFiredCallback? PreShotFired;
        public static event ShotFiredCallback? OnShotFired;

        internal static void FirePreShotFiredCallback(HitData hitData, Ray ray, WeaponType weaponType) => PreShotFired?.Invoke(hitData, ray, weaponType);
        internal static void FireShotFiredCallback(HitData hitData, Vector3 startPos, Vector3 endPos, WeaponType weaponType) => OnShotFired?.Invoke(hitData, startPos, endPos, weaponType);
    }
}

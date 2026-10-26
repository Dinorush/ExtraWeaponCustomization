using EWC.CustomWeapon;
using EWC.Utils;
using UnityEngine;

namespace EWC.API
{
    public static class FireShotAPI
    {
        public delegate void PreShotFiredCallback(HitData hitData, Ray ray, CustomWeaponComponent cwc);
        public delegate void ShotFiredCallback(HitData hitData, Vector3 startPos, Vector3 endPos, CustomWeaponComponent cwc);

        public static event PreShotFiredCallback? PreShotFired;
        public static event ShotFiredCallback? OnShotFired;

        internal static void FirePreShotFiredCallback(HitData hitData, Ray ray, CustomWeaponComponent cwc) => PreShotFired?.Invoke(hitData, ray, cwc);
        internal static void FireShotFiredCallback(HitData hitData, Vector3 startPos, Vector3 endPos, CustomWeaponComponent cwc) => OnShotFired?.Invoke(hitData, startPos, endPos, cwc);
    }
}

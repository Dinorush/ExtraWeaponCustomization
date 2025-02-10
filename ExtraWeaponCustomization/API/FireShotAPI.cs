using EWC.Utils;
using UnityEngine;

namespace EWC.API
{
    public static class FireShotAPI
    {
        public delegate void PreShotFiredCallback(HitData hitData, Ray ray);
        public delegate void ShotFiredCallback(HitData hitData, Vector3 startPos, Vector3 endPos);

        public static event PreShotFiredCallback? PreShotFired;
        public static event ShotFiredCallback? OnShotFired;

        internal static void FirePreShotFiredCallback(HitData hitData, Ray ray) => PreShotFired?.Invoke(hitData, ray);
        internal static void FireShotFiredCallback(HitData hitData, Vector3 startPos, Vector3 endPos) => OnShotFired?.Invoke(hitData, startPos, endPos);
    }
}

using EWC.Utils;
using System;
using UnityEngine;

namespace EWC.API
{
    public static class FireShotAPI
    {
        public static event Action<HitData, Vector3, Vector3>? OnShotFired;

        internal static void FireShotFiredCallback(HitData hitData, Vector3 startPos, Vector3 endPos) => OnShotFired?.Invoke(hitData, startPos, endPos);
    }
}

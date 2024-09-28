using UnityEngine;

namespace EWC.Utils
{
    internal static class FloatExtensions
    {
        public static float Map(this float orig, float fromMin, float fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax) return orig < fromMin ? toMin : toMax;

            orig = Mathf.Clamp(orig, fromMin, fromMax);
            return (orig - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
    }
}
